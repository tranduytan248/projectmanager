using System;
using System.Configuration;

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
