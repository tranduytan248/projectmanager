using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>Kết quả một lần gọi API Telegram.</summary>
    public class TelegramResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public string RawResponse { get; set; }

        public static TelegramResult Success(string raw)
        {
            return new TelegramResult { Ok = true, RawResponse = raw };
        }

        public static TelegramResult Fail(string error, string raw = null)
        {
            return new TelegramResult { Ok = false, Error = error, RawResponse = raw };
        }
    }

    /// <summary>Một cuộc trò chuyện tìm thấy khi dò chat id.</summary>
    public class TelegramChat
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
    }

    /// <summary>Một tin nhắn đến, rút gọn từ cập nhật của Telegram.</summary>
    public class TelegramUpdate
    {
        public long UpdateId { get; set; }
        public string ChatId { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// Gọi Bot API của Telegram. Chỉ dùng WebRequest sẵn có của .NET Framework,
    /// không thêm thư viện ngoài.
    /// </summary>
    public static class TelegramClient
    {
        private const string ApiBase = "https://api.telegram.org/bot";

        static TelegramClient()
        {
            // Telegram chỉ chấp nhận TLS 1.2 trở lên; .NET Framework 4.8 mặc định
            // có thể vẫn dùng giao thức cũ tuỳ cấu hình máy chủ.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        /// <summary>Telegram giới hạn 4096 ký tự mỗi tin nhắn.</summary>
        private const int MaxMessageLength = 4096;

        /// <summary>
        /// Gửi tin nhắn tới một cuộc trò chuyện. Tin dài hơn giới hạn của Telegram
        /// được cắt thành nhiều phần, cắt ở ranh giới dòng để không vỡ danh sách.
        /// </summary>
        public static TelegramResult SendMessage(string botToken, string chatId, string text)
        {
            if (string.IsNullOrWhiteSpace(botToken)) return TelegramResult.Fail("Chưa cấu hình token bot.");
            if (string.IsNullOrWhiteSpace(chatId)) return TelegramResult.Fail("Chưa cấu hình chat id của nhóm.");
            if (string.IsNullOrWhiteSpace(text)) return TelegramResult.Fail("Nội dung tin nhắn rỗng.");

            TelegramResult last = null;

            foreach (var part in SplitMessage(text))
            {
                var payload = new JObject
                {
                    { "chat_id", chatId },
                    { "text", part },
                    { "parse_mode", "HTML" },
                    { "disable_web_page_preview", true }
                };

                last = Post(botToken, "sendMessage", payload.ToString());
                if (!last.Ok) return last;   // Lỗi giữa chừng thì dừng, không gửi tiếp các phần sau
            }

            return last;
        }

        /// <summary>Cắt tin dài theo ranh giới dòng, mỗi phần không vượt giới hạn của Telegram.</summary>
        private static List<string> SplitMessage(string text)
        {
            var parts = new List<string>();

            if (text.Length <= MaxMessageLength)
            {
                parts.Add(text);
                return parts;
            }

            var current = new StringBuilder();

            foreach (var line in text.Split('\n'))
            {
                // Cộng thêm 1 cho ký tự xuống dòng sẽ nối vào.
                if (current.Length > 0 && current.Length + line.Length + 1 > MaxMessageLength)
                {
                    parts.Add(current.ToString().TrimEnd());
                    current.Clear();
                }

                if (current.Length > 0) current.Append('\n');
                current.Append(line);
            }

            if (current.Length > 0) parts.Add(current.ToString().TrimEnd());
            return parts;
        }

        /// <summary>Kiểm tra token có hợp lệ không, trả về tên bot.</summary>
        public static TelegramResult GetMe(string botToken)
        {
            if (string.IsNullOrWhiteSpace(botToken)) return TelegramResult.Fail("Chưa cấu hình token bot.");
            return Get(botToken, "getMe");
        }

        /// <summary>
        /// Dò các cuộc trò chuyện mà bot đang tham gia, dựa trên những cập nhật gần đây.
        /// Telegram chỉ giữ update trong khoảng 24 giờ, nên cần có hoạt động mới trong nhóm.
        /// </summary>
        public static List<TelegramChat> DiscoverChats(string botToken, out TelegramResult result)
        {
            var chats = new List<TelegramChat>();
            result = Get(botToken, "getUpdates");
            if (!result.Ok) return chats;

            var seen = new HashSet<string>(StringComparer.Ordinal);

            try
            {
                var root = JObject.Parse(result.RawResponse);
                var updates = root["result"] as JArray;
                if (updates == null) return chats;

                foreach (var update in updates)
                {
                    foreach (var key in new[] { "message", "my_chat_member", "channel_post", "edited_message" })
                    {
                        var chat = update[key] != null ? update[key]["chat"] : null;
                        if (chat == null) continue;

                        var id = (string)chat["id"];
                        if (string.IsNullOrEmpty(id) || !seen.Add(id)) continue;

                        chats.Add(new TelegramChat
                        {
                            Id = id,
                            Type = (string)chat["type"],
                            Title = (string)chat["title"] ?? (string)chat["username"] ?? (string)chat["first_name"]
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                result = TelegramResult.Fail("Không đọc được phản hồi từ Telegram: " + ex.Message, result.RawResponse);
            }

            return chats;
        }

        /// <summary>
        /// Đọc các tin nhắn mới gửi tới bot theo kiểu long-poll. offset là id cập nhật kế tiếp
        /// cần lấy (đã xử lý tới đâu thì truyền tới đó để Telegram không trả lại tin cũ).
        /// Chỉ lấy loại "message" cho nhẹ. Trả về danh sách rút gọn (id, chat, nội dung).
        /// </summary>
        public static List<TelegramUpdate> GetUpdates(string botToken, long offset, int longPollSeconds, out TelegramResult result)
        {
            var list = new List<TelegramUpdate>();

            if (string.IsNullOrWhiteSpace(botToken))
            {
                result = TelegramResult.Fail("Chưa cấu hình token bot.");
                return list;
            }

            var url = string.Format(
                "{0}{1}/getUpdates?timeout={2}&offset={3}&allowed_updates=%5B%22message%22%5D",
                ApiBase, botToken, longPollSeconds, offset);

            // Chờ đọc phải dài hơn thời gian long-poll, cộng thêm khoảng dư cho mạng.
            result = Send(url, "GET", null, (longPollSeconds + 15) * 1000);
            if (!result.Ok) return list;

            try
            {
                var updates = JObject.Parse(result.RawResponse)["result"] as JArray;
                if (updates == null) return list;

                foreach (var update in updates)
                {
                    var message = update["message"];
                    if (message == null) continue;

                    var chat = message["chat"];
                    list.Add(new TelegramUpdate
                    {
                        UpdateId = (long)update["update_id"],
                        ChatId = chat != null ? (string)chat["id"] : null,
                        Text = (string)message["text"]
                    });
                }
            }
            catch (Exception ex)
            {
                result = TelegramResult.Fail("Không đọc được cập nhật từ Telegram: " + ex.Message, result.RawResponse);
            }

            return list;
        }

        private static TelegramResult Get(string botToken, string method)
        {
            return Send(ApiBase + botToken + "/" + method, "GET", null);
        }

        private static TelegramResult Post(string botToken, string method, string json)
        {
            return Send(ApiBase + botToken + "/" + method, "POST", json);
        }

        private static TelegramResult Send(string url, string httpMethod, string json, int timeoutMs = 30000)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = httpMethod;
                request.Timeout = timeoutMs;
                request.ReadWriteTimeout = timeoutMs;

                if (json != null)
                {
                    var body = Encoding.UTF8.GetBytes(json);
                    request.ContentType = "application/json; charset=utf-8";
                    request.ContentLength = body.Length;
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(body, 0, body.Length);
                    }
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream() ?? Stream.Null, Encoding.UTF8))
                {
                    return TelegramResult.Success(reader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                // Telegram trả lỗi kèm mô tả trong thân phản hồi, đọc ra để báo cho rõ.
                var raw = ReadErrorBody(ex);
                return TelegramResult.Fail(DescribeError(ex, raw), raw);
            }
            catch (Exception ex)
            {
                return TelegramResult.Fail(ex.Message);
            }
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

        private static string DescribeError(WebException ex, string raw)
        {
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    var description = (string)JObject.Parse(raw)["description"];
                    if (!string.IsNullOrWhiteSpace(description)) return description;
                }
                catch
                {
                    // Không phải JSON thì dùng thông báo gốc bên dưới.
                }
            }

            return ex.Message;
        }
    }
}
