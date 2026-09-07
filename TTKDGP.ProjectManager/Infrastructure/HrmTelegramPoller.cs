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
            if (!AppSettings.Hrm.Enabled) return;

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
            while (!ct.IsCancellationRequested)
            {
                var token = AppSettings.Hrm.BotToken;
                if (string.IsNullOrWhiteSpace(token))
                {
                    try { await Task.Delay(5000, ct); } catch { break; }
                    continue;
                }

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

            // 1. Khi đang chờ OTP
            if (HrmCasAutoLogin.IsAwaitingOtp && LooksLikeOtp(text))
            {
                if (HrmCasAutoLogin.SubmitOtp(text)) Reply("Đã nhận OTP, đang xác nhận đăng nhập...");
                return;
            }

            // 2. Lệnh Hủy / Reset phiên bị treo
            if (IsResetCommand(text))
            {
                HrmCasAutoLogin.ForceReset(Reply);
                return;
            }

            // 3. Lệnh Đồng bộ danh bạ (/sync, sync, /dongbo)
            if (IsSyncCommand(text))
            {
                Reply("🔄 Đang kết nối máy chủ HRM để đồng bộ danh bạ nhân sự...");
                Task.Run(async () =>
                {
                    try
                    {
                        await HrmCasAutoLogin.SyncDirectoryAsync(Reply);
                    }
                    catch (Exception ex)
                    {
                        Reply("❌ Lỗi đồng bộ: " + ex.Message);
                    }
                });
                return;
            }

            // 4. Lệnh Đăng nhập (/signin, signin, /login, /sigin...)
            if (IsSignInCommand(text))
            {
                BeginLogin();
                return;
            }

            // 5. Lệnh Kiểm tra trạng thái (/status, status, /tt, tt)
            if (IsStatusCommand(text))
            {
                ReportStatus();
                return;
            }

            // 6. Lệnh Trợ giúp / Hướng dẫn
            if (IsHelpCommand(text))
            {
                Reply("🤖 <b>Bot Hỗ Trợ Đăng Nhập HRM VNPT</b>\n\n"
                    + "Các lệnh khả dụng:\n"
                    + "• <code>/status</code> : Xem tình trạng phiên & số lượng nhân sự đã lưu\n"
                    + "• <code>/sync</code> : Đồng bộ ngay danh bạ nhân sự từ phiên đã lưu\n"
                    + "• <code>/signin</code> : Bắt đầu phiên đăng nhập HRM tự động qua CAS\n"
                    + "• <code>/reset</code> : Hủy/reset phiên nếu bị kẹt hoặc treo\n"
                    + "• Khi nhận được thông báo hỏi OTP, chỉ cần gửi mã số OTP vào đây.");
                return;
            }

            // 7. Tin nhắn không rõ lệnh -> Hướng dẫn người dùng
            Reply("🤖 Bot đã nhận được tin nhắn của bạn nhưng chưa hiểu lệnh.\n\n"
                + "Bạn hãy chọn một trong các lệnh sau:\n"
                + "• <code>/status</code> : Xem trạng thái hệ thống & danh bạ\n"
                + "• <code>/sync</code> : Đồng bộ lại danh bạ nhân sự HRM\n"
                + "• <code>/signin</code> : Đăng nhập tài khoản HRM CAS\n"
                + "• <code>/help</code> : Xem hướng dẫn chi tiết");
        }

        private static void ReportStatus()
        {
            var sessionFile = HrmCasAutoLogin.SessionFilePath();
            var hasSession = System.IO.File.Exists(sessionFile);
            var sessionTime = hasSession ? System.IO.File.GetLastWriteTime(sessionFile).ToString("HH:mm:ss dd/MM/yyyy") : "Chưa có";

            int empCount = 0, deptCount = 0, jobCount = 0;
            try
            {
                using (var conn = Db.Open())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT (SELECT COUNT(*) FROM employee_hrm) AS e, (SELECT COUNT(*) FROM department_hrm) AS d, (SELECT COUNT(*) FROM job_hrm) AS j";
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            empCount = Convert.ToInt32(reader["e"]);
                            deptCount = Convert.ToInt32(reader["d"]);
                            jobCount = Convert.ToInt32(reader["j"]);
                        }
                    }
                }
            }
            catch { }

            Reply(string.Format(
                "📊 <b>Báo Cáo Trạng Thái Hệ Thống HRM</b>\n\n" +
                "• Trạng thái đăng nhập: <code>{0}</code>\n" +
                "• Phiên lưu gần nhất: <b>{1}</b>\n" +
                "• Dữ liệu trong CSDL SQL Server:\n" +
                "   🏢 Đơn vị / Phòng ban: <b>{2}</b>\n" +
                "   💼 Chức danh công việc: <b>{3}</b>\n" +
                "   👤 Nhân sự nhân viên: <b>{4}</b> người\n\n" +
                "Gõ <code>/sync</code> để cập nhật mới nhất từ HRM, hoặc <code>/signin</code> nếu cần đăng nhập lại.",
                HrmCasAutoLogin.State, sessionTime, deptCount, jobCount, empCount));
        }

        private static bool IsStatusCommand(string text)
        {
            return text.Equals("/status", StringComparison.OrdinalIgnoreCase)
                || text.Equals("status", StringComparison.OrdinalIgnoreCase)
                || text.Equals("/tt", StringComparison.OrdinalIgnoreCase)
                || text.Equals("tt", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSyncCommand(string text)
        {
            return text.Equals("/sync", StringComparison.OrdinalIgnoreCase)
                || text.Equals("sync", StringComparison.OrdinalIgnoreCase)
                || text.Equals("/dongbo", StringComparison.OrdinalIgnoreCase)
                || text.Equals("dongbo", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsResetCommand(string text)
        {
            return text.Equals("/reset", StringComparison.OrdinalIgnoreCase)
                || text.Equals("reset", StringComparison.OrdinalIgnoreCase)
                || text.Equals("/cancel", StringComparison.OrdinalIgnoreCase)
                || text.Equals("cancel", StringComparison.OrdinalIgnoreCase)
                || text.Equals("/stop", StringComparison.OrdinalIgnoreCase)
                || text.Equals("stop", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSignInCommand(string text)
        {
            return text.Equals("/signin", StringComparison.OrdinalIgnoreCase)
                || text.Equals("signin", StringComparison.OrdinalIgnoreCase)
                || text.Equals("/login", StringComparison.OrdinalIgnoreCase)
                || text.Equals("login", StringComparison.OrdinalIgnoreCase)
                || text.Equals("/sigin", StringComparison.OrdinalIgnoreCase)
                || text.Equals("sigin", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHelpCommand(string text)
        {
            return text.Equals("/help", StringComparison.OrdinalIgnoreCase)
                || text.Equals("help", StringComparison.OrdinalIgnoreCase)
                || text.Equals("/start", StringComparison.OrdinalIgnoreCase)
                || text.Equals("start", StringComparison.OrdinalIgnoreCase);
        }

        private static void BeginLogin()
        {
            if (HrmCasAutoLogin.State == HrmState.LoggingIn
                || HrmCasAutoLogin.State == HrmState.AwaitingOtp
                || HrmCasAutoLogin.State == HrmState.Verifying)
            {
                // Nếu phiên trước đã chạy quá 2 phút mà chưa xong -> coi như bị treo, tự động ForceReset
                if ((DateTime.Now - HrmCasAutoLogin.LastChangedAt).TotalMinutes >= 2.0)
                {
                    Reply("⚠️ Phiên trước đó chạy quá 2 phút, đã tự động dọn dẹp để bắt đầu phiên mới...");
                    HrmCasAutoLogin.ForceReset(null);
                }
                else
                {
                    Reply("Đang có một phiên đăng nhập khác chạy dở. Nếu bị kẹt, bạn hãy gửi /reset để hủy phiên.");
                    return;
                }
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
