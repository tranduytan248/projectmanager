using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Dịch vụ gửi Push Notification tới thiết bị di động nhân viên qua Firebase Cloud Messaging (FCM HTTP v1).
    ///
    /// Tự động đọc Service Account JSON từ App_Data/firebase-service-account.json, sinh OAuth2 token
    /// và gửi tin nhắn tới FcmDeviceToken mới nhất của tài khoản nhân viên.
    /// Toàn bộ quá trình chạy bất đồng bộ trong background thread, nuốt mọi lỗi để tuyệt đối không
    /// ảnh hưởng đến luồng nghiệp vụ chính.
    /// </summary>
    public static class FcmPushService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly object _tokenLock = new object();
        private static string _cachedAccessToken;
        private static DateTime _tokenExpiresAt = DateTime.MinValue;

        private const string DefaultProjectId = "brewtask-99719";

        /// <summary>
        /// Gửi Push Notification tới thiết bị đăng ký mới nhất của nhân viên (chạy ngầm, không chặn).
        /// </summary>
        public static void SendToUser(int userId, string title, string body, string type = "", int projectId = 0, int taskId = 0)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(body)) return;

            // Thực thi ngầm trong Background Work Item để trả về ngay cho request hiện tại
            HostingEnvironment.QueueBackgroundWorkItem(async ct =>
            {
                try
                {
                    await SendToUserInternalAsync(userId, title, body, type, projectId, taskId);
                }
                catch (Exception ex)
                {
                    LogWarning("Lỗi khi gửi FCM Push Notification: " + ex.Message);
                }
            });
        }

        private static async Task SendToUserInternalAsync(int userId, string title, string body, string type, int projectId, int taskId)
        {
            var user = Repository.Users.Find(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.FcmDeviceToken))
            {
                // Người dùng chưa có Device Token hoặc chưa đăng nhập trên mobile
                return;
            }

            var deviceToken = user.FcmDeviceToken.Trim();
            await SendDirectAsync(deviceToken, title, body, type, projectId, taskId);
        }

        /// <summary>
        /// Gửi trực tiếp tin nhắn Push Notification tới một Device Token cụ thể.
        /// </summary>
        public static async Task<bool> SendDirectAsync(string deviceToken, string title, string body, string type = "", int projectId = 0, int taskId = 0)
        {
            if (string.IsNullOrWhiteSpace(deviceToken)) return false;

            var serviceAccount = LoadServiceAccount();
            if (serviceAccount == null)
            {
                LogWarning("Chưa tìm thấy tệp cấu hình firebase-service-account.json trong App_Data. Không thể gửi FCM.");
                return false;
            }

            var projectIdStr = serviceAccount.ProjectId ?? DefaultProjectId;
            var accessToken = await GetAccessTokenAsync(serviceAccount);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                LogWarning("Không thể tạo OAuth2 Access Token từ Google Service Account.");
                return false;
            }

            var fcmUrl = string.Format("https://fcm.googleapis.com/v1/projects/{0}/messages:send", projectIdStr);

            var messageObj = new
            {
                message = new
                {
                    token = deviceToken.Trim(),
                    notification = new
                    {
                        title = string.IsNullOrWhiteSpace(title) ? "BrewTask" : title,
                        body = body
                    },
                    data = new Dictionary<string, string>
                    {
                        { "title", string.IsNullOrWhiteSpace(title) ? "BrewTask" : title },
                        { "body", body },
                        { "type", type ?? string.Empty },
                        { "projectId", projectId > 0 ? projectId.ToString() : string.Empty },
                        { "taskId", taskId > 0 ? taskId.ToString() : string.Empty },
                        { "click_action", "FLUTTER_NOTIFICATION_CLICK" }
                    },
                    android = new
                    {
                        priority = "high",
                        notification = new
                        {
                            channel_id = "brewtask_high_importance",
                            icon = "@mipmap/ic_launcher",
                            sound = "default"
                        }
                    }
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(messageObj);
            var request = new HttpRequestMessage(HttpMethod.Post, fcmUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var respBody = await response.Content.ReadAsStringAsync();
                LogWarning(string.Format("FCM HTTP v1 trả về mã lỗi {0}: {1}", (int)response.StatusCode, respBody));
                return false;
            }

            return true;
        }

        #region Google Service Account & OAuth2 JWT

        private class ServiceAccountInfo
        {
            [JsonProperty("project_id")]
            public string ProjectId { get; set; }

            [JsonProperty("client_email")]
            public string ClientEmail { get; set; }

            [JsonProperty("private_key")]
            public string PrivateKey { get; set; }

            [JsonProperty("token_uri")]
            public string TokenUri { get; set; }
        }

        private static ServiceAccountInfo LoadServiceAccount()
        {
            try
            {
                // 1. Kiểm tra Web.config AppSettings trực tiếp
                var jsonConfig = ConfigurationManager.AppSettings["FirebaseServiceAccountJson"];
                if (!string.IsNullOrWhiteSpace(jsonConfig))
                {
                    return JsonConvert.DeserializeObject<ServiceAccountInfo>(jsonConfig);
                }

                var customPath = ConfigurationManager.AppSettings["FirebaseServiceAccountPath"];
                if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
                {
                    var content = File.ReadAllText(customPath);
                    return JsonConvert.DeserializeObject<ServiceAccountInfo>(content);
                }

                // 2. Kiểm tra App_Data/firebase-service-account.json
                var appDataPath = HostingEnvironment.MapPath("~/App_Data/firebase-service-account.json");
                if (File.Exists(appDataPath))
                {
                    var content = File.ReadAllText(appDataPath);
                    return JsonConvert.DeserializeObject<ServiceAccountInfo>(content);
                }

                // 3. Kiểm tra App_Data/service-account.json
                var fallbackPath = HostingEnvironment.MapPath("~/App_Data/service-account.json");
                if (File.Exists(fallbackPath))
                {
                    var content = File.ReadAllText(fallbackPath);
                    return JsonConvert.DeserializeObject<ServiceAccountInfo>(content);
                }
            }
            catch (Exception ex)
            {
                LogWarning("Lỗi đọc tệp Service Account JSON: " + ex.Message);
            }
            return null;
        }

        private static async Task<string> GetAccessTokenAsync(ServiceAccountInfo sa)
        {
            lock (_tokenLock)
            {
                if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiresAt)
                {
                    return _cachedAccessToken;
                }
            }

            try
            {
                var jwt = CreateSignedJwt(sa);
                var tokenUri = string.IsNullOrWhiteSpace(sa.TokenUri) ? "https://oauth2.googleapis.com/token" : sa.TokenUri;

                var postData = new Dictionary<string, string>
                {
                    { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                    { "assertion", jwt }
                };

                var resp = await _httpClient.PostAsync(tokenUri, new FormUrlEncodedContent(postData));
                var respJson = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    LogWarning("OAuth2 token endpoint trả về lỗi: " + respJson);
                    return null;
                }

                var tokenResult = JObject.Parse(respJson);
                var accessToken = (string)tokenResult["access_token"];
                var expiresIn = (int?)tokenResult["expires_in"] ?? 3600;

                lock (_tokenLock)
                {
                    _cachedAccessToken = accessToken;
                    _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 300); // Làm mới trước 5 phút
                }

                return accessToken;
            }
            catch (Exception ex)
            {
                LogWarning("Lỗi lấy Google OAuth2 Access Token: " + ex.Message);
                return null;
            }
        }

        private static string CreateSignedJwt(ServiceAccountInfo sa)
        {
            var now = DateTimeOffset.UtcNow;
            var iat = now.ToUnixTimeSeconds();
            var exp = iat + 3600;

            var header = new { alg = "RS256", typ = "JWT" };
            var payload = new
            {
                iss = sa.ClientEmail,
                scope = "https://www.googleapis.com/auth/firebase.messaging",
                aud = string.IsNullOrWhiteSpace(sa.TokenUri) ? "https://oauth2.googleapis.com/token" : sa.TokenUri,
                exp = exp,
                iat = iat
            };

            var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(header)));
            var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
            var unsignedToken = headerBase64 + "." + payloadBase64;

            var rsa = CreateRsaFromPrivateKey(sa.PrivateKey);
            var signatureBytes = rsa.SignData(Encoding.UTF8.GetBytes(unsignedToken), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var signatureBase64 = Base64UrlEncode(signatureBytes);

            return unsignedToken + "." + signatureBase64;
        }

        private static RSA CreateRsaFromPrivateKey(string pemKey)
        {
            var keyStr = pemKey
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();

            var keyBytes = Convert.FromBase64String(keyStr);
            var cngKey = CngKey.Import(keyBytes, CngKeyBlobFormat.Pkcs8PrivateBlob);
            return new RSACng(cngKey);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Split('=')[0]; // Bỏ padding '='
            output = output.Replace('+', '-');
            output = output.Replace('/', '_');
            return output;
        }

        #endregion

        private static void LogWarning(string message)
        {
            try
            {
                var logDir = HostingEnvironment.MapPath("~/App_Data");
                if (Directory.Exists(logDir))
                {
                    var logFile = Path.Combine(logDir, "fcm.log");
                    File.AppendAllText(logFile, string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}\r\n", DateTime.Now, message));
                }
            }
            catch
            {
                // Bỏ qua nếu không ghi được log
            }
        }
    }
}
