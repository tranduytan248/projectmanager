using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Dịch vụ tải ảnh lên Firebase Storage (Google Cloud Storage) sử dụng Firebase Service Account.
    /// Giúp lưu trữ toàn bộ ảnh đính kèm/ảnh copy-paste trên đám mây Firebase, tối ưu dung lượng máy chủ
    /// và cơ sở dữ liệu. Có sẵn cơ chế tự động fallback lưu cục bộ nếu không thể kết nối Firebase.
    /// </summary>
    public static class FirebaseStorageService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _cachedAccessToken = null;
        private static DateTime _tokenExpiresAt = DateTime.MinValue;
        private static readonly object _tokenLock = new object();

        private class ServiceAccountInfo
        {
            [JsonProperty("project_id")]
            public string ProjectId { get; set; }

            [JsonProperty("private_key")]
            public string PrivateKey { get; set; }

            [JsonProperty("client_email")]
            public string ClientEmail { get; set; }

            [JsonProperty("token_uri")]
            public string TokenUri { get; set; }
        }

        /// <summary>
        /// Tải luồng dữ liệu ảnh lên Firebase Storage.
        /// </summary>
        /// <param name="fileStream">Luồng dữ liệu file ảnh</param>
        /// <param name="originalFileName">Tên file gốc</param>
        /// <param name="contentType">MIME type (ví dụ image/png, image/jpeg)</param>
        /// <returns>URL ảnh công khai từ Firebase hoặc đường dẫn local fallback</returns>
        public static async Task<string> UploadImageAsync(Stream fileStream, string originalFileName, string contentType)
        {
            if (fileStream == null || fileStream.Length == 0)
            {
                throw new ArgumentException("Dữ liệu file ảnh rỗng.", nameof(fileStream));
            }

            // Chuẩn hoá định dạng đuôi file và Content-Type
            var ext = Path.GetExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = GetExtensionFromContentType(contentType);
            }
            ext = ext.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = GetContentTypeFromExtension(ext);
            }

            var objectName = string.Format("task_images/{0:yyyyMM}/{1}{2}", DateTime.UtcNow, Guid.NewGuid().ToString("N"), ext);

            // Đọc dữ liệu ảnh vào mảng byte
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await fileStream.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            // Thử tải lên Firebase Storage
            try
            {
                var firebaseUrl = await TryUploadToFirebaseAsync(fileBytes, objectName, contentType);
                if (!string.IsNullOrWhiteSpace(firebaseUrl))
                {
                    return firebaseUrl;
                }
            }
            catch (Exception ex)
            {
                LogWarning("Lỗi khi tải ảnh lên Firebase Storage: " + ex.Message);
            }

            // Fallback lưu cục bộ tại máy chủ web nếu Firebase gặp sự cố
            return SaveLocalFallback(fileBytes, objectName);
        }

        private static async Task<string> TryUploadToFirebaseAsync(byte[] fileBytes, string objectName, string contentType)
        {
            var sa = LoadServiceAccount();
            if (sa == null)
            {
                LogWarning("Chưa tìm thấy cấu hình Service Account JSON để tải ảnh lên Firebase.");
                return null;
            }

            var bucket = ConfigurationManager.AppSettings["Firebase:StorageBucket"];
            if (string.IsNullOrWhiteSpace(bucket))
            {
                bucket = ConfigurationManager.AppSettings["FirebaseStorageBucket"];
            }
            if (string.IsNullOrWhiteSpace(bucket))
            {
                bucket = sa.ProjectId + ".firebasestorage.app";
            }

            var accessToken = await GetAccessTokenAsync(sa);
            if (string.IsNullOrEmpty(accessToken))
            {
                LogWarning("Không thể lấy Access Token từ Google OAuth2 cho Firebase Storage.");
                return null;
            }

            var downloadToken = Guid.NewGuid().ToString("D");

            // Gọi Firebase Storage Upload API
            var uploadUrl = string.Format(
                "https://firebasestorage.googleapis.com/v0/b/{0}/o?name={1}",
                Uri.EscapeDataString(bucket),
                Uri.EscapeDataString(objectName));

            using (var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("x-goog-meta-firebasestoragedownloadtokens", downloadToken);

                var content = new ByteArrayContent(fileBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                var respBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Trả về liên kết xem trực tiếp có kèm download token của Firebase Storage
                    var publicUrl = string.Format(
                        "https://firebasestorage.googleapis.com/v0/b/{0}/o/{1}?alt=media&token={2}",
                        Uri.EscapeDataString(bucket),
                        Uri.EscapeDataString(objectName),
                        downloadToken);

                    return publicUrl;
                }
                else
                {
                    LogWarning(string.Format("Firebase Storage API trả về HTTP {0}: {1}", (int)response.StatusCode, respBody));
                    
                    // Thử phương án 2: Google Cloud Storage standard JSON API
                    return await TryUploadViaGcsApiAsync(fileBytes, bucket, objectName, contentType, accessToken, downloadToken);
                }
            }
        }

        private static async Task<string> TryUploadViaGcsApiAsync(byte[] fileBytes, string bucket, string objectName, string contentType, string accessToken, string downloadToken)
        {
            try
            {
                var gcsUrl = string.Format(
                    "https://storage.googleapis.com/upload/storage/v1/b/{0}/o?uploadType=media&name={1}",
                    Uri.EscapeDataString(bucket),
                    Uri.EscapeDataString(objectName));

                using (var req = new HttpRequestMessage(HttpMethod.Post, gcsUrl))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    req.Headers.Add("x-goog-meta-firebasestoragedownloadtokens", downloadToken);

                    var content = new ByteArrayContent(fileBytes);
                    content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                    req.Content = content;

                    var resp = await _httpClient.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                    {
                        return string.Format(
                            "https://firebasestorage.googleapis.com/v0/b/{0}/o/{1}?alt=media&token={2}",
                            Uri.EscapeDataString(bucket),
                            Uri.EscapeDataString(objectName),
                            downloadToken);
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning("Lỗi GCS fallback: " + ex.Message);
            }
            return null;
        }

        private static string SaveLocalFallback(byte[] fileBytes, string objectName)
        {
            try
            {
                var relativePath = "/Uploads/" + objectName.Replace('/', '/');
                var physicalPath = HostingEnvironment.MapPath("~" + relativePath);
                var dir = Path.GetDirectoryName(physicalPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllBytes(physicalPath, fileBytes);
                return relativePath;
            }
            catch (Exception ex)
            {
                LogWarning("Lỗi lưu file local fallback: " + ex.Message);
                throw new InvalidOperationException("Không thể lưu file ảnh vào hệ thống.", ex);
            }
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
                    _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 300);
                }

                return accessToken;
            }
            catch (Exception ex)
            {
                LogWarning("Lỗi lấy Google OAuth2 Access Token cho Storage: " + ex.Message);
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
                scope = "https://www.googleapis.com/auth/devstorage.read_write https://www.googleapis.com/auth/cloud-platform",
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
            output = output.Split('=')[0];
            output = output.Replace('+', '-');
            output = output.Replace('/', '_');
            return output;
        }

        private static ServiceAccountInfo LoadServiceAccount()
        {
            try
            {
                var customPath = ConfigurationManager.AppSettings["FirebaseServiceAccountPath"];
                if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
                {
                    return JsonConvert.DeserializeObject<ServiceAccountInfo>(File.ReadAllText(customPath));
                }

                var appDataPath = HostingEnvironment.MapPath("~/App_Data/firebase-service-account.json");
                if (File.Exists(appDataPath))
                {
                    return JsonConvert.DeserializeObject<ServiceAccountInfo>(File.ReadAllText(appDataPath));
                }

                var fallbackPath = HostingEnvironment.MapPath("~/App_Data/service-account.json");
                if (File.Exists(fallbackPath))
                {
                    return JsonConvert.DeserializeObject<ServiceAccountInfo>(File.ReadAllText(fallbackPath));
                }
            }
            catch (Exception ex)
            {
                LogWarning("Lỗi đọc file Service Account: " + ex.Message);
            }
            return null;
        }

        private static string GetExtensionFromContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return ".png";
            switch (contentType.ToLowerInvariant().Trim())
            {
                case "image/jpeg":
                case "image/jpg":
                    return ".jpg";
                case "image/gif":
                    return ".gif";
                case "image/webp":
                    return ".webp";
                case "image/bmp":
                    return ".bmp";
                default:
                    return ".png";
            }
        }

        private static string GetContentTypeFromExtension(string ext)
        {
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".webp":
                    return "image/webp";
                case ".bmp":
                    return "image/bmp";
                default:
                    return "image/png";
            }
        }

        private static void LogWarning(string message)
        {
            try
            {
                var logDir = HostingEnvironment.MapPath("~/App_Data");
                if (Directory.Exists(logDir))
                {
                    var logFile = Path.Combine(logDir, "firebase_storage.log");
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
