using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Đọc cấu hình từ appSettings. Token thật nằm trong App_Config\secrets.config
    /// (không đưa vào kho mã nguồn), được Web.config nạp đè qua thuộc tính file=.
    /// </summary>
    public static class AppSettings
    {
        private static string Get(string key, string fallback = "")
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static int GetInt(string key, int fallback)
        {
            int result;
            return int.TryParse(Get(key), out result) ? result : fallback;
        }

        private static bool GetBool(string key, bool fallback)
        {
            bool result;
            return bool.TryParse(Get(key), out result) ? result : fallback;
        }

        /// <summary>Tách chuỗi nhiều giá trị ngăn cách bởi dấu phẩy.</summary>
        private static List<string> GetList(string key, string fallback)
        {
            var raw = Get(key, fallback);

            return raw.Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        /// <summary>Cấu hình cho các biểu đồ trên màn hình tổng hợp.</summary>
        public static class Dashboard
        {
            /// <summary>
            /// Chỉ những phân công ở các trạng thái công việc này mới được tính vào
            /// biểu đồ khối lượng theo thành viên.
            /// </summary>
            public static List<string> WorkloadWorkStatuses
            {
                get { return GetList("Dashboard:WorkloadWorkStatuses", "Đang thực hiện"); }
            }

            /// <summary>
            /// Và dự án phải đang ở một trong các trạng thái này.
            /// </summary>
            public static List<string> WorkloadProjectStatuses
            {
                get { return GetList("Dashboard:WorkloadProjectStatuses", "Đang thực hiện, Đang hỗ trợ"); }
            }
        }

        /// <summary>Địa chỉ người dùng truy cập hệ thống, đính kèm cuối mỗi tin nhắc.</summary>
        public static string PublicUrl { get { return Get("App:PublicUrl", "pmncpt.cenit.vn"); } }

        /// <summary>Địa chỉ đầy đủ có giao thức, dùng làm href trong tin nhắn.</summary>
        public static string PublicLink
        {
            get
            {
                var url = PublicUrl;
                if (string.IsNullOrWhiteSpace(url)) return null;

                return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                       url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? url
                    : "http://" + url;
            }
        }

        public static class Telegram
        {
            public static bool Enabled { get { return GetBool("Telegram:Enabled", false); } }
            public static string BotToken { get { return Get("Telegram:BotToken"); } }
            public static string ChatId { get { return Get("Telegram:ChatId"); } }

            public static bool HasToken { get { return !string.IsNullOrWhiteSpace(BotToken); } }
            public static bool HasChatId { get { return !string.IsNullOrWhiteSpace(ChatId); } }

            /// <summary>Đủ điều kiện để gửi tin: đã bật, có token và có nơi nhận.</summary>
            public static bool IsConfigured { get { return Enabled && HasToken && HasChatId; } }

            /// <summary>Che bớt token khi hiển thị lên màn hình quản trị.</summary>
            public static string MaskedToken
            {
                get
                {
                    var token = BotToken;
                    if (string.IsNullOrWhiteSpace(token)) return "(chưa đặt)";

                    var colon = token.IndexOf(':');
                    var prefix = colon > 0 ? token.Substring(0, colon) : string.Empty;
                    var tail = token.Length >= 4 ? token.Substring(token.Length - 4) : string.Empty;
                    return string.Format("{0}:••••••••{1}", prefix, tail);
                }
            }
        }

        public static class Reminder
        {
            /// <summary>
            /// Cho phép lịch tự động gửi hay không.
            /// Đặt false ở máy phát triển để không bắn tin vào nhóm khi đang chạy thử;
            /// bản publish (Release) được chuyển thành true qua Web.Release.config.
            /// Nút gửi thủ công trên màn hình Thông báo không phụ thuộc cờ này.
            /// </summary>
            public static bool AutoSend { get { return GetBool("Reminder:AutoSend", false); } }

            /// <summary>Giờ gửi nhắc sáng thứ Hai (nhắc về tuần trước).</summary>
            public static int MondayHour { get { return Clamp(GetInt("Reminder:MondayHour", 8)); } }

            /// <summary>Giờ gửi nhắc chiều thứ Sáu (nhắc về tuần này).</summary>
            public static int FridayHour { get { return Clamp(GetInt("Reminder:FridayHour", 15)); } }

            /// <summary>Giờ gửi tổng hợp sáng thứ Bảy (báo cáo cho anh Tân, về tuần này).</summary>
            public static int SaturdayHour { get { return Clamp(GetInt("Reminder:SaturdayHour", 9)); } }

            public static string TriggerKey { get { return Get("Reminder:TriggerKey"); } }

            private static int Clamp(int hour)
            {
                return Math.Min(23, Math.Max(0, hour));
            }
        }
    }
}
