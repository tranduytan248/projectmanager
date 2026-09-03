using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Thông báo tổng số giờ logtime trong ngày qua tin nhắn SMS vào lúc 17h mỗi ngày làm việc.
    ///
    /// Giúp nhân sự nắm bắt số giờ công thực tế đã ghi nhận trong ngày, kịp thời bổ sung
    /// hoặc hoàn tất định mức trước khi kết thúc ca làm việc.
    ///
    /// Tự động bỏ qua Thứ 7, Chủ Nhật và các ngày nghỉ lễ đã cấu hình (HolidayService).
    /// Hỗ trợ cả SMS viễn thông và FCM Push Notification tới ứng dụng di động BrewTask.
    /// </summary>
    public static class DailyLogTimeSmsService
    {
        /// <summary>
        /// Một người nhận tin, kèm tổng giờ logtime và trạng thái gửi.
        /// </summary>
        public class Recipient
        {
            public int UserId { get; set; }
            public string FullName { get; set; }

            /// <summary>Số điện thoại nhận tin trên tài khoản. Trống thì không gửi được SMS.</summary>
            public string Phone { get; set; }

            /// <summary>Tổng giờ đã log trong ngày (cộng từ tất cả đầu việc).</summary>
            public decimal TotalHours { get; set; }

            /// <summary>Định mức giờ làm việc chuẩn trong ngày (mặc định 8h).</summary>
            public decimal StandardHours { get; set; }

            /// <summary>Nội dung tin nhắn SMS (không dấu, chuẩn viễn thông).</summary>
            public string SmsContent { get; set; }

            /// <summary>Nội dung thông báo hiển thị trên app mobile (tiếng Việt có dấu).</summary>
            public string MobilePushBody { get; set; }

            /// <summary>Đã đạt hoặc vượt định mức giờ chuẩn trong ngày hay chưa.</summary>
            public bool IsSufficient { get { return TotalHours >= StandardHours; } }

            /// <summary>Có số điện thoại để gửi SMS hay không.</summary>
            public bool CanSendSms { get { return !string.IsNullOrWhiteSpace(Phone); } }
        }

        /// <summary>
        /// Kết quả rà soát phục vụ cả màn hình xem trước (preview) lẫn lượt gửi thật.
        /// </summary>
        public class Preview
        {
            public DateTime Day { get; set; }
            public List<Recipient> Recipients { get; set; }

            /// <summary>Ngày làm việc hợp lệ (không phải thứ 7, chủ nhật hay ngày lễ).</summary>
            public bool IsWorkingDay { get; set; }

            public decimal StandardHours { get; set; }

            public Preview()
            {
                Recipients = new List<Recipient>();
            }

            public int TotalCount { get { return Recipients.Count; } }
            public int SendableCount { get { return Recipients.Count(r => r.CanSendSms); } }
            public int NoPhoneCount { get { return Recipients.Count(r => !r.CanSendSms); } }

            public int SufficientCount { get { return Recipients.Count(r => r.IsSufficient); } }
            public int UnderCount { get { return Recipients.Count(r => r.TotalHours > 0 && !r.IsSufficient); } }
            public int ZeroCount { get { return Recipients.Count(r => r.TotalHours <= 0); } }
        }

        /// <summary>
        /// Kiểm tra ngày gửi tin. Mặc định gửi tất cả các ngày (không bỏ qua Thứ 7, Chủ Nhật và ngày lễ)
        /// theo yêu cầu vận hành. Có thể bật lại cờ SkipWeekendsAndHolidays trong cấu hình nếu cần.
        /// </summary>
        public static bool IsWorkingDay(DateTime day)
        {
            if (AppSettings.Reminder.LogTimeSkipWeekendsAndHolidays)
            {
                if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                    return false;

                if (HolidayService.IsHoliday(day))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Dựng nội dung tin nhắn SMS (không dấu, chuẩn viễn thông, dưới 160 ký tự = 1 tin).
        /// </summary>
        public static string BuildSmsMessage(string fullName, decimal totalHours, decimal standardHours, DateTime day)
        {
            var dateStr = day.ToString("dd/MM");
            var hoursStr = totalHours.ToString("0.##");
            var standardStr = standardHours.ToString("0.##");

            if (totalHours >= standardHours)
            {
                return string.Format("[BrewTask] Hom nay {0} ban da logtime du {1}h/{2}h. Cam on ban da tich cuc lam viec!",
                    dateStr, hoursStr, standardStr);
            }
            else if (totalHours > 0)
            {
                return string.Format("[BrewTask] Hom nay {0} ban da logtime {1}h/{2}h. Vui long kiem tra va log bo sung truoc khi ket thuc ca!",
                    dateStr, hoursStr, standardStr);
            }
            else
            {
                return string.Format("[BrewTask] Hom nay {0} ban chua logtime gio cong nao (0h/{1}h). Vui long cap nhat gio lam truoc khi ve!",
                    dateStr, standardStr);
            }
        }

        /// <summary>
        /// Dựng nội dung thông báo đẩy mobile app (tiếng Việt có dấu).
        /// </summary>
        public static string BuildMobilePushBody(decimal totalHours, decimal standardHours, DateTime day)
        {
            var dateStr = day.ToString("dd/MM");
            var hoursStr = totalHours.ToString("0.##");
            var standardStr = standardHours.ToString("0.##");

            if (totalHours >= standardHours)
            {
                return string.Format("Hôm nay ({0}) bạn đã hoàn thành logtime {1}h/{2}h. Cảm ơn bạn đã tích cực làm việc!",
                    dateStr, hoursStr, standardStr);
            }
            else if (totalHours > 0)
            {
                return string.Format("Hôm nay ({0}) bạn mới logtime {1}h/{2}h. Hãy nhớ log bổ sung trước khi kết thúc ca nhé!",
                    dateStr, hoursStr, standardStr);
            }
            else
            {
                return string.Format("Hôm nay ({0}) bạn chưa ghi nhận giờ công nào (0h/{1}h). Hãy cập nhật logtime nhé!",
                    dateStr, standardStr);
            }
        }

        /// <summary>
        /// Rà soát dữ liệu logtime trong ngày cho toàn bộ nhân sự.
        /// </summary>
        public static Preview Build(DateTime day)
        {
            var targetDate = day.Date;
            var standardHours = AppSettings.Reminder.LogTimeStandardHours;
            var sendAll = AppSettings.Reminder.LogTimeSendAllActiveUsers;

            var result = new Preview
            {
                Day = targetDate,
                IsWorkingDay = IsWorkingDay(day),
                StandardHours = standardHours
            };

            // Gom tổng giờ logtime theo từng người trong ngày đang xét
            var logsToday = Repository.WorkTimeLogs.All()
                .Where(l => l.WorkDate.Date == targetDate)
                .GroupBy(l => l.UserId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Hours));

            var activeUsers = WorkService.ActiveUsers().OrderBy(u => u.FullName).ToList();

            foreach (var user in activeUsers)
            {
                decimal hours;
                logsToday.TryGetValue(user.Id, out hours);

                // Nếu cấu hình chỉ gửi người có logtime và người này chưa có log nào thì bỏ qua
                if (!sendAll && hours <= 0) continue;

                var recipient = new Recipient
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Phone = (user.Phone ?? string.Empty).Trim(),
                    TotalHours = hours,
                    StandardHours = standardHours,
                    SmsContent = BuildSmsMessage(user.FullName, hours, standardHours, targetDate),
                    MobilePushBody = BuildMobilePushBody(hours, standardHours, targetDate)
                };

                result.Recipients.Add(recipient);
            }

            return result;
        }

        /// <summary>
        /// Thực thi gửi tin nhắn và ghi nhật ký hệ thống.
        /// </summary>
        public static ReminderLog Run(DateTime now, bool isManual, string triggeredBy)
        {
            var preview = Build(now);

            var log = new ReminderLog
            {
                Kind = ReminderKind.DailyLogTimeSms,
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
                log.Error = "Ngày nghỉ hoặc ngày lễ — không gửi tin nhắn.";
                Repository.ReminderLogs.Insert(log);
                return log;
            }

            if (preview.Recipients.Count == 0)
            {
                log.Success = true;
                log.Error = "Không có nhân sự nào trong danh sách cần gửi hôm nay.";
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
            var summary = new StringBuilder();
            var pushEnabled = AppSettings.Reminder.LogTimeFcmPushEnabled;

            foreach (var r in preview.Recipients)
            {
                // 1. Gửi SMS viễn thông
                if (!r.CanSendSms)
                {
                    summary.AppendLine(string.Format("- {0}: chưa có số điện thoại, bỏ qua SMS (Log: {1:0.##}h).",
                        r.FullName, r.TotalHours));
                }
                else
                {
                    List<string> invalid;
                    var phones = SmsClient.NormalizePhones(r.Phone, out invalid);

                    if (phones.Count == 0)
                    {
                        failures.Add(r.FullName + ": số điện thoại không hợp lệ (" + r.Phone + ")");
                        summary.AppendLine(string.Format("- {0}: LỖI SMS — số không hợp lệ ({1}).",
                            r.FullName, r.Phone));
                    }
                    else
                    {
                        var result = SmsClient.Send(phones, r.SmsContent);
                        if (result.Ok)
                        {
                            log.SentCount++;
                            summary.AppendLine(string.Format("- {0} <{1}>: đã gửi SMS (Log: {2:0.##}h).",
                                r.FullName, phones[0], r.TotalHours));
                        }
                        else
                        {
                            failures.Add(r.FullName + ": " + result.Error);
                            summary.AppendLine(string.Format("- {0} <{1}>: LỖI SMS — {2}",
                                r.FullName, phones[0], result.Error));
                        }
                    }
                }

                // 2. Gửi kèm thông báo đẩy mobile app BrewTask nếu bật
                if (pushEnabled)
                {
                    try
                    {
                        var pushTitle = string.Format("[BrewTask] Nhắc nhở giờ công ngày {0:dd/MM}", preview.Day);
                        FcmPushService.SendToUser(r.UserId, pushTitle, r.MobilePushBody, "logtime_daily");
                    }
                    catch
                    {
                        // Nuốt lỗi push để không ảnh hưởng luồng gửi SMS
                    }
                }
            }

            var sendable = preview.SendableCount;
            log.Success = log.SentCount > 0 || sendable == 0;
            log.Error = failures.Count == 0
                ? (sendable == 0 ? "Không ai trong danh sách có số điện thoại — chưa gửi được tin SMS nào." : null)
                : string.Format("{0}/{1} tin gửi lỗi: {2}",
                    failures.Count, sendable, string.Join(" | ", failures));
            log.Message = summary.ToString().TrimEnd();

            Repository.ReminderLogs.Insert(log);
            return log;
        }

        /// <summary>
        /// Kiểm tra xem lượt thông báo logtime tự động của ngày này đã gửi thành công chưa.
        /// </summary>
        public static bool AlreadySent(DateTime day)
        {
            var date = day.Date;

            return Repository.ReminderLogs.All()
                .Any(l => l.Kind == ReminderKind.DailyLogTimeSms && l.Success && !l.IsManual && l.SentAt.Date == date);
        }
    }
}
