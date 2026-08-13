using System;
using System.IO;
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
    /// Điều khiển trình duyệt nền (Playwright/Chromium) để đăng nhập cổng CAS của VNPT
    /// (id.vnpt.com.vn → hrm.vnpt.vn): điền username/mật khẩu (lấy từ secrets.config) → bấm
    /// Đăng nhập → nếu hệ thống hỏi OTP thì chờ chủ tài khoản trả lời qua Telegram → lưu phiên.
    ///
    /// Đây là hệ thống HOÀN TOÀN KHÁC với <see cref="GoConnectAutoLogin"/> (goconnect.vnpt.vn,
    /// đăng nhập bằng số điện thoại + OTP, không có mật khẩu). Cổng này dùng usernam/mật khẩu
    /// CAS chuẩn, nên không dùng chung mã với GoConnect dù cấu trúc chạy nền giống nhau.
    ///
    /// Lưu ý selector: trang OTP (nếu có) thuộc hrm.vnpt.vn — mã dưới đây đoán theo các mẫu
    /// input phổ biến (numeric, maxlength ngắn, placeholder chứa "OTP"/"mã"). Nếu giao diện
    /// thật khác, ảnh + HTML gỡ lỗi được lưu ở App_Data\hrm-debug để chỉnh lại selector.
    /// </summary>
    public static class HrmCasAutoLogin
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static TaskCompletionSource<string> _otpWaiter;

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
                if (notify != null) notify("Đang có một phiên đăng nhập khác chạy dở, thử lại sau.");
                return false;
            }

            try
            {
                return await RunCoreAsync(notify);
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
                Gate.Release();
            }
        }

        private static async Task<bool> RunCoreAsync(Action<string> notify)
        {
            SetState(HrmState.LoggingIn, "Đang mở trình duyệt và điền đăng nhập...");
            ConfigureBrowsersPath();

            using (var playwright = await Playwright.CreateAsync())
            {
                var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = AppSettings.Hrm.Headless
                });

                IPage page = null;
                try
                {
                    var context = await browser.NewContextAsync();
                    page = await context.NewPageAsync();
                    page.SetDefaultTimeout(30000);

                    await page.GotoAsync(AppSettings.Hrm.LoginUrl,
                        new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

                    await FillFirstAsync(page, UsernameSelectors, AppSettings.Hrm.Username, "ô tên đăng nhập");
                    await FillFirstAsync(page, PasswordSelectors, AppSettings.Hrm.Password, "ô mật khẩu");
                    if (notify != null) notify("① Đã điền tên đăng nhập và mật khẩu, đang bấm Đăng nhập...");

                    await ClickFirstAsync(page, SubmitSelectors, "nút Đăng nhập");

                    // Sau khi bấm submit, chờ một trong ba khả năng: có ô OTP hiện ra, rời khỏi
                    // domain CAS (id.vnpt.com.vn) coi như qua bước mật khẩu, hoặc lỗi sai thông tin.
                    var outcome = await WaitAfterSubmitAsync(page, 20000);

                    if (outcome == SubmitOutcome.Otp)
                    {
                        SetState(HrmState.AwaitingOtp, "Đã gửi thông tin đăng nhập, đang chờ OTP.");
                        var minutes = Math.Max(1, AppSettings.Hrm.OtpTimeoutSeconds / 60);
                        if (notify != null)
                            notify(string.Format(
                                "🔐 Hệ thống HRM yêu cầu OTP.\nTrả lời mã vào đây trong ~{0} phút.", minutes));

                        var otp = await WaitForOtpAsync(TimeSpan.FromSeconds(AppSettings.Hrm.OtpTimeoutSeconds));
                        if (otp == null)
                        {
                            SetState(HrmState.Failed, "Hết thời gian chờ OTP.");
                            if (notify != null) notify("⏰ Hết thời gian chờ OTP, đã huỷ phiên. Gõ /signin để thử lại.");
                            return false;
                        }

                        SetState(HrmState.Verifying, "Đang nhập OTP...");
                        await FillFirstAsync(page, OtpInputSelectors, otp, "ô nhập OTP");

                        // Chờ 3 giây sau khi điền OTP rồi mới bấm, theo đúng yêu cầu — cho trang
                        // kịp xử lý/bật nút trước khi submit.
                        await Task.Delay(3000);

                        await ClickFirstAsync(page, OtpSubmitSelectors, "nút xác nhận OTP");

                        outcome = await WaitAfterSubmitAsync(page, 20000);
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
                    await Task.Delay(15000);

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
                    await browser.CloseAsync();
                }
            }
        }

        private enum SubmitOutcome { LeftCasDomain, Otp, Unknown }

        /// <summary>
        /// Sau khi bấm nút đăng nhập/OTP, thăm dò xem trang đã rời khỏi domain CAS (thành công)
        /// hay đang hiện ô nhập OTP, trong thời gian chờ cho phép.
        /// </summary>
        private static async Task<SubmitOutcome> WaitAfterSubmitAsync(IPage page, int timeoutMs)
        {
            const int step = 250;
            var elapsed = 0;
            var casHost = new Uri(AppSettings.Hrm.LoginUrl).Host;

            while (elapsed < timeoutMs)
            {
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

                await Task.Delay(step);
                elapsed += step;
            }

            return SubmitOutcome.Unknown;
        }

        private static string SafeHost(string url)
        {
            try { return new Uri(url).Host; }
            catch { return string.Empty; }
        }

        private static async Task<string> WaitForOtpAsync(TimeSpan timeout)
        {
            var waiter = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _otpWaiter = waiter;

            var completed = await Task.WhenAny(waiter.Task, Task.Delay(timeout));
            var otp = completed == waiter.Task ? waiter.Task.Result : null;

            _otpWaiter = null;
            return otp;
        }

        private static async Task<ILocator> WaitAnyAsync(IPage page, string[] selectors, int timeoutMs, string what)
        {
            const int step = 250;
            var elapsed = 0;

            while (true)
            {
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
                await Task.Delay(step);
                elapsed += step;
            }
        }

        private static async Task FillFirstAsync(IPage page, string[] selectors, string value, string what)
        {
            var locator = await WaitAnyAsync(page, selectors, 20000, what);
            await locator.FillAsync(value, new LocatorFillOptions { Timeout = 5000 });
        }

        private static async Task ClickFirstAsync(IPage page, string[] selectors, string what)
        {
            var locator = await WaitAnyAsync(page, selectors, 20000, what);
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
