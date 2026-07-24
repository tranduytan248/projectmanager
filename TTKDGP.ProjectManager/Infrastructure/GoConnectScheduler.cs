using System;
using System.IO;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Cứ mỗi phút kiểm tra một lần: tới giờ hẹn buổi sáng (mặc định 07:30) mà hôm nay chưa
    /// chạy thì tự khởi động một phiên đăng nhập GoConnect với số điện thoại mặc định.
    ///
    /// Dùng điều kiện "đã qua giờ hẹn và hôm nay chưa chạy" (giống bộ nhắc báo cáo) nên nếu
    /// lỡ nhịp — ví dụ ứng dụng vừa khởi động lại sau giờ hẹn — thì vẫn chạy bù trong ngày.
    /// </summary>
    public static class GoConnectScheduler
    {
        private static Timer _timer;
        private static readonly object Sync = new object();
        private static DateTime _lastRunDate = DateTime.MinValue;

        public static void Start()
        {
            if (!AppSettings.GoConnect.IsConfigured) return;
            if (string.IsNullOrWhiteSpace(AppSettings.GoConnect.DefaultPhone)) return;

            lock (Sync)
            {
                if (_timer != null) return;
                _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
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
            if (HostingEnvironment.ShutdownReason != ApplicationShutdownReason.None) return;

            try
            {
                var now = DateTime.Now;

                if (now.Date == _lastRunDate) return;          // hôm nay đã chạy (trong phiên này)
                if (!IsPastSchedule(now)) return;              // chưa tới giờ hẹn
                if (AlreadyRanToday(now)) { _lastRunDate = now.Date; return; }  // đã chạy hôm nay ở phiên trước

                _lastRunDate = now.Date;
                MarkRanToday(now);

                Action<string> notify = m => TelegramClient.SendMessage(
                    AppSettings.GoConnect.BotToken, AppSettings.GoConnect.ChatId, m);

                notify(string.Format("⏰ Tới giờ đăng nhập GoConnect tự động ({0:HH:mm dd/MM}).", now));

                // Chạy nền: phiên sẽ chờ OTP tới qua bộ nhận tin Telegram.
                var ignored = GoConnectAutoLogin.RunAsync(AppSettings.GoConnect.DefaultPhone, notify);
            }
            catch
            {
                // Bộ lịch chạy nền, có lỗi cũng không được làm sập ứng dụng.
            }
        }

        /// <summary>Ghi mốc "đã chạy hôm nay" ra file để restart/recycle không kích hoạt lại.</summary>
        private static bool AlreadyRanToday(DateTime now)
        {
            try
            {
                var file = LastRunFile();
                if (!File.Exists(file)) return false;
                return File.ReadAllText(file).Trim() == now.ToString("yyyy-MM-dd");
            }
            catch
            {
                return false;   // Đọc lỗi thì coi như chưa chạy, thà chạy còn hơn bỏ sót.
            }
        }

        private static void MarkRanToday(DateTime now)
        {
            try
            {
                var file = LastRunFile();
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, now.ToString("yyyy-MM-dd"));
            }
            catch
            {
                // Không ghi được thì thôi, chỉ mất tác dụng chống lặp qua restart.
            }
        }

        private static string LastRunFile()
        {
            var root = HostingEnvironment.IsHosted
                ? HostingEnvironment.MapPath("~/App_Data")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            return Path.Combine(root, "goconnect-lastrun.txt");
        }

        private static bool IsPastSchedule(DateTime now)
        {
            var hour = AppSettings.GoConnect.AutoLoginHour;
            var minute = AppSettings.GoConnect.AutoLoginMinute;

            if (now.Hour > hour) return true;
            if (now.Hour < hour) return false;
            return now.Minute >= minute;
        }
    }
}
