using System;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Bộ lịch chạy trong tiến trình ứng dụng: cứ vài phút kiểm tra một lần xem
    /// đã tới giờ gửi nhắc chưa.
    ///
    /// Hạn chế: IIS có thể tắt ứng dụng khi không ai truy cập, lúc đó bộ lịch cũng dừng.
    /// Muốn chắc chắn, hãy bật Application Initialization / Always Running cho application pool,
    /// hoặc dùng Task Scheduler gọi endpoint /Notifications/Trigger (xem README).
    ///
    /// Nếu lỡ giờ, lần chạy sau trong cùng ngày vẫn gửi bù, vì điều kiện là
    /// "đã qua giờ hẹn và kỳ này chưa gửi" chứ không phải "đúng vào giờ hẹn".
    /// </summary>
    public static class ReminderScheduler
    {
        private static Timer _timer;
        private static readonly object Sync = new object();
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

        public static void Start()
        {
            // Máy phát triển để Reminder:AutoSend = false nên không dựng bộ hẹn giờ,
            // tránh việc chạy thử ở local lại bắn mail thật cho anh em.
            if (!AppSettings.Reminder.AutoSend) return;

            lock (Sync)
            {
                if (_timer != null) return;
                _timer = new Timer(OnTick, null, TimeSpan.FromMinutes(1), Interval);
            }
        }

        public static void Stop()
        {
            lock (Sync)
            {
                if (_timer == null) return;
                _timer.Dispose();
                _timer = null;
            }
        }

        private static void OnTick(object state)
        {
            // Ứng dụng đang bị gỡ xuống thì thôi, không đụng vào dữ liệu nữa.
            if (HostingEnvironment.ShutdownReason != ApplicationShutdownReason.None) return;

            try
            {
                RunDue(DateTime.Now);
            }
            catch
            {
                // Bộ lịch chạy nền, có lỗi cũng không được phép làm sập ứng dụng.
                // Kết quả từng lần gửi đã được ghi vào nhật ký nhắc việc.
            }
        }

        /// <summary>
        /// Kiểm tra và gửi các kỳ nhắc đã tới hạn. Tách riêng để endpoint kích hoạt
        /// thủ công và bộ hẹn giờ dùng chung một logic.
        /// </summary>
        public static int RunDue(DateTime now)
        {
            // Endpoint kích hoạt từ Task Scheduler cũng tôn trọng cờ này, để một máy
            // cấu hình nhầm không gửi trùng tin của máy chủ thật.
            if (!AppSettings.Reminder.AutoSend) return 0;

            var sent = 0;

            // Sáng thứ Sáu: mail riêng cho từng thành viên.
            // Các kỳ nhắc gửi lên nhóm Telegram đã bỏ.
            if (IsDue(now, DayOfWeek.Friday, AppSettings.Reminder.MemberEmailHour, ReminderKind.FridayMemberEmails))
            {
                ReminderService.RunMemberEmails(now, false, null);
                sent++;
            }

            sent += RunTaskDueSms(now);

            return sent;
        }

        /// <summary>
        /// Nhắc hạn công việc bằng SMS: mỗi ngày làm việc hai lượt, sáng và chiều.
        ///
        /// Hai lượt là hai kỳ riêng nên lượt chiều vẫn chạy dù sáng đã gửi. Ngày nghỉ được chặn
        /// ngay tại đây để bộ lịch không ghi thêm dòng nhật ký thừa vào mỗi thứ Bảy, Chủ nhật.
        /// </summary>
        private static int RunTaskDueSms(DateTime now)
        {
            if (!AppSettings.Reminder.TaskSmsEnabled) return 0;
            if (!TaskDueSmsService.IsWorkingDay(now)) return 0;

            var sent = 0;
            var morningHour = AppSettings.Reminder.TaskSmsMorningHour;
            var afternoonHour = AppSettings.Reminder.TaskSmsAfternoonHour;

            // Lượt sáng chỉ gửi bù TRONG BUỔI SÁNG — hết giờ chiều thì thôi, vì lượt chiều ngay
            // sau đó mang đúng nội dung ấy. Không chặn thì một lần khởi động lại lúc 17h sẽ bắn
            // hai tin giống hệt nhau cách nhau vài phút.
            if (now.Hour < afternoonHour
                && IsDailyDue(now, morningHour, ReminderKind.TaskDueSmsMorning))
            {
                TaskDueSmsService.Run(ReminderKind.TaskDueSmsMorning, now, false, null);
                sent++;
            }

            if (IsDailyDue(now, afternoonHour, ReminderKind.TaskDueSmsAfternoon))
            {
                TaskDueSmsService.Run(ReminderKind.TaskDueSmsAfternoon, now, false, null);
                sent++;
            }

            return sent;
        }

        private static bool IsDue(DateTime now, DayOfWeek day, int hour, ReminderKind kind)
        {
            if (now.DayOfWeek != day) return false;
            if (now.Hour < hour) return false;

            var year = WeekHelper.GetYear(now);
            var week = WeekHelper.GetWeek(now);

            return !ReminderService.AlreadySent(kind, year, week);
        }

        /// <summary>
        /// Kỳ lặp theo NGÀY đã tới giờ chưa. Khác <see cref="IsDue"/> ở chỗ mốc chống gửi trùng
        /// là ngày chứ không phải tuần — kỳ nhắc ngày nào cũng chạy nên dùng mốc tuần thì cả
        /// tuần chỉ gửi được đúng một lần.
        /// </summary>
        private static bool IsDailyDue(DateTime now, int hour, ReminderKind kind)
        {
            if (now.Hour < hour) return false;

            return !TaskDueSmsService.AlreadySent(kind, now);
        }
    }
}
