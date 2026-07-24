using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Vòng lặp nền lắng nghe tin nhắn gửi tới bot Telegram, phục vụ đăng nhập GoConnect:
    ///   • "/hrm"          → đăng nhập bằng số mặc định, xong thì đồng bộ nhân sự về CSDL
    ///   • "/hrm 09xxxxxx" → như trên nhưng dùng số kèm theo
    ///   • "/dongbo"       → chỉ đồng bộ, dùng lại token của phiên đã lưu (không đăng nhập lại)
    ///   • khi đang chờ OTP → tin toàn chữ số được coi là mã OTP
    ///
    /// Không có hẹn giờ tự động: mọi thứ chỉ chạy khi chủ tài khoản chủ động nhắn lệnh.
    ///
    /// Chỉ nghe đúng chat id của chủ tài khoản (GoConnect:ChatId); tin từ nơi khác bị bỏ qua.
    /// Việc đăng nhập chạy nền (không await trong vòng lặp) để vòng lặp còn đọc được chính
    /// mã OTP mà chủ tài khoản gửi vào.
    /// </summary>
    public static class GoConnectTelegramPoller
    {
        private static CancellationTokenSource _cts;
        private static Task _loop;
        private static readonly object Sync = new object();

        private static long _offset;

        public static void Start()
        {
            if (!AppSettings.GoConnect.IsConfigured) return;

            lock (Sync)
            {
                if (_loop != null) return;
                _cts = new CancellationTokenSource();
                _loop = Task.Run(() => LoopAsync(_cts.Token));
            }
        }

        public static void Stop()
        {
            lock (Sync)
            {
                if (_cts == null) return;
                _cts.Cancel();
                _cts = null;
                _loop = null;
            }
        }

        private static async Task LoopAsync(CancellationToken ct)
        {
            // Máy đăng nhập nghe bot GoConnect (@HRM_KHA_bot); bản chạy thật nghe bot Thư ký
            // (@Imu_Luffy_bot). Mỗi bot chỉ một máy đọc nên không giành tin, tránh lỗi 409.
            var token = AppSettings.GoConnect.ActiveBotToken;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    TelegramResult result;
                    List<TelegramUpdate> updates = TelegramClient.GetUpdates(token, _offset, 20, out result);

                    foreach (var update in updates)
                    {
                        _offset = update.UpdateId + 1;   // đánh dấu đã xử lý để không nhận lại
                        HandleAsync(update);
                    }

                    if (!result.Ok)
                    {
                        // Lỗi tạm thời (mạng, Telegram bận): nghỉ một nhịp rồi thử lại.
                        await Task.Delay(3000, ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Không để lỗi làm chết vòng lặp; nghỉ rồi thử tiếp.
                    try { await Task.Delay(3000, ct); } catch { break; }
                }
            }
        }

        private static void HandleAsync(TelegramUpdate update)
        {
            // Chỉ nghe đúng chat của chủ tài khoản.
            if (update.ChatId != AppSettings.GoConnect.ChatId) return;

            var text = (update.Text ?? string.Empty).Trim();
            if (text.Length == 0) return;

            // Đang chờ OTP và tin toàn chữ số → coi là mã OTP.
            if (GoConnectAutoLogin.IsAwaitingOtp && LooksLikeOtp(text))
            {
                if (GoConnectAutoLogin.SubmitOtp(text)) Reply("Đã nhận OTP, đang đăng nhập...");
                return;
            }

            // /hrm : đăng nhập bằng số mặc định (mở trình duyệt, hỏi OTP), xong thì lưu token + đồng bộ.
            if (text.Equals("/hrm", StringComparison.OrdinalIgnoreCase))
            {
                if (RejectLoginIfLocked()) return;
                BeginLogin(AppSettings.GoConnect.DefaultPhone);
                return;
            }

            // /hrm 09xxxxxxxx : dùng số kèm theo.
            if (text.StartsWith("/hrm ", StringComparison.OrdinalIgnoreCase))
            {
                if (RejectLoginIfLocked()) return;
                BeginLogin(text.Substring(5));
                return;
            }

            // /dongbo : bỏ qua đăng nhập, đồng bộ bằng token đã lưu trong CSDL. Access token sống
            // khoảng một tháng nên phần lớn thời gian không cần đăng nhập lại chỉ để đồng bộ.
            // Đây là lệnh dùng trên bản chạy thật, nơi /hrm bị khoá.
            if (text.Equals("/dongbo", StringComparison.OrdinalIgnoreCase))
            {
                var ignored = Task.Run(() => SyncToDatabase(GoConnectSyncService.SyncFromDatabase));
                return;
            }
        }

        private static void BeginLogin(string rawPhone)
        {
            var phone = DigitsOnly(rawPhone);
            if (phone.Length < 9 || phone.Length > 11)
            {
                Reply("Chưa có số điện thoại hợp lệ (kiểm tra GoConnect:DefaultPhone).");
                return;
            }

            Reply("Đang mở trình duyệt, điền số điện thoại và gửi OTP..." + Where());

            // Chạy nền, KHÔNG await để vòng lặp nhận tin không bị chặn (OTP sẽ tới qua vòng lặp này).
            // Full luồng: điền số → OTP → nhập OTP → tick điều khoản → đăng nhập → workspace.
            var login = GoConnectAutoLogin.RunAsync(phone, Reply, false);

            // Đăng nhập xong (và chỉ khi thành công) thì lưu token vào CSDL rồi đồng bộ luôn.
            // Đặt trong continuation để vòng lặp nhận tin không phải chờ.
            var ignored = login.ContinueWith(task =>
            {
                if (task.Status != TaskStatus.RanToCompletion || !task.Result) return;
                SaveTokenThenSync();
            });
        }

        /// <summary>
        /// Từ chối /hrm khi máy này không phải máy đăng nhập (không mang IP đã khai). Trả về true
        /// nếu đã từ chối. Bản chạy thật không có Chromium và không tự đăng nhập; token do máy
        /// local lo. Nhận diện theo IP nên không cần đặt cờ riêng cho từng máy.
        /// </summary>
        private static bool RejectLoginIfLocked()
        {
            if (AppSettings.GoConnect.IsLoginMachine) return false;

            Reply("🔒 Lệnh /hrm chỉ chạy được ở máy đăng nhập (có trình duyệt). "
                  + "Hãy gõ /hrm cho bot đăng nhập ở máy local; token sẽ tự lưu vào CSDL. "
                  + "Ở đây chỉ dùng /dongbo để đồng bộ bằng token đó." + Where());
            return true;
        }

        /// <summary>
        /// Sau khi /hrm đăng nhập xong: lưu token của phiên vừa tạo vào CSDL (để bản live dùng),
        /// rồi đồng bộ luôn vì đang có token tươi.
        /// </summary>
        private static void SaveTokenThenSync()
        {
            try
            {
                if (GoConnectSyncService.SaveSessionTokenToDatabase())
                    Reply("🔑 Đã lưu token vào cơ sở dữ liệu. Bản chạy thật giờ có thể /dongbo." + Where());
                else
                    Reply("⚠️ Đăng nhập xong nhưng không đọc được token để lưu vào CSDL." + Where());
            }
            catch (Exception ex)
            {
                Reply("⚠️ Lưu token vào CSDL lỗi: " + ex.Message);
            }

            SyncToDatabase(GoConnectSyncService.SyncFromSavedSession);
        }

        /// <summary>
        /// Kéo danh sách nhân sự từ GoConnect về CSDL. Nguồn token do <paramref name="sync"/>
        /// quyết định: phiên trên đĩa (sau /hrm ở local) hay token trong CSDL (khi /dongbo).
        /// Mọi lỗi đều nhắn lại cho chủ tài khoản chứ không được ném lên vòng lặp nhận tin.
        /// </summary>
        private static void SyncToDatabase(Func<Action<string>, GoConnectSyncResult> sync)
        {
            try
            {
                Reply("📥 Bắt đầu đồng bộ nhân sự từ GoConnect về cơ sở dữ liệu..." + Where());

                var result = sync(ReportProgress);

                Reply("✅ Đồng bộ xong.\n" + result + Where());
            }
            catch (GoConnectAuthException ex)
            {
                Reply("❌ " + ex.Message + "\nGõ /hrm để đăng nhập lại." + Where());
            }
            catch (Exception ex)
            {
                Reply("❌ Đồng bộ lỗi: " + ex.Message + Where());
            }
        }

        // Đồng bộ đi qua 16 trang API rồi ghi ba bảng; nhắn từng bước sẽ ra hơn hai chục tin và
        // chạm giới hạn tần suất của Telegram. Chỉ nhắn lại mỗi 15 giây một lần cho biết còn sống.
        private static DateTime _lastProgressAt = DateTime.MinValue;

        private static void ReportProgress(string message)
        {
            var now = DateTime.Now;
            if ((now - _lastProgressAt).TotalSeconds < 15) return;

            _lastProgressAt = now;
            Reply("… " + message);
        }

        private static void Reply(string text)
        {
            TelegramClient.SendMessage(AppSettings.GoConnect.ActiveBotToken, AppSettings.GoConnect.ChatId, text);
        }

        /// <summary>
        /// Địa chỉ hệ thống + tên máy đang xử lý, đính vào cuối các tin quan trọng. Vì cả bản
        /// local và bản chạy thật cùng nghe một bot, dòng này cho biết chính xác máy nào đã
        /// nhận lệnh và trả lời — domain hai nơi có thể trùng nên phải kèm tên máy để phân biệt.
        /// </summary>
        private static string Where()
        {
            // Địa chỉ riêng của phần GoConnect (GoConnect:PublicUrl = pm.tdt.vn), tách khỏi
            // App:PublicUrl mà nhắc báo cáo/mail dùng. Khai cứng trong cấu hình chứ không đoán
            // theo request, vì một máy có thể trả lời nhiều tên miền cùng lúc.
            var url = AppSettings.GoConnect.PublicUrl;
            var host = Environment.MachineName;

            if (string.IsNullOrWhiteSpace(url)) return "\n\n🖥️ máy " + host;
            return string.Format("\n\n🌐 {0} · 🖥️ máy {1}", url, host);
        }

        private static bool LooksLikeOtp(string text)
        {
            var digits = DigitsOnly(text);
            return digits.Length >= 4 && digits.Length <= 8 && digits == text;
        }

        private static string DigitsOnly(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var chars = new char[text.Length];
            var n = 0;
            foreach (var c in text)
            {
                if (c >= '0' && c <= '9') chars[n++] = c;
            }
            return new string(chars, 0, n);
        }
    }
}
