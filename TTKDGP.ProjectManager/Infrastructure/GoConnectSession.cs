using System;
using System.IO;
using System.Web.Hosting;
using Newtonsoft.Json.Linq;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>Cặp token đọc được từ phiên đăng nhập GoConnect đã lưu.</summary>
    public class GoConnectTokens
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        /// <summary>Hạn của access token, theo giờ máy. Null nếu không đọc được.</summary>
        public DateTime? AccessTokenExpiry { get; set; }

        /// <summary>Đơn vị của chính chủ tài khoản, dùng khi cấu hình chưa chỉ định đơn vị khác.</summary>
        public string OwnWorkplaceId { get; set; }

        public bool HasAccessToken { get { return !string.IsNullOrWhiteSpace(AccessToken); } }

        /// <summary>Còn dùng được: có token và chưa quá hạn (chừa 5 phút phòng lệch giờ).</summary>
        public bool IsUsable
        {
            get
            {
                if (!HasAccessToken) return false;
                if (!AccessTokenExpiry.HasValue) return true;
                return AccessTokenExpiry.Value > DateTime.Now.AddMinutes(5);
            }
        }
    }

    /// <summary>
    /// Đọc token từ phiên đăng nhập GoConnect mà Playwright lưu lại sau mỗi lần đăng nhập thành
    /// công (App_Data\goconnect_session.json). Đây là nguồn token đáng tin nhất: có cấu trúc sẵn,
    /// không phải bóc tách từ tin nhắn Telegram — tin Telegram chỉ để người đọc.
    ///
    /// Tên khoá lấy đúng theo localStorage của GoConnect: "token" là access token, "refToken"
    /// là refresh token (đã đối chiếu trên phiên thật).
    /// </summary>
    public static class GoConnectSession
    {
        private const string Origin = "goconnect.vnpt.vn";

        public static string FilePath()
        {
            var root = HostingEnvironment.IsHosted
                ? HostingEnvironment.MapPath("~/App_Data")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");

            return Path.Combine(root, "goconnect_session.json");
        }

        public static bool Exists()
        {
            return File.Exists(FilePath());
        }

        /// <summary>
        /// Đọc cặp token. Trả về null nếu chưa từng đăng nhập thành công hoặc file hỏng —
        /// người gọi phải xử lý null chứ không nên coi là lỗi hệ thống.
        /// </summary>
        public static GoConnectTokens Read()
        {
            var path = FilePath();
            if (!File.Exists(path)) return null;

            try
            {
                var root = JObject.Parse(File.ReadAllText(path));
                var origins = root["origins"] as JArray;
                if (origins == null) return null;

                foreach (var entry in origins)
                {
                    var name = (string)entry["origin"];
                    if (name == null || name.IndexOf(Origin, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    var items = entry["localStorage"] as JArray;
                    if (items == null) continue;

                    var tokens = new GoConnectTokens();

                    foreach (var item in items)
                    {
                        var key = (string)item["name"];
                        var value = (string)item["value"];
                        if (string.IsNullOrEmpty(key)) continue;

                        if (key == "token") tokens.AccessToken = value;
                        else if (key == "refToken") tokens.RefreshToken = value;
                        else if (key == "accessTokenExpiry") tokens.AccessTokenExpiry = ParseTime(value);
                        else if (key == "workplaceAct") tokens.OwnWorkplaceId = ReadWorkplaceId(value);
                    }

                    return tokens.HasAccessToken ? tokens : null;
                }
            }
            catch
            {
                // File chưa ghi xong hoặc đổi định dạng: coi như chưa có phiên.
            }

            return null;
        }

        private static DateTime? ParseTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            DateTime parsed;
            return DateTime.TryParse(value, out parsed) ? parsed.ToLocalTime() : (DateTime?)null;
        }

        private static string ReadWorkplaceId(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                return (string)JObject.Parse(json)["wpId"];
            }
            catch
            {
                return null;
            }
        }
    }
}
