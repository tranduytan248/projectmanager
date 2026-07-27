using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>Kết quả một lần gọi tổng đài SMS.</summary>
    public class SmsResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public string RawResponse { get; set; }

        public static SmsResult Success(string message, string raw)
        {
            return new SmsResult { Ok = true, Message = message, RawResponse = raw };
        }

        public static SmsResult Fail(string error, string raw = null)
        {
            return new SmsResult { Ok = false, Error = error, RawResponse = raw };
        }
    }

    /// <summary>
    /// Gửi tin nhắn qua tổng đài SMS (địa chỉ khai trong Sms:GatewayUrl). Gọi bằng GET với hai
    /// tham số phoneNums và smsContents, giống cách phần mềm HRM đang dùng. Chỉ dùng WebRequest
    /// sẵn có của .NET Framework, không thêm thư viện ngoài.
    /// </summary>
    public static class SmsClient
    {
        static SmsClient()
        {
            // Tổng đài chạy HTTPS và chỉ chấp nhận TLS 1.2 trở lên.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Chuẩn hoá danh sách số điện thoại về dạng tổng đài nhận (84xxxxxxxxx): tách theo dấu
        /// phẩy hoặc chấm phẩy, bỏ khoảng trắng và dấu nối, đổi đầu 0 hoặc +84 thành 84, bỏ số trùng.
        /// Số không đúng dạng được trả riêng qua <paramref name="invalid"/> để báo lại cho bên gọi.
        /// </summary>
        public static List<string> NormalizePhones(string raw, out List<string> invalid)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            invalid = new List<string>();

            if (string.IsNullOrWhiteSpace(raw)) return result;

            foreach (var piece in raw.Split(',', ';'))
            {
                var input = piece.Trim();
                if (input.Length == 0) continue;

                var normalized = Normalize(input);
                if (normalized == null)
                {
                    invalid.Add(input);
                    continue;
                }

                if (seen.Add(normalized)) result.Add(normalized);
            }

            return result;
        }

        private static string Normalize(string input)
        {
            var digits = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                if (c >= '0' && c <= '9') digits.Append(c);
                else if (c == '+' || c == ' ' || c == '.' || c == '-' || c == '(' || c == ')') continue;
                else return null;   // Có ký tự lạ thì coi như số sai, không đoán bừa.
            }

            var number = digits.ToString();

            // Số trong nước nhập kiểu 09xx / 03xx: bỏ số 0 đầu, thay bằng mã quốc gia.
            if (number.StartsWith("0", StringComparison.Ordinal)) number = "84" + number.Substring(1);
            else if (!number.StartsWith("84", StringComparison.Ordinal)) return null;

            // 84 + 9 chữ số (di động hiện nay) hoặc 84 + 10 chữ số (số cũ/cố định).
            return number.Length == 11 || number.Length == 12 ? number : null;
        }

        /// <summary>
        /// Gửi một nội dung tới nhiều số. Danh sách số phải đã chuẩn hoá bằng
        /// <see cref="NormalizePhones"/>.
        /// </summary>
        public static SmsResult Send(IEnumerable<string> phones, string contents)
        {
            if (!AppSettings.Sms.HasGateway) return SmsResult.Fail("Chưa cấu hình địa chỉ tổng đài SMS.");

            var list = string.Join(",", phones);
            if (string.IsNullOrWhiteSpace(list)) return SmsResult.Fail("Không có số điện thoại nào hợp lệ.");
            if (string.IsNullOrWhiteSpace(contents)) return SmsResult.Fail("Nội dung tin nhắn rỗng.");

            var url = string.Format("{0}{1}phoneNums={2}&smsContents={3}",
                AppSettings.Sms.GatewayUrl,
                AppSettings.Sms.GatewayUrl.IndexOf('?') >= 0 ? "&" : "?",
                Uri.EscapeDataString(list),
                Uri.EscapeDataString(contents));

            return Get(url, AppSettings.Sms.TimeoutSeconds * 1000);
        }

        private static SmsResult Get(string url, int timeoutMs)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Accept = "application/json";
                request.Timeout = timeoutMs;
                request.ReadWriteTimeout = timeoutMs;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream() ?? Stream.Null, Encoding.UTF8))
                {
                    var raw = reader.ReadToEnd();
                    return SmsResult.Success(DescribeResponse(raw), raw);
                }
            }
            catch (WebException ex)
            {
                var raw = ReadErrorBody(ex);
                var described = DescribeResponse(raw);
                return SmsResult.Fail(string.IsNullOrWhiteSpace(described) ? ex.Message : described, raw);
            }
            catch (Exception ex)
            {
                return SmsResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Tổng đài trả JSON dạng {"Data":"Gửi tin nhắn thành công"}. Lấy nội dung trường Data
        /// làm thông báo; không đọc được thì trả nguyên văn phản hồi.
        /// </summary>
        private static string DescribeResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            try
            {
                var data = JObject.Parse(raw)["Data"];
                if (data != null && data.Type != JTokenType.Null)
                {
                    var text = data.ToString();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }
            catch
            {
                // Không phải JSON thì dùng nguyên văn bên dưới.
            }

            return raw.Trim();
        }

        private static string ReadErrorBody(WebException ex)
        {
            if (ex.Response == null) return null;

            try
            {
                using (var reader = new StreamReader(ex.Response.GetResponseStream() ?? Stream.Null, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
