using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Vòng lặp nền lắng nghe tin nhắn gửi tới bot Telegram riêng của tính năng đăng nhập HRM
    /// (CAS id.vnpt.com.vn → hrm.vnpt.vn):
    ///   • "/signin"       → đăng nhập bằng username/mật khẩu trong secrets.config
    ///   • khi đang chờ OTP → tin toàn chữ số được coi là mã OTP
    ///
    /// Hoàn toàn độc lập với <see cref="GoConnectTelegramPoller"/> (khác bot, khác hệ thống).
    /// Chỉ nghe đúng chat id của chủ tài khoản (Hrm:ChatId); tin từ nơi khác bị bỏ qua.
    /// </summary>
    public static class HrmTelegramPoller
    {
        private static CancellationTokenSource _cts;
        private static Task _loop;
        private static readonly object Sync = new object();

        private static long _offset;

        public static void Start()
        {
            if (!AppSettings.Hrm.IsConfigured) return;

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
            var token = AppSettings.Hrm.BotToken;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    TelegramResult result;
                    List<TelegramUpdate> updates = TelegramClient.GetUpdates(token, _offset, 20, out result);

                    foreach (var update in updates)
                    {
                        _offset = update.UpdateId + 1;
                        HandleAsync(update);
                    }

                    if (!result.Ok)
                    {
                        await Task.Delay(3000, ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    try { await Task.Delay(3000, ct); } catch { break; }
                }
            }
        }

        private static void HandleAsync(TelegramUpdate update)
        {
            if (update.ChatId != AppSettings.Hrm.ChatId) return;

            var text = (update.Text ?? string.Empty).Trim();
            if (text.Length == 0) return;

            if (HrmCasAutoLogin.IsAwaitingOtp && LooksLikeOtp(text))
            {
                if (HrmCasAutoLogin.SubmitOtp(text)) Reply("Đã nhận OTP, đang xác nhận đăng nhập...");
                return;
            }

            if (text.Equals("/signin", StringComparison.OrdinalIgnoreCase))
            {
                BeginLogin();
                return;
            }
        }

        private static void BeginLogin()
        {
            if (HrmCasAutoLogin.State == HrmState.LoggingIn
                || HrmCasAutoLogin.State == HrmState.AwaitingOtp
                || HrmCasAutoLogin.State == HrmState.Verifying)
            {
                Reply("Đang có một phiên đăng nhập khác chạy dở, chờ xong đã.");
                return;
            }

            Reply("Đang mở trình duyệt, điền tên đăng nhập và mật khẩu...");

            // Chạy nền, KHÔNG await để vòng lặp nhận tin không bị chặn (OTP sẽ tới qua vòng lặp này).
            var ignored = HrmCasAutoLogin.RunAsync(Reply);
        }

        private static void Reply(string text)
        {
            TelegramClient.SendMessage(AppSettings.Hrm.BotToken, AppSettings.Hrm.ChatId, text);
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
