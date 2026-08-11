using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Nhắc hạn công việc bằng tin nhắn SMS, hai lượt mỗi ngày làm việc (sáng và chiều).
    ///
    /// Nội dung cố ý ngắn: "Bạn có N task sẽ hết hạn trong ngày dd/MM/yyyy". Tin nhắn tính cước
    /// theo độ dài, và người nhận chỉ cần biết CÓ VIỆC ĐẾN HẠN để mở hệ thống lên xem — liệt kê
    /// tên từng việc vừa dài vừa lộ nội dung công việc ra ngoài đường truyền của nhà mạng.
    ///
    /// Chỉ đếm việc hết hạn ĐÚNG NGÀY GỬI. Việc đã quá hạn từ hôm trước không được cộng vào, để
    /// con số trong tin luôn khớp với chữ "trong ngày" đứng ngay sau nó.
    /// </summary>
    public static class TaskDueSmsService
    {
        /// <summary>
        /// Một người sẽ nhận tin, kèm số việc đến hạn và số điện thoại.
        /// </summary>
        public class Recipient
        {
            public int UserId { get; set; }
            public string FullName { get; set; }

            /// <summary>Số điện thoại trên tài khoản. Trống thì không gửi được.</summary>
            public string Phone { get; set; }

            /// <summary>Số việc hết hạn đúng trong ngày đang xét.</summary>
            public int DueCount { get; set; }

            public bool CanSend { get { return !string.IsNullOrWhiteSpace(Phone); } }
        }

        /// <summary>Kết quả một lượt rà soát, dùng cho cả xem trước lẫn lúc gửi thật.</summary>
        public class Preview
        {
            public DateTime Day { get; set; }
            public List<Recipient> Recipients { get; set; }

            /// <summary>Ngày nghỉ thì không nhắc ai — thứ Bảy và Chủ nhật.</summary>
            public bool IsWorkingDay { get; set; }

            public Preview()
            {
                Recipients = new List<Recipient>();
            }

            public int SendableCount { get { return Recipients.Count(r => r.CanSend); } }
            public int NoPhoneCount { get { return Recipients.Count(r => !r.CanSend); } }
        }

        /// <summary>
        /// Ngày làm việc: từ thứ Hai đến thứ Sáu. Thứ Bảy và Chủ nhật không nhắc.
        ///
        /// Chưa xét ngày lễ — hệ thống không có danh mục ngày nghỉ lễ, mà đoán theo ngày dương
        /// lịch thì sai với lịch nghỉ bù của nhà nước. Thà nhắn thừa một hôm lễ còn hơn im lặng
        /// vào một ngày làm việc thật.
        /// </summary>
        public static bool IsWorkingDay(DateTime day)
        {
            return day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday;
        }

        /// <summary>Nội dung tin nhắn gửi cho một người.</summary>
        public static string BuildMessage(int dueCount, DateTime day)
        {
            return string.Format("Ban co {0} task se het han trong ngay {1:dd/MM/yyyy}", dueCount, day);
        }

        /// <summary>
        /// Rà soát xem hôm đó ai có việc đến hạn. KHÔNG gửi gì — dùng chung cho màn xem trước
        /// và cho lượt gửi thật, để những gì nhìn thấy trên màn hình đúng bằng những gì sẽ gửi.
        /// </summary>
        public static Preview Build(DateTime day)
        {
            var result = new Preview
            {
                Day = day.Date,
                IsWorkingDay = IsWorkingDay(day)
            };

            if (!result.IsWorkingDay) return result;

            // Nạp cả bảng một lần rồi gom theo người, thay vì hỏi lại kho cho từng tài khoản.
            var dueByUser = WorkService.AllTasks()
                .Where(t => t.AssigneeUserId > 0)
                .Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == result.Day)

                // Việc đã xong, đã huỷ, hay đang tạm dừng thì không nhắc: hai loại đầu không còn
                // phải làm, loại thứ ba đang chờ gỡ vướng chứ không chờ người thực hiện.
                .Where(t => !TaskStates.IsClockStopped(t.State))

                .GroupBy(t => t.AssigneeUserId)
                .ToDictionary(g => g.Key, g => g.Count());

            if (dueByUser.Count == 0) return result;

            // Đi theo danh sách tài khoản đang hoạt động: việc còn gán cho người đã nghỉ thì
            // không nhắc ai cả, mà cũng không được lặng lẽ biến mất khỏi bảng xem trước.
            foreach (var user in WorkService.ActiveUsers())
            {
                int count;
                if (!dueByUser.TryGetValue(user.Id, out count) || count <= 0) continue;

                result.Recipients.Add(new Recipient
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Phone = (user.Phone ?? string.Empty).Trim(),
                    DueCount = count
                });
            }

            return result;
        }

        /// <summary>
        /// Rà soát rồi gửi tin cho từng người. Ghi một dòng nhật ký cho mỗi lượt, kể cả lượt
        /// không có ai để nhắc — nhìn nhật ký phải biết được lịch có chạy hay không.
        /// </summary>
        public static ReminderLog Run(ReminderKind kind, DateTime now, bool isManual, string triggeredBy)
        {
            var preview = Build(now);

            var log = new ReminderLog
            {
                Kind = kind,
                Year = WeekHelper.GetYear(now),
                Week = WeekHelper.GetWeek(now),
                SentAt = now,
                IsManual = isManual,
                TriggeredBy = triggeredBy,
                PmCount = preview.Recipients.Count,
                NoEmailCount = preview.NoPhoneCount
            };

            if (!preview.IsWorkingDay)
            {
                log.Success = true;
                log.Error = "Ngày nghỉ (thứ Bảy/Chủ nhật) — không nhắc.";
                Repository.ReminderLogs.Insert(log);
                return log;
            }

            if (preview.Recipients.Count == 0)
            {
                log.Success = true;
                log.Error = "Không có việc nào đến hạn hôm nay — không gửi tin.";
                Repository.ReminderLogs.Insert(log);
                return log;
            }

            if (!AppSettings.Sms.IsConfigured)
            {
                log.Success = false;
                log.Error = "Chưa cấu hình tổng đài SMS (cần bật Sms:Enabled và có địa chỉ tổng đài).";
                Repository.ReminderLogs.Insert(log);
                return log;
            }

            var failures = new List<string>();
            var summary = new System.Text.StringBuilder();

            foreach (var r in preview.Recipients)
            {
                if (!r.CanSend)
                {
                    summary.AppendLine(string.Format("- {0}: chưa có số điện thoại, bỏ qua ({1} việc).",
                        r.FullName, r.DueCount));
                    continue;
                }

                // Chuẩn hoá ngay trước khi gửi: số nhập tay có thể sai dạng, và tổng đài chỉ
                // nhận 84xxxxxxxxx. Sai dạng thì ghi rõ tên người để quản trị sửa lại hồ sơ.
                List<string> invalid;
                var phones = SmsClient.NormalizePhones(r.Phone, out invalid);

                if (phones.Count == 0)
                {
                    failures.Add(r.FullName + ": số điện thoại không hợp lệ (" + r.Phone + ")");
                    summary.AppendLine(string.Format("- {0}: LỖI — số không hợp lệ ({1}).",
                        r.FullName, r.Phone));
                    continue;
                }

                var result = SmsClient.Send(phones, BuildMessage(r.DueCount, preview.Day));

                if (result.Ok)
                {
                    log.SentCount++;
                    summary.AppendLine(string.Format("- {0} <{1}>: đã gửi ({2} việc).",
                        r.FullName, phones[0], r.DueCount));
                }
                else
                {
                    failures.Add(r.FullName + ": " + result.Error);
                    summary.AppendLine(string.Format("- {0} <{1}>: LỖI — {2}",
                        r.FullName, phones[0], result.Error));
                }
            }

            // Gửi được cho ít nhất một người thì coi như lượt này đã chạy, giống cách kỳ gửi mail
            // đang làm: đánh dấu thất bại chỉ vì vài số lỗi sẽ khiến bộ lịch chạy lại sau 5 phút
            // và nhắn trùng cho những người đã nhận. Lỗi lẻ vẫn ghi rõ trong nhật ký.
            var sendable = preview.SendableCount;
            log.Success = log.SentCount > 0 || sendable == 0;
            log.Error = failures.Count == 0
                ? (sendable == 0 ? "Không ai trong danh sách có số điện thoại — chưa gửi được tin nào." : null)
                : string.Format("{0}/{1} tin gửi lỗi: {2}",
                    failures.Count, sendable, string.Join(" | ", failures));
            log.Message = summary.ToString().TrimEnd();

            Repository.ReminderLogs.Insert(log);
            return log;
        }

        /// <summary>
        /// Lượt nhắc này đã gửi xong chưa. Mốc so sánh là NGÀY chứ không phải tuần: hai lượt
        /// sáng/chiều lặp lại mỗi ngày, dùng mốc tuần như các kỳ nhắc báo cáo thì cả tuần chỉ
        /// gửi được đúng một lần.
        /// </summary>
        public static bool AlreadySent(ReminderKind kind, DateTime day)
        {
            var date = day.Date;

            return Repository.ReminderLogs.All()
                .Any(l => l.Kind == kind && l.Success && !l.IsManual && l.SentAt.Date == date);
        }
    }
}
