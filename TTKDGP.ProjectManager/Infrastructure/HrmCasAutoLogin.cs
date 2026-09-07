using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TTKDGP.ProjectManager.Data;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>Trạng thái của phiên đăng nhập tự động vào CAS/HRM VNPT.</summary>
    public enum HrmState
    {
        Idle,
        LoggingIn,
        AwaitingOtp,
        Verifying,
        Success,
        Failed
    }

    /// <summary>
    /// Điều khiển trình duyệt nền (Obscura Rust engine hoặc Playwright/Chromium) để đăng nhập
    /// cổng CAS của VNPT (id.vnpt.com.vn → hrm.vnpt.vn): điền username/mật khẩu (lấy từ secrets.config) →
    /// bấm Đăng nhập → nếu hệ thống hỏi OTP thì chờ chủ tài khoản trả lời qua Telegram → lưu phiên.
    ///
    /// Mặc định sử dụng Obscura (Rust) kết nối trực tiếp qua giao thức CDP WebSocket:
    /// - Cực nhẹ (~18MB RAM thay vì 400-500MB của Chromium).
    /// - Không phụ thuộc Node.js hay Playwright driver (loại bỏ hoàn toàn lỗi treo/timeout 25s trên IIS).
    /// - Tích hợp sẵn chế độ Stealth chống chặn bot.
    /// - Tự động fallback sang Playwright/Chromium nếu Obscura bị tắt hoặc lỗi.
    /// </summary>
    public static class HrmCasAutoLogin
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static TaskCompletionSource<string> _otpWaiter;
        private static CancellationTokenSource _currentCts;
        private static IBrowser _activeBrowser;
        private static ObscuraCdpClient _activeCdpClient;
        private static Process _obscuraProcess;

        public static HrmState State { get; private set; }
        public static string LastMessage { get; private set; }
        public static DateTime LastChangedAt { get; private set; }

        public static bool IsAwaitingOtp
        {
            get
            {
                var waiter = _otpWaiter;
                return waiter != null && !waiter.Task.IsCompleted;
            }
        }

        /// <summary>Bộ nhận tin Telegram gọi hàm này khi chủ tài khoản trả lời mã OTP.</summary>
        public static bool SubmitOtp(string otp)
        {
            var waiter = _otpWaiter;
            if (waiter == null || waiter.Task.IsCompleted) return false;
            return waiter.TrySetResult((otp ?? string.Empty).Trim());
        }

        /// <summary>
        /// Ép huỷ phiên đăng nhập hiện tại nếu đang chạy hoặc bị kẹt, giải phóng Semaphore và đưa trạng thái về Idle.
        /// </summary>
        public static void ForceReset(Action<string> notify)
        {
            try
            {
                if (_currentCts != null)
                {
                    _currentCts.Cancel();
                    _currentCts.Dispose();
                    _currentCts = null;
                }
            }
            catch { }

            try
            {
                if (_otpWaiter != null && !_otpWaiter.Task.IsCompleted)
                {
                    _otpWaiter.TrySetCanceled();
                }
            }
            catch { }
            _otpWaiter = null;

            try
            {
                if (_activeCdpClient != null)
                {
                    var c = _activeCdpClient;
                    _activeCdpClient = null;
                    c.Dispose();
                }
            }
            catch { }

            try
            {
                if (_activeBrowser != null)
                {
                    var b = _activeBrowser;
                    _activeBrowser = null;
                    Task.Run(async () =>
                    {
                        try { await b.CloseAsync(); } catch { }
                    });
                }
            }
            catch { }

            try
            {
                if (_obscuraProcess != null && !_obscuraProcess.HasExited)
                {
                    _obscuraProcess.Kill();
                }
            }
            catch { }
            _obscuraProcess = null;

            SetState(HrmState.Idle, "Đã được reset thủ công.");

            try
            {
                if (Gate.CurrentCount == 0)
                {
                    Gate.Release();
                }
            }
            catch { }

            if (notify != null)
            {
                notify("🔄 Đã reset phiên đăng nhập HRM thành công. Bạn có thể gửi /signin để bắt đầu lại.");
            }
        }

        private static string ObscuraExePath()
        {
            return Path.Combine(MapAppData("obscura"), "obscura.exe");
        }

        private static async Task<Process> StartObscuraServerAsync(int port, CancellationToken ct)
        {
            var exePath = ObscuraExePath();
            if (!File.Exists(exePath)) return null;

            if (await IsCdpPortAvailableAsync(port, 1000))
            {
                return null; // Server đã chạy sẵn từ trước
            }

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = string.Format("serve --port {0} --host 127.0.0.1 --stealth --allow-private-network --quiet", port),
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(exePath)
            };

            var proc = Process.Start(psi);
            if (proc == null) return null;

            var ready = false;
            for (var i = 0; i < 20; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (proc.HasExited) break;
                if (await IsCdpPortAvailableAsync(port, 500))
                {
                    ready = true;
                    break;
                }
                await Task.Delay(500, ct);
            }

            if (!ready)
            {
                try { proc.Kill(); } catch { }
                return null;
            }

            return proc;
        }

        private static async Task<bool> IsCdpPortAvailableAsync(int port, int timeoutMs)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
                    var res = await client.GetAsync(string.Format("http://127.0.0.1:{0}/json/version", port));
                    return res.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private static readonly string[] UsernameSelectors = { "#username", "input[name='username']" };
        private static readonly string[] PasswordSelectors = { "#password", "input[name='password']" };
        private static readonly string[] SubmitSelectors =
        {
            "button[name='submit']",
            "button[type='submit']",
            "input[type='submit']"
        };

        // Đã xác nhận trên trang thật: ô OTP là #passOTP (name="validate_pass_otp").
        private static readonly string[] OtpInputSelectors =
        {
            "#passOTP",
            "input[name='validate_pass_otp']",
            "input[placeholder*='OTP']",
            "input[placeholder*='mã xác th']",
            "input[name*='otp']",
            "input[id*='otp']"
        };

        // Nút "ĐĂNG NHẬP" ở màn OTP nhận diện qua onclick="submitForm(this)" (đã xác nhận thật).
        private static readonly string[] OtpSubmitSelectors =
        {
            "button[onclick*='submitForm']",
            "button[type='submit']",
            "input[type='submit']"
        };

        /// <summary>Chạy một phiên đăng nhập. Trả về true nếu vào được hrm.vnpt.vn.</summary>
        public static async Task<bool> RunAsync(Action<string> notify)
        {
            if (string.IsNullOrWhiteSpace(AppSettings.Hrm.Username) || string.IsNullOrWhiteSpace(AppSettings.Hrm.Password))
            {
                if (notify != null) notify("Chưa cấu hình Hrm:Username / Hrm:Password trong secrets.config.");
                return false;
            }

            if (!await Gate.WaitAsync(0))
            {
                // Nếu phiên cũ đã chạy quá 2 phút, tự động dọn dẹp để không làm kẹt
                if ((DateTime.Now - LastChangedAt).TotalMinutes >= 2.0)
                {
                    ForceReset(null);
                    if (!await Gate.WaitAsync(0))
                    {
                        if (notify != null) notify("Đang có một phiên đăng nhập khác chạy dở. Gõ /reset nếu cần hủy phiên.");
                        return false;
                    }
                }
                else
                {
                    if (notify != null) notify("Đang có một phiên đăng nhập khác chạy dở. Gõ /reset nếu cần hủy phiên.");
                    return false;
                }
            }

            var cts = new CancellationTokenSource();
            _currentCts = cts;

            try
            {
                var totalTimeout = TimeSpan.FromSeconds(AppSettings.Hrm.OtpTimeoutSeconds + 120);
                cts.CancelAfter(totalTimeout);

                return await RunCoreAsync(notify, cts.Token);
            }
            catch (OperationCanceledException)
            {
                SetState(HrmState.Failed, "Phiên đăng nhập đã bị hủy hoặc hết hạn thời gian.");
                if (notify != null) notify("⏰ Phiên đăng nhập đã kết thúc (hết thời gian hoặc được reset). Gõ /signin để thử lại.");
                return false;
            }
            catch (Exception ex)
            {
                SetState(HrmState.Failed, "Lỗi: " + ex.Message);
                if (notify != null) notify("❌ Đăng nhập HRM lỗi: " + ex.Message);
                return false;
            }
            finally
            {
                _otpWaiter = null;
                _currentCts = null;
                _activeCdpClient = null;
                _activeBrowser = null;
                try
                {
                    if (Gate.CurrentCount == 0)
                    {
                        Gate.Release();
                    }
                }
                catch { }
            }
        }

        private static async Task<bool> RunCoreAsync(Action<string> notify, CancellationToken ct)
        {
            // 1. Ưu tiên chạy bằng engine Obscura (Rust CDP) nếu được cấu hình và file binary tồn tại
            if (AppSettings.Hrm.UseObscura && File.Exists(ObscuraExePath()))
            {
                try
                {
                    return await RunObscuraLoginAsync(notify, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (notify != null)
                    {
                        notify("⚠️ Engine Obscura gặp lỗi (" + ex.Message + "), đang thử chuyển sang Chromium tiêu chuẩn...");
                    }
                }
            }

            // 2. Fallback về Chromium Playwright nếu Obscura bị tắt hoặc khởi chạy thất bại
            return await RunPlaywrightLoginAsync(notify, ct);
        }

        #region Obscura Rust Engine CDP Flow (Siêu nhẹ, Native WebSocket C#)

        private static async Task<bool> RunObscuraLoginAsync(Action<string> notify, CancellationToken ct)
        {
            SetState(HrmState.LoggingIn, "Đang mở trình duyệt Obscura và điền thông tin đăng nhập...");
            var port = AppSettings.Hrm.ObscuraPort;

            if (notify != null) notify("① Đang khởi động engine Obscura (Rust, ~20MB RAM, stealth)...");
            var obscuraProc = await StartObscuraServerAsync(port, ct);
            _obscuraProcess = obscuraProc;

            var wsUrl = string.Format("ws://127.0.0.1:{0}/devtools/browser", port);
            var client = await ObscuraCdpClient.ConnectAsync(wsUrl, ct);
            _activeCdpClient = client;

            try
            {
                if (notify != null) notify("② Đang mở trang đăng nhập CAS VNPT...");
                await client.NavigateAsync(AppSettings.Hrm.LoginUrl, ct);

                // Chờ ô username xuất hiện (tối đa 20s)
                var found = await client.WaitAnyVisibleAsync(UsernameSelectors, 20000, ct);
                if (!found)
                {
                    throw new Exception("Không tìm thấy ô đăng nhập trên trang CAS VNPT sau khi tải trang.");
                }

                await client.FillFirstAsync(UsernameSelectors, AppSettings.Hrm.Username, ct);
                await client.FillFirstAsync(PasswordSelectors, AppSettings.Hrm.Password, ct);

                if (notify != null) notify("③ Đã điền tên đăng nhập và mật khẩu, đang bấm Đăng nhập...");
                await client.ClickFirstAsync(SubmitSelectors, ct);

                // Sau khi submit, thăm dò xem trang có yêu cầu OTP hay đã chuyển hướng thành công
                var outcome = await WaitAfterSubmitObscuraAsync(client, 20000, ct);

                if (outcome == SubmitOutcome.Otp)
                {
                    SetState(HrmState.AwaitingOtp, "Đã gửi thông tin đăng nhập, đang chờ OTP.");
                    var minutes = Math.Max(1, AppSettings.Hrm.OtpTimeoutSeconds / 60);
                    if (notify != null)
                    {
                        notify(string.Format("🔐 Hệ thống HRM yêu cầu OTP.\nTrả lời mã vào đây trong ~{0} phút.", minutes));
                    }

                    var otp = await WaitForOtpAsync(TimeSpan.FromSeconds(AppSettings.Hrm.OtpTimeoutSeconds), ct);
                    if (otp == null)
                    {
                        SetState(HrmState.Failed, "Hết thời gian chờ OTP.");
                        if (notify != null) notify("⏰ Hết thời gian chờ OTP, đã huỷ phiên. Gõ /signin để thử lại.");
                        return false;
                    }

                    SetState(HrmState.Verifying, "Đang nhập OTP...");
                    await client.FillFirstAsync(OtpInputSelectors, otp, ct);

                    // Chờ 3 giây sau khi điền OTP rồi mới bấm, cho trang kịp xử lý/bật nút trước khi submit
                    await Task.Delay(3000, ct);

                    await client.ClickFirstAsync(OtpSubmitSelectors, ct);

                    outcome = await WaitAfterSubmitObscuraAsync(client, 20000, ct);
                }

                if (outcome != SubmitOutcome.LeftCasDomain)
                {
                    await SaveDebugObscuraAsync(client, "khong-vao-duoc-hrm");
                    SetState(HrmState.Failed, "Không xác nhận được đăng nhập thành công.");
                    if (notify != null)
                    {
                        notify("❌ Đăng nhập không thành công — không rời khỏi được trang CAS. "
                             + "Kiểm tra lại username/mật khẩu hoặc OTP. "
                             + "(Ảnh gỡ lỗi đã lưu ở App_Data\\hrm-debug)");
                    }
                    return false;
                }

                // Rời khỏi domain CAS, điều hướng thẳng vào https://hrm.vnpt.vn/web
                try
                {
                    await client.NavigateAsync("https://hrm.vnpt.vn/web", ct);
                }
                catch { }

                // Chờ 15 giây cho Odoo SPA hoàn tất nạp phiên làm việc
                await Task.Delay(15000, ct);

                // Lưu cookies vào hrm_session.json
                var cookies = await client.GetCookiesAsync(ct);
                var sessionFile = SessionFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(sessionFile));

                var storageState = new JObject
                {
                    ["cookies"] = cookies,
                    ["origins"] = new JArray()
                };
                File.WriteAllText(sessionFile, storageState.ToString(Formatting.Indented), Encoding.UTF8);

                var finalUrl = await client.GetUrlAsync(ct);
                SetState(HrmState.Success, "Đăng nhập thành công. URL: " + finalUrl);
                if (notify != null)
                {
                    notify(string.Format(
                        "✅ Đã đăng nhập HRM thành công lúc {0:HH:mm dd/MM}.\n🌐 Đang ở: {1}\nPhiên đã được lưu lại (Engine Obscura ~20MB RAM).",
                        DateTime.Now, finalUrl));
                }

                // Tự động đồng bộ danh bạ Odoo qua HttpClient Native
                await SyncDirectoryAsync(notify, ct);

                return true;
            }
            catch
            {
                await SaveDebugObscuraAsync(client, "loi-thao-tac-obscura");
                throw;
            }
            finally
            {
                _activeCdpClient = null;
                if (client != null)
                {
                    try { client.Dispose(); } catch { }
                }
                if (obscuraProc != null && !obscuraProc.HasExited)
                {
                    try { obscuraProc.Kill(); } catch { }
                }
                _obscuraProcess = null;
            }
        }

        private static async Task<SubmitOutcome> WaitAfterSubmitObscuraAsync(ObscuraCdpClient client, int timeoutMs, CancellationToken ct)
        {
            const int step = 250;
            var elapsed = 0;
            var casHost = new Uri(AppSettings.Hrm.LoginUrl).Host;

            while (elapsed < timeoutMs)
            {
                ct.ThrowIfCancellationRequested();
                var currentUrl = await client.GetUrlAsync(ct);
                var currentHost = SafeHost(currentUrl);

                if (!string.IsNullOrEmpty(currentHost) && !string.Equals(currentHost, casHost, StringComparison.OrdinalIgnoreCase))
                {
                    return SubmitOutcome.LeftCasDomain;
                }

                foreach (var selector in OtpInputSelectors)
                {
                    try
                    {
                        if (await client.IsVisibleAsync(selector, ct))
                        {
                            return SubmitOutcome.Otp;
                        }
                    }
                    catch { }
                }

                await Task.Delay(step, ct);
                elapsed += step;
            }

            return SubmitOutcome.Unknown;
        }

        private static async Task SaveDebugObscuraAsync(ObscuraCdpClient client, string reason)
        {
            if (client == null) return;

            string baseName;
            try
            {
                var dir = MapAppData("hrm-debug");
                Directory.CreateDirectory(dir);
                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                baseName = Path.Combine(dir, reason + "-" + stamp);
            }
            catch { return; }

            try
            {
                var html = await client.GetContentAsync(CancellationToken.None);
                if (!string.IsNullOrEmpty(html))
                {
                    File.WriteAllText(baseName + ".html", html, Encoding.UTF8);
                }
            }
            catch { }

            try
            {
                var bytes = await client.CaptureScreenshotAsync(CancellationToken.None);
                if (bytes != null && bytes.Length > 0)
                {
                    File.WriteAllBytes(baseName + ".png", bytes);
                }
            }
            catch { }
        }

        #endregion

        #region Fallback Chromium Playwright Flow

        private static async Task<bool> RunPlaywrightLoginAsync(Action<string> notify, CancellationToken ct)
        {
            SetState(HrmState.LoggingIn, "Đang mở trình duyệt Chromium và điền đăng nhập...");
            ConfigureBrowsersPath();

            var createPlaywrightTask = Playwright.CreateAsync();
            if (await Task.WhenAny(createPlaywrightTask, Task.Delay(25000, ct)) != createPlaywrightTask)
            {
                throw new TimeoutException("Khởi tạo Playwright driver quá thời gian (25s).");
            }

            using (var playwright = await createPlaywrightTask)
            {
                ct.ThrowIfCancellationRequested();
                IBrowser browser = null;
                IPage page = null;

                try
                {
                    if (notify != null) notify("① Đang khởi động trình duyệt Chromium bảo mật...");
                    var launchOptions = new BrowserTypeLaunchOptions
                    {
                        Headless = AppSettings.Hrm.Headless,
                        Args = new[] { "--no-sandbox", "--disable-gpu", "--disable-dev-shm-usage" },
                        Timeout = 35000
                    };

                    var launchBrowserTask = playwright.Chromium.LaunchAsync(launchOptions);
                    if (await Task.WhenAny(launchBrowserTask, Task.Delay(35000, ct)) != launchBrowserTask)
                    {
                        throw new TimeoutException("Khởi chạy trình duyệt Chromium quá thời gian (35s).");
                    }

                    browser = await launchBrowserTask;
                    _activeBrowser = browser;

                    var context = browser.Contexts.Count > 0 ? browser.Contexts[0] : await browser.NewContextAsync();
                    page = context.Pages.Count > 0 ? context.Pages[0] : await context.NewPageAsync();
                    page.SetDefaultTimeout(30000);

                    if (notify != null) notify("② Đang tải trang đăng nhập CAS VNPT...");
                    await page.GotoAsync(AppSettings.Hrm.LoginUrl,
                        new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

                    ct.ThrowIfCancellationRequested();
                    await FillFirstPlaywrightAsync(page, UsernameSelectors, AppSettings.Hrm.Username, "ô tên đăng nhập", ct);
                    await FillFirstPlaywrightAsync(page, PasswordSelectors, AppSettings.Hrm.Password, "ô mật khẩu", ct);
                    if (notify != null) notify("③ Đã điền tên đăng nhập và mật khẩu, đang bấm Đăng nhập...");

                    await ClickFirstPlaywrightAsync(page, SubmitSelectors, "nút Đăng nhập", ct);

                    var outcome = await WaitAfterSubmitPlaywrightAsync(page, 20000, ct);

                    if (outcome == SubmitOutcome.Otp)
                    {
                        SetState(HrmState.AwaitingOtp, "Đã gửi thông tin đăng nhập, đang chờ OTP.");
                        var minutes = Math.Max(1, AppSettings.Hrm.OtpTimeoutSeconds / 60);
                        if (notify != null)
                        {
                            notify(string.Format("🔐 Hệ thống HRM yêu cầu OTP.\nTrả lời mã vào đây trong ~{0} phút.", minutes));
                        }

                        var otp = await WaitForOtpAsync(TimeSpan.FromSeconds(AppSettings.Hrm.OtpTimeoutSeconds), ct);
                        if (otp == null)
                        {
                            SetState(HrmState.Failed, "Hết thời gian chờ OTP.");
                            if (notify != null) notify("⏰ Hết thời gian chờ OTP, đã huỷ phiên. Gõ /signin để thử lại.");
                            return false;
                        }

                        SetState(HrmState.Verifying, "Đang nhập OTP...");
                        await FillFirstPlaywrightAsync(page, OtpInputSelectors, otp, "ô nhập OTP", ct);

                        await Task.Delay(3000, ct);

                        await ClickFirstPlaywrightAsync(page, OtpSubmitSelectors, "nút xác nhận OTP", ct);

                        outcome = await WaitAfterSubmitPlaywrightAsync(page, 20000, ct);
                    }

                    if (outcome != SubmitOutcome.LeftCasDomain)
                    {
                        await SaveDebugPlaywrightAsync(page, "khong-vao-duoc-hrm");
                        SetState(HrmState.Failed, "Không xác nhận được đăng nhập thành công.");
                        if (notify != null)
                        {
                            notify("❌ Đăng nhập không thành công — không rời khỏi được trang CAS. "
                                 + "Kiểm tra lại username/mật khẩu hoặc OTP. "
                                 + "(Ảnh gỡ lỗi đã lưu ở App_Data\\hrm-debug)");
                        }
                        return false;
                    }

                    try
                    {
                        await page.GotoAsync("https://hrm.vnpt.vn/web",
                            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });
                    }
                    catch { }
                    await Task.Delay(15000, ct);

                    var sessionFile = SessionFilePath();
                    Directory.CreateDirectory(Path.GetDirectoryName(sessionFile));
                    await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = sessionFile });

                    var finalUrl = page.Url;
                    SetState(HrmState.Success, "Đăng nhập thành công. URL: " + finalUrl);
                    if (notify != null)
                    {
                        notify(string.Format(
                            "✅ Đã đăng nhập HRM thành công lúc {0:HH:mm dd/MM}.\n🌐 Đang ở: {1}\nPhiên đã được lưu lại.",
                            DateTime.Now, finalUrl));
                    }

                    await SyncDirectoryAsync(notify, ct);

                    return true;
                }
                catch
                {
                    await SaveDebugPlaywrightAsync(page, "loi-thao-tac");
                    throw;
                }
                finally
                {
                    _activeBrowser = null;
                    if (browser != null)
                    {
                        try { await browser.CloseAsync(); } catch { }
                    }
                }
            }
        }

        private static async Task<SubmitOutcome> WaitAfterSubmitPlaywrightAsync(IPage page, int timeoutMs, CancellationToken ct)
        {
            const int step = 250;
            var elapsed = 0;
            var casHost = new Uri(AppSettings.Hrm.LoginUrl).Host;

            while (elapsed < timeoutMs)
            {
                ct.ThrowIfCancellationRequested();
                var currentHost = SafeHost(page.Url);
                if (!string.Equals(currentHost, casHost, StringComparison.OrdinalIgnoreCase))
                    return SubmitOutcome.LeftCasDomain;

                foreach (var selector in OtpInputSelectors)
                {
                    try
                    {
                        if (await page.Locator(selector).First.IsVisibleAsync()) return SubmitOutcome.Otp;
                    }
                    catch { }
                }

                await Task.Delay(step, ct);
                elapsed += step;
            }

            return SubmitOutcome.Unknown;
        }

        private static async Task<ILocator> WaitAnyPlaywrightAsync(IPage page, string[] selectors, int timeoutMs, string what, CancellationToken ct)
        {
            const int step = 250;
            var elapsed = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                foreach (var selector in selectors)
                {
                    var locator = page.Locator(selector).First;
                    try
                    {
                        if (await locator.IsVisibleAsync()) return locator;
                    }
                    catch { }
                }

                if (elapsed >= timeoutMs) throw new Exception("Chờ mãi không thấy " + what + " trên trang.");
                await Task.Delay(step, ct);
                elapsed += step;
            }
        }

        private static async Task FillFirstPlaywrightAsync(IPage page, string[] selectors, string value, string what, CancellationToken ct)
        {
            var locator = await WaitAnyPlaywrightAsync(page, selectors, 20000, what, ct);
            await locator.FillAsync(value, new LocatorFillOptions { Timeout = 5000 });
        }

        private static async Task ClickFirstPlaywrightAsync(IPage page, string[] selectors, string what, CancellationToken ct)
        {
            var locator = await WaitAnyPlaywrightAsync(page, selectors, 20000, what, ct);
            await locator.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
        }



        private static async Task SaveDebugPlaywrightAsync(IPage page, string reason)
        {
            if (page == null) return;

            string baseName;
            try
            {
                var dir = MapAppData("hrm-debug");
                Directory.CreateDirectory(dir);
                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                baseName = Path.Combine(dir, reason + "-" + stamp);
            }
            catch { return; }

            try
            {
                var html = await page.ContentAsync();
                File.WriteAllText(baseName + ".html", html, Encoding.UTF8);
            }
            catch { }

            try
            {
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = baseName + ".png" });
            }
            catch { }
        }

        #endregion

        private enum SubmitOutcome { LeftCasDomain, Otp, Unknown }

        private static string SafeHost(string url)
        {
            try { return new Uri(url).Host; }
            catch { return string.Empty; }
        }

        private static async Task<string> WaitForOtpAsync(TimeSpan timeout, CancellationToken ct)
        {
            var waiter = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _otpWaiter = waiter;

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                linkedCts.CancelAfter(timeout);
                var tcs = new TaskCompletionSource<bool>();
                using (linkedCts.Token.Register(() => tcs.TrySetResult(true)))
                {
                    var completed = await Task.WhenAny(waiter.Task, tcs.Task);
                    if (completed == waiter.Task && waiter.Task.Status == TaskStatus.RanToCompletion)
                    {
                        _otpWaiter = null;
                        return waiter.Task.Result;
                    }
                }
            }

            _otpWaiter = null;
            return null;
        }

        private static void ConfigureBrowsersPath()
        {
            var current = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
            if (!string.IsNullOrWhiteSpace(current)) return;

            var dir = MapAppData("playwright-browsers");
            Directory.CreateDirectory(dir);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", dir);
        }

        public static string SessionFilePath()
        {
            return Path.Combine(MapAppData(""), "hrm_session.json");
        }

        private const int DanhBaPageSize = 80;
        private const int DanhBaMaxPages = 500;

        /// <summary>
        /// Đồng bộ danh bạ nhân sự VNPT (vnpt.hr.danhba.view) trực tiếp qua HTTPS JSON-RPC API của Odoo
        /// sử dụng cookies phiên làm việc từ file hrm_session.json.
        /// Chạy trực tiếp từ C# HttpClient: nhanh, ổn định, không phụ thuộc vào việc nạp trang của trình duyệt.
        /// </summary>
        public static async Task<bool> SyncDirectoryAsync(Action<string> notify, CancellationToken ct = default(CancellationToken))
        {
            var sessionFile = SessionFilePath();
            if (!File.Exists(sessionFile))
            {
                if (notify != null) notify("⚠️ Chưa có file phiên đăng nhập (hrm_session.json). Hãy gõ /signin để đăng nhập trước.");
                return false;
            }

            JArray cookies;
            try
            {
                var json = File.ReadAllText(sessionFile, Encoding.UTF8);
                var parsed = JObject.Parse(json);
                cookies = parsed["cookies"] as JArray;
            }
            catch (Exception ex)
            {
                if (notify != null) notify("⚠️ Lỗi đọc file phiên: " + ex.Message);
                return false;
            }

            if (cookies == null || cookies.Count == 0)
            {
                if (notify != null) notify("⚠️ Phiên đăng nhập rỗng. Hãy gửi /signin để đăng nhập lại.");
                return false;
            }

            var handler = new HttpClientHandler();
            var cookieContainer = new CookieContainer();
            foreach (var c in cookies)
            {
                var domain = c["domain"]?.ToString().TrimStart('.');
                var name = c["name"]?.ToString();
                var val = c["value"]?.ToString();
                var path = c["path"]?.ToString() ?? "/";
                if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(name))
                {
                    try
                    {
                        var ck = new System.Net.Cookie(name, val);
                        if (!string.IsNullOrEmpty(path)) ck.Path = path;
                        if (!string.IsNullOrEmpty(domain)) ck.Domain = domain;
                        cookieContainer.Add(ck);
                    }
                    catch { }
                }
            }
            handler.CookieContainer = cookieContainer;

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, ssl) => true;

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(60);
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
                    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

                    var offset = 0;
                    var all = new JArray();
                    int? total = null;

                    for (var page = 0; page < DanhBaMaxPages; page++)
                    {
                        ct.ThrowIfCancellationRequested();

                        var payload = new
                        {
                            jsonrpc = "2.0",
                            method = "call",
                            @params = new
                            {
                                model = "vnpt.hr.danhba.view",
                                domain = new object[]
                                {
                                    new object[] { "department_id", "!=", false },
                                    new object[] { "status", "!=", "exit" },
                                    new object[] { "department_id", "child_of", 5741 }
                                },
                                fields = new[]
                                {
                                    "vnpt_ma_nhan_vien", "name", "mobile_phone", "work_email",
                                    "department_id", "job_id", "vitri_congviec",
                                    "vitri_congviec_code", "birthday", "gioi_tinh", "is_congtacvien"
                                },
                                sort = "department_code ASC, vitri_congviec_code ASC, vnpt_ma_nhan_vien ASC",
                                limit = DanhBaPageSize,
                                offset = offset
                            },
                            id = page + 1
                        };

                        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                        var res = await client.PostAsync("https://hrm.vnpt.vn/web/dataset/search_read", content, ct);
                        var resStr = await res.Content.ReadAsStringAsync();

                        var resObj = JObject.Parse(resStr);
                        if (resObj["error"] != null)
                        {
                            var errMsg = resObj["error"]?["message"]?.ToString() ?? "Không rõ";
                            if (notify != null) notify("⚠️ Máy chủ HRM trả về lỗi: " + errMsg);
                            break;
                        }

                        var records = resObj["result"]?["records"] as JArray ?? new JArray();
                        if (!total.HasValue)
                        {
                            total = resObj["result"]?["length"]?.Value<int>() ?? records.Count;
                        }

                        foreach (var r in records)
                        {
                            all.Add(r);
                        }

                        offset += DanhBaPageSize;

                        if (records.Count < DanhBaPageSize || (total.HasValue && all.Count >= total.Value))
                        {
                            break;
                        }
                    }

                    var count = all.Count;
                    var totalRecords = total ?? count;

                    if (notify != null)
                    {
                        notify(string.Format("📇 Đã lấy danh bạ: {0}/{1} bản ghi.", count, totalRecords));
                    }

                    if (all.Count > 0)
                    {
                        try
                        {
                            HrmDirectorySync.SaveRecords(all, notify);
                        }
                        catch (Exception ex)
                        {
                            if (notify != null) notify("⚠️ Lưu danh bạ vào CSDL lỗi: " + ex.Message);
                        }
                    }

                    return all.Count > 0;
                }
            }
            catch (Exception ex)
            {
                if (notify != null) notify("⚠️ Lỗi trong quá trình đồng bộ danh bạ: " + ex.Message);
                return false;
            }
        }

        private static string MapAppData(string sub)
        {
            var root = HostingEnvironment.IsHosted
                ? HostingEnvironment.MapPath("~/App_Data")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            return string.IsNullOrEmpty(sub) ? root : Path.Combine(root, sub);
        }

        private static void SetState(HrmState state, string message)
        {
            State = state;
            LastMessage = message;
            LastChangedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Client điều khiển trình duyệt Obscura thuần C# qua giao thức Chrome DevTools Protocol (CDP) WebSocket.
    /// Không cần Node.js, không cần Playwright, hoàn toàn không bị deadlock hay timeout trên IIS Express.
    /// </summary>
    public class ObscuraCdpClient : IDisposable
    {
        private readonly ClientWebSocket _ws = new ClientWebSocket();
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JObject>> _pending = new ConcurrentDictionary<int, TaskCompletionSource<JObject>>();
        private int _idCounter = 0;
        private string _sessionId;
        private CancellationTokenSource _readCts;

        public static async Task<ObscuraCdpClient> ConnectAsync(string browserWsUrl, CancellationToken ct)
        {
            var client = new ObscuraCdpClient();
            await client._ws.ConnectAsync(new Uri(browserWsUrl), ct);
            client._readCts = new CancellationTokenSource();
            var ignore = client.ReceiveLoopAsync(client._readCts.Token);

            // 1. Tạo Target trang mới
            var createTargetRes = await client.SendAsync("Target.createTarget", new { url = "about:blank" }, ct);
            var targetId = createTargetRes["result"]?["targetId"]?.ToString();
            if (string.IsNullOrEmpty(targetId))
            {
                throw new Exception("Không thể tạo Target trên Obscura CDP");
            }

            // 2. Attach vào Target để lấy sessionId điều khiển
            var attachRes = await client.SendAsync("Target.attachToTarget", new { targetId = targetId, flatten = true }, ct);
            client._sessionId = attachRes["result"]?["sessionId"]?.ToString();

            // 3. Kích hoạt Page, Runtime và Network domain
            await client.SendToSessionAsync("Page.enable", new { }, ct);
            await client.SendToSessionAsync("Runtime.enable", new { }, ct);
            await client.SendToSessionAsync("Network.enable", new { }, ct);

            return client;
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[65536];
            var ms = new MemoryStream();
            try
            {
                while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
                {
                    ms.SetLength(0);
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        if (result.MessageType == WebSocketMessageType.Close) return;
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    var jsonStr = Encoding.UTF8.GetString(ms.ToArray());
                    try
                    {
                        var msg = JObject.Parse(jsonStr);
                        var idToken = msg["id"];
                        if (idToken != null && idToken.Type == JTokenType.Integer)
                        {
                            var id = idToken.Value<int>();
                            TaskCompletionSource<JObject> tcs;
                            if (_pending.TryRemove(id, out tcs))
                            {
                                tcs.TrySetResult(msg);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public async Task<JObject> SendAsync(string method, object parameters = null, CancellationToken ct = default(CancellationToken))
        {
            var id = Interlocked.Increment(ref _idCounter);
            var tcs = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[id] = tcs;

            var payload = new JObject
            {
                ["id"] = id,
                ["method"] = method
            };
            if (parameters != null)
            {
                payload["params"] = JObject.FromObject(parameters);
            }

            var json = payload.ToString(Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);

            using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token))
            using (linked.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }

        public async Task<JObject> SendToSessionAsync(string method, object parameters = null, CancellationToken ct = default(CancellationToken))
        {
            var id = Interlocked.Increment(ref _idCounter);
            var tcs = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[id] = tcs;

            var payload = new JObject
            {
                ["id"] = id,
                ["sessionId"] = _sessionId,
                ["method"] = method
            };
            if (parameters != null)
            {
                payload["params"] = JObject.FromObject(parameters);
            }

            var json = payload.ToString(Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);

            using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token))
            using (linked.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }

        public async Task NavigateAsync(string url, CancellationToken ct)
        {
            await SendToSessionAsync("Page.navigate", new { url = url }, ct);
        }

        public async Task<JToken> EvaluateAsync(string expression, CancellationToken ct)
        {
            var res = await SendToSessionAsync("Runtime.evaluate", new
            {
                expression = expression,
                awaitPromise = true,
                returnByValue = true
            }, ct);

            return res["result"]?["result"]?["value"];
        }

        public async Task<T> EvaluateAsync<T>(string expression, CancellationToken ct)
        {
            var val = await EvaluateAsync(expression, ct);
            if (val == null) return default(T);
            return val.ToObject<T>();
        }

        public async Task<string> GetUrlAsync(CancellationToken ct)
        {
            var res = await EvaluateAsync<string>("window.location.href", ct);
            return res ?? string.Empty;
        }

        public async Task<string> GetContentAsync(CancellationToken ct)
        {
            var res = await EvaluateAsync<string>("document.documentElement ? document.documentElement.outerHTML : ''", ct);
            return res ?? string.Empty;
        }

        public async Task<byte[]> CaptureScreenshotAsync(CancellationToken ct)
        {
            var res = await SendToSessionAsync("Page.captureScreenshot", new { format = "png" }, ct);
            var b64 = res["result"]?["data"]?.ToString();
            if (string.IsNullOrEmpty(b64)) return null;
            return Convert.FromBase64String(b64);
        }

        public async Task<JArray> GetCookiesAsync(CancellationToken ct)
        {
            var res = await SendToSessionAsync("Network.getCookies", new { }, ct);
            return (res["result"]?["cookies"] as JArray) ?? new JArray();
        }

        public async Task<bool> IsVisibleAsync(string selector, CancellationToken ct)
        {
            var escaped = JsonConvert.ToString(selector);
            var exp = string.Format(@"(function() {{
                var el = document.querySelector({0});
                if (!el) return false;
                var style = window.getComputedStyle(el);
                return style && style.display !== 'none' && style.visibility !== 'hidden' && el.offsetWidth > 0 && el.offsetHeight > 0;
            }})()", escaped);

            var res = await EvaluateAsync<bool?>(exp, ct);
            return res.GetValueOrDefault();
        }

        public async Task<bool> WaitAnyVisibleAsync(string[] selectors, int timeoutMs, CancellationToken ct)
        {
            const int step = 250;
            var elapsed = 0;
            while (elapsed < timeoutMs)
            {
                ct.ThrowIfCancellationRequested();
                foreach (var sel in selectors)
                {
                    if (await IsVisibleAsync(sel, ct)) return true;
                }
                await Task.Delay(step, ct);
                elapsed += step;
            }
            return false;
        }

        public async Task<bool> FillFirstAsync(string[] selectors, string value, CancellationToken ct)
        {
            var selsJson = JsonConvert.SerializeObject(selectors);
            var valJson = JsonConvert.ToString(value ?? string.Empty);

            var exp = string.Format(@"(function() {{
                var sels = {0};
                for (var i = 0; i < sels.length; i++) {{
                    var el = document.querySelector(sels[i]);
                    if (el) {{
                        el.focus();
                        el.value = {1};
                        el.dispatchEvent(new Event('input', {{ bubbles: true }}));
                        el.dispatchEvent(new Event('change', {{ bubbles: true }}));
                        return true;
                    }}
                }}
                return false;
            }})()", selsJson, valJson);

            var res = await EvaluateAsync<bool?>(exp, ct);
            return res.GetValueOrDefault();
        }

        public async Task<bool> ClickFirstAsync(string[] selectors, CancellationToken ct)
        {
            var selsJson = JsonConvert.SerializeObject(selectors);
            var exp = string.Format(@"(function() {{
                var sels = {0};
                for (var i = 0; i < sels.length; i++) {{
                    var el = document.querySelector(sels[i]);
                    if (el) {{
                        el.click();
                        return true;
                    }}
                }}
                return false;
            }})()", selsJson);

            var res = await EvaluateAsync<bool?>(exp, ct);
            return res.GetValueOrDefault();
        }

        public void Dispose()
        {
            try { _readCts?.Cancel(); } catch { }
            try { _readCts?.Dispose(); } catch { }
            try { _ws?.Dispose(); } catch { }
        }
    }
}
