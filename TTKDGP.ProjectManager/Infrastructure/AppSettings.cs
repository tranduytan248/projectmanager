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

        /// <summary>Cấu hình cho màn hình báo cáo công việc cá nhân.</summary>
        public static class Reporter
        {
            /// <summary>
            /// Chỉ những phân công ở các trạng thái này mới được người phụ trách báo cáo tuần.
            /// Khai trong cấu hình để đổi được mà không phải sửa mã nguồn.
            /// </summary>
            public static List<string> ReportableWorkStatuses
            {
                get { return GetList("Reporter:WorkStatuses", "Đang thực hiện, Đang hỗ trợ"); }
            }
        }

        /// <summary>Cấu hình cho bộ quản lý công việc &amp; KPI.</summary>
        public static class Work
        {
            /// <summary>
            /// Đơn vị được phép lấy nhân sự sang bộ quản lý công việc. Màn "Lấy từ HRM" chỉ hiện
            /// người thuộc đơn vị này và TOÀN BỘ đơn vị con bên dưới — nhân sự thật nằm ở phòng/tổ
            /// sâu vài cấp, lọc phẳng theo đúng một đơn vị sẽ không ra ai.
            ///
            /// Nhận cả MÃ lẫn TÊN đơn vị. Nên khai bằng mã (239.603 = Trung tâm Kinh doanh Giải
            /// pháp) vì mã không đổi, còn tên có thể đổi hoặc lệch dấu.
            /// </summary>
            public static string HrmWorkplace
            {
                get { return Get("Work:HrmWorkplace", "239.603"); }
            }
        }

        /// <summary>Địa chỉ người dùng truy cập hệ thống, đính kèm cuối mỗi tin nhắc.</summary>
        public static string PublicUrl { get { return Get("App:PublicUrl", "pmncpt.cenit.vn"); } }

        /// <summary>
        /// Kết nối SQL Server — nơi lưu toàn bộ dữ liệu nghiệp vụ.
        /// Mật khẩu nằm trong App_Config\secrets.config, không đưa vào kho mã nguồn.
        /// </summary>
        public static class Db
        {
            public static string Server { get { return Get("Db:Server"); } }
            public static string Database { get { return Get("Db:Name"); } }
            public static string User { get { return Get("Db:User"); } }
            public static string Password { get { return Get("Db:Password"); } }

            /// <summary>Đăng nhập bằng tài khoản Windows thay cho user/mật khẩu SQL.</summary>
            public static bool IntegratedSecurity { get { return GetBool("Db:IntegratedSecurity", false); } }

            public static bool HasServer { get { return !string.IsNullOrWhiteSpace(Server); } }
        }

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
            public static string MaskedToken { get { return Mask(BotToken); } }
        }

        /// <summary>
        /// Che token bot khi đưa lên màn hình: giữ lại phần id ở đầu (phần trước dấu hai chấm,
        /// vốn công khai và đủ để phân biệt hai bot) và bốn ký tự cuối để đối chiếu nhanh.
        /// </summary>
        private static string Mask(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return "(chưa đặt)";

            var colon = token.IndexOf(':');
            var prefix = colon > 0 ? token.Substring(0, colon) : string.Empty;
            var tail = token.Length >= 4 ? token.Substring(token.Length - 4) : string.Empty;
            return string.Format("{0}:••••••••{1}", prefix, tail);
        }

        /// <summary>
        /// Máy chủ SMTP dùng để gửi mail nhắc báo cáo cho từng thành viên.
        /// Mật khẩu nằm trong App_Config\secrets.config, không đưa vào kho mã nguồn.
        /// </summary>
        public static class Email
        {
            public static bool Enabled { get { return GetBool("Email:Enabled", false); } }
            public static string Host { get { return Get("Email:Host"); } }
            public static int Port { get { return GetInt("Email:Port", 587); } }
            public static string User { get { return Get("Email:User"); } }
            public static string Password { get { return Get("Email:Password"); } }
            public static bool EnableSsl { get { return GetBool("Email:EnableSsl", true); } }

            /// <summary>Địa chỉ hiện ở ô "Người gửi"; mặc định lấy chính tài khoản đăng nhập SMTP.</summary>
            public static string From { get { return Get("Email:From", User); } }

            /// <summary>Tên hiển thị kèm địa chỉ người gửi.</summary>
            public static string DisplayName
            {
                get { return Get("Email:DisplayName", "Hệ thống quản lý dự án của Nghiên Cứu Phát Triển"); }
            }

            public static bool HasHost { get { return !string.IsNullOrWhiteSpace(Host); } }
            public static bool HasAccount
            {
                get { return !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password); }
            }

            /// <summary>Đủ điều kiện để gửi mail: đã bật, có máy chủ và có tài khoản.</summary>
            public static bool IsConfigured { get { return Enabled && HasHost && HasAccount; } }

            /// <summary>Che mật khẩu khi hiển thị lên màn hình quản trị.</summary>
            public static string MaskedPassword
            {
                get { return string.IsNullOrWhiteSpace(Password) ? "(chưa đặt)" : "••••••••"; }
            }
        }

        /// <summary>
        /// Tổng đài gửi tin nhắn SMS. Phần mềm không tự nối vào nhà mạng mà gọi lại tổng đài
        /// dùng chung (cùng đường mà HRM đang gọi), nên chỉ cần khai địa chỉ endpoint.
        /// </summary>
        public static class Sms
        {
            /// <summary>Bật/tắt API gửi SMS. Tắt thì endpoint trả lỗi ngay, không gọi ra ngoài.</summary>
            public static bool Enabled { get { return GetBool("Sms:Enabled", false); } }

            /// <summary>Địa chỉ tổng đài, nhận GET với hai tham số phoneNums và smsContents.</summary>
            public static string GatewayUrl
            {
                get { return Get("Sms:GatewayUrl", "https://vnptkhanhhoa.cenit.vn/api/Util/SendSms"); }
            }

            /// <summary>Thời gian chờ tổng đài trả lời (giây).</summary>
            public static int TimeoutSeconds { get { return Math.Max(5, GetInt("Sms:TimeoutSeconds", 30)); } }

            /// <summary>Trần số máy nhận trong một lần gọi, chặn việc bắn hàng loạt do gọi nhầm.</summary>
            public static int MaxPhones { get { return Math.Max(1, GetInt("Sms:MaxPhones", 50)); } }

            public static bool HasGateway { get { return !string.IsNullOrWhiteSpace(GatewayUrl); } }

            /// <summary>Đủ điều kiện gửi: đã bật và có địa chỉ tổng đài.</summary>
            public static bool IsConfigured { get { return Enabled && HasGateway; } }
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

            /// <summary>Giờ gửi mail riêng cho từng thành viên (sáng thứ Sáu, về tuần này).</summary>
            public static int MemberEmailHour { get { return Clamp(GetInt("Reminder:MemberEmailHour", 8)); } }

            public static string TriggerKey { get { return Get("Reminder:TriggerKey"); } }

            private static int Clamp(int hour)
            {
                return Math.Min(23, Math.Max(0, hour));
            }
        }

        /// <summary>
        /// Tự động đăng nhập cổng VNPT GoConnect bằng số điện thoại của chính chủ tài khoản.
        /// Mỗi sáng tới giờ hẹn, hệ thống mở trình duyệt nền, bấm gửi OTP rồi nhắn qua Telegram
        /// để chủ tài khoản trả lời mã OTP; nhập xong sẽ lưu phiên đăng nhập lại để dùng tiếp.
        ///
        /// Cơ chế chỉ nghe đúng một chat id (của chủ tài khoản) và chỉ dùng một số điện thoại
        /// mặc định, không nhận số của người khác.
        /// </summary>
        public static class GoConnect
        {
            /// <summary>Bật/tắt toàn bộ tính năng. Để false ở máy phát triển khi chưa muốn chạy.</summary>
            public static bool Enabled { get { return GetBool("GoConnect:Enabled", false); } }

            /// <summary>
            /// Địa chỉ IP của máy được phép chạy /hrm (máy local có Chromium, phục vụ pm.tdt.vn).
            /// Máy nào mang IP này thì đóng vai "máy đăng nhập"; máy khác (bản chạy thật) chỉ chạy
            /// /dongbo. Cùng một bản build thả lên máy nào cũng tự nhận vai theo IP, không cần
            /// cấu hình riêng. Đổi máy thì sửa khoá này, khỏi build lại.
            /// </summary>
            public static string LoginMachineIp { get { return Get("GoConnect:LoginMachineIp", "10.57.33.84"); } }

            // IP máy không đổi khi ứng dụng đang chạy nên tính một lần rồi nhớ lại.
            private static bool? _isLoginMachine;

            /// <summary>
            /// Máy đang chạy có phải "máy đăng nhập" (mang <see cref="LoginMachineIp"/>) không.
            /// Quyết định: được chạy /hrm không, nghe bot nào, và hiện tên miền nào ở footer.
            /// </summary>
            public static bool IsLoginMachine
            {
                get
                {
                    if (_isLoginMachine.HasValue) return _isLoginMachine.Value;
                    _isLoginMachine = HasLocalIp(LoginMachineIp);
                    return _isLoginMachine.Value;
                }
            }

            private static bool HasLocalIp(string target)
            {
                if (string.IsNullOrWhiteSpace(target)) return false;

                try
                {
                    foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;

                        foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                                && addr.Address.ToString() == target)
                                return true;
                        }
                    }
                }
                catch
                {
                    // Không đọc được danh sách IP thì coi như không phải máy đăng nhập (an toàn hơn).
                }

                return false;
            }

            /// <summary>
            /// Token bot Telegram RIÊNG cho việc đăng nhập GoConnect (đặt trong secrets.config).
            /// Để trống thì lùi về token bot nhắc báo cáo.
            /// </summary>
            public static string BotToken
            {
                get
                {
                    var token = Get("GoConnect:BotToken");
                    return string.IsNullOrWhiteSpace(token) ? Telegram.BotToken : token;
                }
            }

            /// <summary>
            /// Bot mà máy này thực sự lắng nghe, quyết định theo vai trò để hai máy không giành
            /// tin của nhau (mỗi bot chỉ một máy đọc, tránh lỗi 409 của Telegram):
            ///   • Máy đăng nhập  → bot GoConnect (@HRM_KHA_bot) cho /hrm.
            ///   • Máy chạy thật  → bot nhắc báo cáo (@Imu_Luffy_bot) cho /dongbo.
            /// </summary>
            public static string ActiveBotToken
            {
                get { return IsLoginMachine ? BotToken : Telegram.BotToken; }
            }

            public static bool HasToken { get { return !string.IsNullOrWhiteSpace(ActiveBotToken); } }

            /// <summary>Che bớt token khi hiển thị lên màn hình quản trị.</summary>
            public static string MaskedToken { get { return Mask(BotToken); } }

            /// <summary>
            /// Đang dùng chung bot với phần nhắc báo cáo hay không — tức chưa khai báo
            /// GoConnect:BotToken riêng nên phải lùi về Telegram:BotToken. Dùng chung thì tin
            /// nhắc báo cáo và tin hỏi OTP cùng đi ra từ một bot, cần cảnh báo cho người quản trị.
            /// </summary>
            public static bool SharesBotWithReminder
            {
                get { return HasToken && string.Equals(BotToken, Telegram.BotToken, StringComparison.Ordinal); }
            }

            /// <summary>Tương tự với nơi nhận: dùng chung chat id nghĩa là OTP bắn vào nhóm.</summary>
            public static bool SharesChatIdWithReminder
            {
                get { return HasChatId && string.Equals(ChatId, Telegram.ChatId, StringComparison.Ordinal); }
            }

            /// <summary>
            /// Địa chỉ hiện trong footer của bot GoConnect, chọn theo vai trò máy:
            ///   • Máy đăng nhập (pm.tdt.vn) → GoConnect:PublicUrl.
            ///   • Máy chạy thật            → App:PublicUrl (pmncpt.cenit.vn) như phần nhắc báo cáo.
            /// Nhờ vậy /hrm (chạy ở máy đăng nhập) hiện pm.tdt.vn, còn /dongbo (chạy ở bản thật)
            /// hiện pmncpt.cenit.vn — đúng địa chỉ nơi lệnh thực sự chạy.
            /// </summary>
            public static string PublicUrl
            {
                get
                {
                    if (!IsLoginMachine) return AppSettings.PublicUrl;

                    var url = Get("GoConnect:PublicUrl");
                    return string.IsNullOrWhiteSpace(url) ? AppSettings.PublicUrl : url;
                }
            }

            /// <summary>Số điện thoại mặc định dùng cho lần đăng nhập tự động lúc sáng.</summary>
            public static string DefaultPhone { get { return Get("GoConnect:DefaultPhone"); } }

            /// <summary>
            /// Chat id Telegram của chủ tài khoản — nơi bot gửi yêu cầu nhập OTP và nhận mã trả về.
            /// Thường là chat riêng (id dương), khác với nhóm nhắc báo cáo. Nếu để trống thì
            /// dùng tạm Telegram:ChatId.
            /// </summary>
            public static string ChatId
            {
                get
                {
                    var id = Get("GoConnect:ChatId");
                    return string.IsNullOrWhiteSpace(id) ? Telegram.ChatId : id;
                }
            }

            public static bool HasChatId { get { return !string.IsNullOrWhiteSpace(ChatId); } }

            /// <summary>
            /// Đơn vị cần lấy danh sách nhân sự khi đồng bộ. Phải là đơn vị cấp trên chứa toàn
            /// bộ người cần lấy, không phải đơn vị riêng của chủ tài khoản. Để trống thì lùi về
            /// đơn vị của chính chủ tài khoản đọc từ phiên đăng nhập.
            /// </summary>
            public static string WorkplaceId { get { return Get("GoConnect:WorkplaceId"); } }

            /// <summary>Trang đăng nhập của GoConnect.</summary>
            public static string LoginUrl { get { return Get("GoConnect:LoginUrl", "https://goconnect.vnpt.vn/login"); } }

            /// <summary>Mẫu URL nhận biết đã vào tới màn làm việc (đăng nhập thành công).</summary>
            public static string WorkspaceUrlPattern { get { return Get("GoConnect:WorkspaceUrlPattern", "**/home/workspace**"); } }

            /// <summary>Giờ/phút đăng nhập tự động mỗi ngày (mặc định 07:30).</summary>
            public static int AutoLoginHour { get { return Math.Min(23, Math.Max(0, GetInt("GoConnect:AutoLoginHour", 7))); } }
            public static int AutoLoginMinute { get { return Math.Min(59, Math.Max(0, GetInt("GoConnect:AutoLoginMinute", 30))); } }

            /// <summary>Chạy trình duyệt ẩn (headless). Đặt false khi cần xem tận mắt lúc gỡ lỗi.</summary>
            public static bool Headless { get { return GetBool("GoConnect:Headless", true); } }

            /// <summary>
            /// Sau khi đăng nhập, trích token truy cập (từ localStorage/cookie của chính tài khoản)
            /// và gửi lên Telegram để dùng gọi API. Token là thông tin nhạy cảm — chỉ gửi vào
            /// đúng chat riêng của chủ tài khoản.
            /// </summary>
            public static bool SendToken { get { return GetBool("GoConnect:SendToken", false); } }

            /// <summary>Thời gian chờ chủ tài khoản trả lời OTP trước khi huỷ phiên (giây).</summary>
            public static int OtpTimeoutSeconds { get { return Math.Max(30, GetInt("GoConnect:OtpTimeoutSeconds", 180)); } }

            /// <summary>Đủ điều kiện chạy: đã bật, có token bot và có chat id người nhận.</summary>
            public static bool IsConfigured
            {
                get { return Enabled && HasToken && HasChatId; }
            }
        }
    }
}
