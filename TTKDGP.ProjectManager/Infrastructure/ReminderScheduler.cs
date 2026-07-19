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
            var sent = 0;

            if (IsDue(now, DayOfWeek.Monday, AppSettings.Reminder.MondayHour, ReminderKind.MondayPreviousWeek))
            {
                ReminderService.Run(ReminderKind.MondayPreviousWeek, now, false, null);
                sent++;
            }

            if (IsDue(now, DayOfWeek.Friday, AppSettings.Reminder.FridayHour, ReminderKind.FridayCurrentWeek))
            {
                ReminderService.Run(ReminderKind.FridayCurrentWeek, now, false, null);
                sent++;
            }

            return sent;
        }

        private static bool IsDue(DateTime now, DayOfWeek day, int hour, ReminderKind kind)
        {
            if (now.DayOfWeek != day) return false;
            if (now.Hour < hour) return false;

            // Kỳ được rà soát: thứ Hai xét tuần trước, thứ Sáu xét tuần này.
            var target = kind == ReminderKind.MondayPreviousWeek ? now.AddDays(-7) : now;
            var year = WeekHelper.GetYear(target);
            var week = WeekHelper.GetWeek(target);

            return !ReminderService.AlreadySent(kind, year, week);
        }
    }
}
