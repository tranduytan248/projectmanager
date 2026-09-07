using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Microsoft.Playwright;
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
    /// Mặc định sử dụng Obscura (Rust) để tiết kiệm ~95% RAM (chỉ tốn ~20MB so với 400MB của Chromium)
    /// và tích hợp sẵn chế độ Stealth chống chặn bot. Tự động fallback sang Chromium nếu Obscura lỗi.
    /// </summary>
    public static class HrmCasAutoLogin
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static TaskCompletionSource<string> _otpWaiter;
        private static CancellationTokenSource _currentCts;
        private static IBrowser _activeBrowser;
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
            for (var i = 0; i < 16; i++)
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
            "button:has-text('ĐĂNG NHẬP')",
            "button:has-text('Đăng nhập')",
            "input[type='submit']"
        };

        // Đã xác nhận trên trang thật: ô OTP là #passOTP (name="validate_pass_otp").
        // Giữ thêm vài mẫu dự phòng phòng khi giao diện đổi.
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
            "button:has-text('ĐĂNG NHẬP')",
            "button:has-text('Đăng nhập')",
            "button:has-text('Xác nhận')",
            "button[type='submit']"
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
            SetState(HrmState.LoggingIn, "Đang mở trình duyệt và điền đăng nhập...");
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
                Process obscuraProc = null;
                IPage page = null;

                try
                {
                    // 1. Thử dùng engine Obscura nếu được cấu hình và file binary tồn tại
                    if (AppSettings.Hrm.UseObscura && File.Exists(ObscuraExePath()))
                    {
                        try
                        {
                            if (notify != null) notify("① Đang khởi động engine Obscura (Rust, ~20MB RAM, stealth)...");
                            var port = AppSettings.Hrm.ObscuraPort;
                            obscuraProc = await StartObscuraServerAsync(port, ct);
                            _obscuraProcess = obscuraProc;

                            var connectTask = playwright.Chromium.ConnectOverCDPAsync(string.Format("http://127.0.0.1:{0}", port));
                            if (await Task.WhenAny(connectTask, Task.Delay(15000, ct)) == connectTask)
                            {
                                browser = await connectTask;
                                if (notify != null) notify("⚡ Kết nối Obscura thành công!");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (notify != null) notify("⚠️ Obscura không khởi động được (" + ex.Message + "), chuyển sang Chromium...");
                        }
                    }

                    // 2. Fallback về Chromium tiêu chuẩn nếu chưa kết nối được Obscura
                    if (browser == null)
                    {
                        if (notify != null) notify("① Đang khởi động trình duyệt Chromium bảo mật...");
                        var launchOptions = new BrowserTypeLaunchOptions
                        {
                            Headless = AppSettings.Hrm.Headless,
                            Args = new[] { "--no-sandbox", "--disable-gpu", "--disable-dev-shm-usage" },
                            Timeout = 30000
                        };

                        var launchBrowserTask = playwright.Chromium.LaunchAsync(launchOptions);
                        if (await Task.WhenAny(launchBrowserTask, Task.Delay(35000, ct)) != launchBrowserTask)
                        {
                            throw new TimeoutException("Khởi chạy trình duyệt Chromium quá thời gian (35s).");
                        }

                        browser = await launchBrowserTask;
                    }

                    _activeBrowser = browser;

                    var context = browser.Contexts.Count > 0 ? browser.Contexts[0] : await browser.NewContextAsync();
                    page = context.Pages.Count > 0 ? context.Pages[0] : await context.NewPageAsync();
                    page.SetDefaultTimeout(30000);

                    if (notify != null) notify("② Đang tải trang đăng nhập CAS VNPT...");
                    await page.GotoAsync(AppSettings.Hrm.LoginUrl,
                            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

                        ct.ThrowIfCancellationRequested();
                        await FillFirstAsync(page, UsernameSelectors, AppSettings.Hrm.Username, "ô tên đăng nhập", ct);
                        await FillFirstAsync(page, PasswordSelectors, AppSettings.Hrm.Password, "ô mật khẩu", ct);
                        if (notify != null) notify("③ Đã điền tên đăng nhập và mật khẩu, đang bấm Đăng nhập...");

                    await ClickFirstAsync(page, SubmitSelectors, "nút Đăng nhập", ct);

                    // Sau khi bấm submit, chờ một trong ba khả năng: có ô OTP hiện ra, rời khỏi
                    // domain CAS (id.vnpt.com.vn) coi như qua bước mật khẩu, hoặc lỗi sai thông tin.
                    var outcome = await WaitAfterSubmitAsync(page, 20000, ct);

                    if (outcome == SubmitOutcome.Otp)
                    {
                        SetState(HrmState.AwaitingOtp, "Đã gửi thông tin đăng nhập, đang chờ OTP.");
                        var minutes = Math.Max(1, AppSettings.Hrm.OtpTimeoutSeconds / 60);
                        if (notify != null)
                            notify(string.Format(
                                "🔐 Hệ thống HRM yêu cầu OTP.\nTrả lời mã vào đây trong ~{0} phút.", minutes));

                        var otp = await WaitForOtpAsync(TimeSpan.FromSeconds(AppSettings.Hrm.OtpTimeoutSeconds), ct);
                        if (otp == null)
                        {
                            SetState(HrmState.Failed, "Hết thời gian chờ OTP.");
                            if (notify != null) notify("⏰ Hết thời gian chờ OTP, đã huỷ phiên. Gõ /signin để thử lại.");
                            return false;
                        }

                        SetState(HrmState.Verifying, "Đang nhập OTP...");
                        await FillFirstAsync(page, OtpInputSelectors, otp, "ô nhập OTP", ct);

                        // Chờ 3 giây sau khi điền OTP rồi mới bấm, theo đúng yêu cầu — cho trang
                        // kịp xử lý/bật nút trước khi submit.
                        await Task.Delay(3000, ct);

                        await ClickFirstAsync(page, OtpSubmitSelectors, "nút xác nhận OTP", ct);

                        outcome = await WaitAfterSubmitAsync(page, 20000, ct);
                    }

                    if (outcome != SubmitOutcome.LeftCasDomain)
                    {
                        await SaveDebugAsync(page, "khong-vao-duoc-hrm");
                        SetState(HrmState.Failed, "Không xác nhận được đăng nhập thành công.");
                        if (notify != null)
                            notify("❌ Đăng nhập không thành công — không rời khỏi được trang CAS. "
                                   + "Kiểm tra lại username/mật khẩu hoặc OTP. "
                                   + "(Ảnh gỡ lỗi đã lưu ở App_Data\\hrm-debug)");
                        return false;
                    }

                    // Rời khỏi domain CAS mới chỉ là vừa nhận ticket (URL dạng
                    // hrm.vnpt.vn/web/login?...&ticket=ST-...) — SPA của HRM cần thêm chút thời
                    // gian xử lý ticket rồi mới tự chuyển vào /web. Ép chuyển thẳng cho chắc thay
                    // vì chờ SPA tự điều hướng, rồi để nguyên trang 15 giây cho nó xử lý xong.
                    try
                    {
                        await page.GotoAsync("https://hrm.vnpt.vn/web",
                            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });
                    }
                    catch
                    {
                        // Điều hướng lỗi thì thôi, vẫn báo URL thật đang đứng ở đâu bên dưới.
                    }
                    await Task.Delay(15000, ct);

                    var sessionFile = SessionFilePath();
                    Directory.CreateDirectory(Path.GetDirectoryName(sessionFile));
                    await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = sessionFile });

                    var finalUrl = page.Url;
                    SetState(HrmState.Success, "Đăng nhập thành công. URL: " + finalUrl);
                    if (notify != null)
                        notify(string.Format(
                            "✅ Đã đăng nhập HRM thành công lúc {0:HH:mm dd/MM}.\n🌐 Đang ở: {1}\nPhiên đã được lưu lại.",
                            DateTime.Now, finalUrl));

                    await TryFetchDanhBaAsync(page, notify);

                    return true;
                }
                catch
                {
                    await SaveDebugAsync(page, "loi-thao-tac");
                    throw;
                }
                finally
                {
                    _activeBrowser = null;
                    if (browser != null)
                    {
                        try { await browser.CloseAsync(); } catch { }
                    }
                    if (obscuraProc != null && !obscuraProc.HasExited)
                    {
                        try { obscuraProc.Kill(); } catch { }
                    }
                    _obscuraProcess = null;
                }
            }
        }

        private enum SubmitOutcome { LeftCasDomain, Otp, Unknown }

        /// <summary>
        /// Sau khi bấm nút đăng nhập/OTP, thăm dò xem trang đã rời khỏi domain CAS (thành công)
        /// hay đang hiện ô nhập OTP, trong thời gian chờ cho phép.
        /// </summary>
        private static async Task<SubmitOutcome> WaitAfterSubmitAsync(IPage page, int timeoutMs, CancellationToken ct)
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
                    catch
                    {
                    }
                }

                await Task.Delay(step, ct);
                elapsed += step;
            }

            return SubmitOutcome.Unknown;
        }

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

        private static async Task<ILocator> WaitAnyAsync(IPage page, string[] selectors, int timeoutMs, string what, CancellationToken ct)
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
                    catch
                    {
                    }
                }

                if (elapsed >= timeoutMs) throw new Exception("Chờ mãi không thấy " + what + " trên trang.");
                await Task.Delay(step, ct);
                elapsed += step;
            }
        }

        private static async Task FillFirstAsync(IPage page, string[] selectors, string value, string what, CancellationToken ct)
        {
            var locator = await WaitAnyAsync(page, selectors, 20000, what, ct);
            await locator.FillAsync(value, new LocatorFillOptions { Timeout = 5000 });
        }

        private static async Task ClickFirstAsync(IPage page, string[] selectors, string what, CancellationToken ct)
        {
            var locator = await WaitAnyAsync(page, selectors, 20000, what, ct);
            await locator.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
        }

        private static void ConfigureBrowsersPath()
        {
            var current = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
            if (!string.IsNullOrWhiteSpace(current)) return;

            var dir = MapAppData("playwright-browsers");
            Directory.CreateDirectory(dir);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", dir);
        }

        private static string SessionFilePath()
        {
            return Path.Combine(MapAppData(""), "hrm_session.json");
        }

        /// <summary>
        /// Gọi API "danh bạ" (vnpt.hr.danhba.view) ngay TRONG trang đang mở bằng fetch() của
        /// chính trình duyệt — cookie phiên tự động đi kèm vì cùng origin, không cần trích cookie
        /// ra ngoài. Server giới hạn mỗi lần gọi tối đa <see cref="DanhBaPageSize"/> bản ghi (đúng
        /// bằng "limit" quan sát được trong curl thật), nên phải phân trang bằng "offset" và gộp
        /// lại cho tới khi đủ "length" (tổng số bản ghi khớp domain) server trả về ở trang đầu.
        /// Sau khi gộp xong: lưu thẳng vào CSDL qua <see cref="HrmDirectorySync"/> rồi báo số bản
        /// ghi qua Telegram — KHÔNG lưu JSON thô ra file nữa (dữ liệu đã có trong CSDL, giữ thêm
        /// bản file chỉ là trùng lặp và có thể tồn đọng dữ liệu nhân sự trên đĩa không cần thiết).
        ///
        /// department_id=5741 lấy nguyên theo ví dụ curl bạn cung cấp (đơn vị của chính tài khoản
        /// đăng nhập) — nếu cần tổng quát hoá cho tài khoản khác thì phải đọc động từ phiên, không
        /// khai cứng ở đây.
        /// </summary>
        private const int DanhBaPageSize = 80;

        // Chặn vòng lặp phân trang chạy vô hạn nếu "length" server trả về sai lệch — 500 trang x
        // 80 bản ghi/trang = 40.000 bản ghi, dư sức cho toàn bộ danh bạ VNPT.
        private const int DanhBaMaxPages = 500;

        private static async Task TryFetchDanhBaAsync(IPage page, Action<string> notify)
        {
            const string script = @"
                async (args) => {
                    const pageSize = args.pageSize;
                    const maxPages = args.maxPages;

                    // uid thật của tài khoản đang đăng nhập — Odoo web client (Odoo 14+) gắn
                    // sẵn vào window.odoo.session_info (một số bản cũ dùng __session_info__)
                    // ngay khi /web tải xong. Không khai cứng vì mỗi tài khoản một uid khác nhau.
                    const sessionInfo = (window.odoo && (odoo.session_info || odoo.__session_info__)) || {};
                    const context = {
                        tz: 'Asia/Ho_Chi_Minh', lang: 'vi_VN',
                        search_default_nvct: 1, loai: 'danhba',
                        non_display_department: 'non_display_department',
                        view_from_action: 'vnpt_hr_danhba_view',
                        domain_ctv: [['department_id', '!=', false], ['status', '!=', 'exit']],
                        human_resource: 'HR', option_deptree: '0'
                    };
                    if (sessionInfo.uid) context.uid = sessionInfo.uid;

                    const baseParams = {
                        model: 'vnpt.hr.danhba.view',
                        domain: [
                            ['department_id', '!=', false],
                            ['status', '!=', 'exit'],
                            ['department_id', 'child_of', 5741]
                        ],
                        fields: ['vnpt_ma_nhan_vien', 'name', 'mobile_phone', 'work_email',
                                 'department_id', 'job_id', 'vitri_congviec',
                                 'vitri_congviec_code', 'birthday', 'gioi_tinh', 'is_congtacvien'],
                        sort: 'department_code ASC, vitri_congviec_code ASC, vnpt_ma_nhan_vien ASC',
                        context: context
                    };

                    let all = [];
                    let total = null;
                    let offset = 0;

                    for (let page = 0; page < maxPages; page++) {
                        const res = await fetch('/web/dataset/search_read', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json',
                                'X-Requested-With': 'XMLHttpRequest'
                            },
                            body: JSON.stringify({
                                jsonrpc: '2.0',
                                method: 'call',
                                params: Object.assign({}, baseParams, { limit: pageSize, offset: offset }),
                                id: Date.now() + offset
                            })
                        });
                        const data = await res.json();

                        if (data.error) return JSON.stringify(data);

                        const records = (data.result && data.result.records) || [];
                        if (total === null) total = data.result ? data.result.length : records.length;

                        all = all.concat(records);
                        offset += pageSize;

                        if (records.length < pageSize || all.length >= total) break;
                    }

                    return JSON.stringify({ jsonrpc: '2.0', result: { length: total, records: all } });
                }";

            try
            {
                var json = await page.EvaluateAsync<string>(script,
                    new { pageSize = DanhBaPageSize, maxPages = DanhBaMaxPages });

                var parsed = JObject.Parse(json);
                var records = parsed["result"] != null ? parsed["result"]["records"] as JArray : null;
                var error = parsed["error"];

                if (error != null)
                {
                    if (notify != null) notify("⚠️ Gọi API danh bạ lỗi: " + error["message"]);
                    return;
                }

                var count = records != null ? records.Count : 0;
                var total = parsed["result"] != null ? (int?)parsed["result"]["length"] : null;
                var warnTruncated = total.HasValue && count < total.Value
                    ? string.Format(" ⚠️ Chưa đủ so với tổng {0} (chạm giới hạn {1} trang) — kiểm tra lại.", total, DanhBaMaxPages)
                    : string.Empty;

                if (notify != null)
                    notify(string.Format("📇 Đã lấy danh bạ: {0}/{1} bản ghi.{2}",
                        count, total.HasValue ? total.Value.ToString() : count.ToString(), warnTruncated));

                if (records != null && records.Count > 0)
                {
                    try
                    {
                        HrmDirectorySync.SaveRecords(records, notify);
                    }
                    catch (Exception ex)
                    {
                        if (notify != null) notify("⚠️ Lưu danh bạ vào CSDL lỗi: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                if (notify != null) notify("⚠️ Gọi API danh bạ lỗi: " + ex.Message);
            }
        }

        private static async Task SaveDebugAsync(IPage page, string reason)
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
            catch
            {
                return;
            }

            try
            {
                var html = await page.ContentAsync();
                File.WriteAllText(baseName + ".html", html);
            }
            catch
            {
            }

            try
            {
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = baseName + ".png" });
            }
            catch
            {
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
}
