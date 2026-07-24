using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Gọi API danh sách nhân sự của GoConnect. Cặp token lấy được từ phiên đăng nhập tự động
    /// (xem GoConnectAutoLogin) nên có hạn dùng ngắn: hết hạn thì phải đăng nhập lại chứ không
    /// tự gia hạn ở đây.
    ///
    /// Chỉ dùng WebRequest sẵn có của .NET Framework và Newtonsoft.Json — hai thứ toàn bộ dự án
    /// đã dùng — để không phải tham chiếu thêm assembly nào. Gọi đồng bộ cho khớp với phần còn
    /// lại của tầng dữ liệu, và vì Timeout của HttpWebRequest chỉ có tác dụng ở lối gọi đồng bộ.
    /// </summary>
    public class GoConnectService
    {
        private readonly string _accessToken;
        private readonly string _refreshToken;

        private const string Url =
            "https://goconnect.vnpt.vn/nestjs/api/v1/permission/users/getListUsersBelongWorkplacesPaging";

        /// <summary>Số bản ghi mỗi lần gọi, theo giới hạn của API.</summary>
        private const int PageSize = 50;

        /// <summary>
        /// Trần số trang, để một giá trị tổng số sai từ máy chủ không kéo vòng lặp chạy mãi.
        /// 200 trang ≈ 10.000 nhân sự, dư cho quy mô hiện tại.
        /// </summary>
        private const int MaxPages = 200;

        private const int TimeoutMs = 30000;

        static GoConnectService()
        {
            // GoConnect chỉ chấp nhận TLS 1.2 trở lên, giống Telegram; .NET Framework 4.8 mặc định
            // có thể vẫn dùng giao thức cũ tuỳ cấu hình máy chủ.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public GoConnectService(string accessToken, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Thiếu access token của GoConnect.", "accessToken");

            _accessToken = accessToken.Trim();
            _refreshToken = string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken.Trim();
        }

        /// <summary>
        /// Tải toàn bộ nhân sự thuộc một đơn vị, đi lần lượt từng trang cho tới hết.
        /// <paramref name="progress"/> dùng để báo tiến độ ra ngoài (có thể null).
        /// </summary>
        public List<UserInfo> GetAllUsers(string wpId, Action<string> progress = null)
        {
            if (string.IsNullOrWhiteSpace(wpId))
                throw new ArgumentException("Thiếu mã đơn vị (wpId).", "wpId");

            var result = new List<UserInfo>();

            // Chưa biết tổng số trang cho tới khi đọc xong trang đầu.
            var totalPage = MaxPages;

            for (var page = 1; page <= totalPage && page <= MaxPages; page++)
            {
                var data = FetchPage(wpId, page);
                var items = data.Data != null ? data.Data.Items : null;

                if (items == null || items.Count == 0) break;

                result.AddRange(items);

                if (page == 1)
                {
                    // Tổng số bản ghi được máy chủ nhét vào từng dòng dưới dạng chuỗi. Nếu đọc
                    // không ra thì không dừng ở đây: cứ đi tiếp và dựa vào trang thiếu (dưới) để
                    // biết đã hết, tránh mất dữ liệu chỉ vì một trường phụ bị đổi kiểu.
                    int total;
                    if (int.TryParse(items.First().TotalCount, out total) && total > 0)
                        totalPage = Math.Min(MaxPages, (int)Math.Ceiling(total / (double)PageSize));
                }

                if (progress != null)
                    progress(string.Format("Đã tải trang {0}/{1} ({2} người).", page, totalPage, result.Count));

                // Trang thiếu so với cỡ trang nghĩa là đã hết dữ liệu. Chốt dừng này cần thiết cho
                // trường hợp tổng số đọc không ra, và cũng cắt sớm khi máy chủ trả ít hơn khai báo.
                if (items.Count < PageSize) break;
            }

            return result;
        }

        /// <summary>Gọi một trang và trả về payload đã đọc, ném lỗi có nội dung rõ ràng khi hỏng.</summary>
        private GoConnectResponse FetchPage(string wpId, int page)
        {
            var payload = JsonConvert.SerializeObject(new UserRequest
            {
                wpId = wpId,
                page = page,
                limit = PageSize
            });

            var body = Send(payload, page);

            GoConnectResponse data;
            try
            {
                data = JsonConvert.DeserializeObject<GoConnectResponse>(body);
            }
            catch (JsonException ex)
            {
                throw new GoConnectApiException(string.Format(
                    "Không đọc được dữ liệu GoConnect ở trang {0}: {1}. {2}", page, ex.Message, Shorten(body)));
            }

            if (data == null)
                throw new GoConnectApiException(string.Format("GoConnect trả về rỗng ở trang {0}.", page));

            // Máy chủ có thể trả HTTP 200 kèm cờ báo lỗi trong thân tin; không xét thì
            // dữ liệu rỗng sẽ bị hiểu nhầm là "đơn vị không có ai".
            if (!data.Success)
                throw new GoConnectApiException(string.Format(
                    "GoConnect từ chối yêu cầu ở trang {0} (mã {1}): {2}",
                    page, data.Code, string.IsNullOrWhiteSpace(data.Msg) ? "(không có mô tả)" : data.Msg));

            return data;
        }

        /// <summary>Gửi một yêu cầu POST và trả về thân phản hồi dạng chuỗi.</summary>
        private string Send(string json, int page)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "POST";
                request.Timeout = TimeoutMs;
                request.ReadWriteTimeout = TimeoutMs;
                request.Accept = "application/json";
                request.ContentType = "application/json; charset=utf-8";

                // Cổng của VNPT chặn yêu cầu không khai báo trình duyệt, nên vẫn phải gửi UserAgent.
                request.UserAgent = "TTKDGP.ProjectManager";

                request.Headers["Authorization"] = "Bearer " + _accessToken;
                if (_refreshToken != null) request.Headers["Refresh-Token"] = _refreshToken;
                request.Headers["X-Custom-Timeout"] = TimeoutMs.ToString();

                var data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return ReadBody(response);
                }
            }
            catch (WebException ex)
            {
                throw Describe(ex, page);
            }
        }

        /// <summary>
        /// Dựng lỗi có nội dung đọc được từ một WebException. Phân biệt hết hạn token với lỗi khác,
        /// vì cách xử lý hoàn toàn khác nhau: một bên phải đăng nhập lại, một bên là sự cố máy chủ.
        /// </summary>
        private static GoConnectApiException Describe(WebException ex, int page)
        {
            using (var response = ex.Response as HttpWebResponse)
            {
                if (response == null)
                    return new GoConnectApiException(string.Format(
                        "Không gọi được GoConnect ở trang {0}: {1}", page, ex.Message));

                var raw = ReadBody(response);

                if (response.StatusCode == HttpStatusCode.Unauthorized
                    || response.StatusCode == HttpStatusCode.Forbidden)
                    return new GoConnectAuthException(
                        "Token GoConnect đã hết hạn hoặc không đủ quyền, cần đăng nhập lại.");

                return new GoConnectApiException(string.Format(
                    "GoConnect trả lỗi {0} ở trang {1}. {2}", (int)response.StatusCode, page, Shorten(raw)));
            }
        }

        private static string ReadBody(HttpWebResponse response)
        {
            try
            {
                using (var reader = new StreamReader(response.GetResponseStream() ?? Stream.Null, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Cắt ngắn thân tin lỗi để thông báo không bị tràn bởi cả trang HTML.</summary>
        private static string Shorten(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "(không có nội dung trả về)";
            body = body.Trim();
            return body.Length <= 300 ? body : body.Substring(0, 300) + "...";
        }
    }

    /// <summary>Lỗi do GoConnect trả về khi gọi API.</summary>
    public class GoConnectApiException : Exception
    {
        public GoConnectApiException(string message) : base(message)
        {
        }
    }

    /// <summary>Token hết hạn hoặc không đủ quyền — người gọi cần chạy lại phiên đăng nhập.</summary>
    public class GoConnectAuthException : GoConnectApiException
    {
        public GoConnectAuthException(string message) : base(message)
        {
        }
    }

    #region Request

    public class UserRequest
    {
        public string wpId { get; set; }

        public int page { get; set; }

        public int limit { get; set; }

        public string search { get; set; }

        public UserFilter filter { get; set; }

        public UserRequest()
        {
            search = "";
            filter = new UserFilter();
        }
    }

    public class UserFilter
    {
        public string userfullname { get; set; }

        public string code { get; set; }

        public string phonenumber { get; set; }

        public string email { get; set; }

        public string gender { get; set; }

        public string posId { get; set; }

        public UserFilter()
        {
            userfullname = "";
            code = "";
            phonenumber = "";
            email = "";
            gender = "";
            posId = "";
        }
    }

    #endregion

    #region Response

    public class GoConnectResponse
    {
        public int Code { get; set; }

        public bool Success { get; set; }

        public string Msg { get; set; }

        public GoConnectData Data { get; set; }
    }

    public class GoConnectData
    {
        public List<UserInfo> Items { get; set; }
    }

    public class UserInfo
    {
        public string UserId { get; set; }

        public string UserCode { get; set; }

        public string UserFirstname { get; set; }

        public string UserMiddlename { get; set; }

        public string UserLastname { get; set; }

        public string UserFullname { get; set; }

        public string UserEmail { get; set; }

        public string UserGender { get; set; }

        public string UserPhonenumber { get; set; }

        public int UserActive { get; set; }

        public DateTime? UserBirthday { get; set; }

        public string UserAvatar { get; set; }

        public string UserAvatarThumb { get; set; }

        public int UserHiddenBirthday { get; set; }

        public int UserBirthdayNotification { get; set; }

        public string WpId { get; set; }

        public string WpCode { get; set; }

        public string WpName { get; set; }

        public int? WpOrder { get; set; }

        public string PosId { get; set; }

        public string PosCode { get; set; }

        public string PosName { get; set; }

        public int? PosOrder { get; set; }

        public string OnlineStatus { get; set; }

        public DateTime? LastActive { get; set; }

        public string TotalCount { get; set; }

        public bool Online { get; set; }

        public List<Workplace> Workplaces { get; set; }

        public List<object> UserIdentifiers { get; set; }
    }

    public class Workplace
    {
        public string WpId { get; set; }

        public string WpCode { get; set; }

        public string WpName { get; set; }

        public string WpParent { get; set; }

        public int? WpOrder { get; set; }

        public string Level { get; set; }
    }

    #endregion
}
