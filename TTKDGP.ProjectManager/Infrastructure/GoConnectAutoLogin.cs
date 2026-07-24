using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Microsoft.Playwright;
using Newtonsoft.Json.Linq;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>Trạng thái của phiên đăng nhập tự động, để hiển thị và tránh chạy chồng.</summary>
    public enum GoConnectState
    {
        Idle,
        RequestingOtp,
        AwaitingOtp,
        Verifying,
        Success,
        Failed
    }

    /// <summary>
    /// Điều khiển trình duyệt nền (Playwright/Chromium) để đăng nhập cổng GoConnect:
    /// điền số điện thoại → bấm gửi OTP → chờ chủ tài khoản trả OTP qua Telegram →
    /// nhập OTP + tick điều khoản + bấm Đăng nhập → vào workspace → lưu phiên.
    ///
    /// Chỉ cho phép một phiên chạy tại một thời điểm. Việc chờ OTP được "gửi" từ bên ngoài
    /// (bộ nhận tin Telegram gọi <see cref="SubmitOtp"/>), nên hàm chạy không được chặn
    /// luồng đọc tin — người gọi phải chạy nền, không await ngay trong vòng lặp nhận tin.
    ///
    /// Lưu ý hạ tầng: cần cài Chromium cho Playwright một lần trên máy chủ, và application pool
    /// nên đặt Always Running để tiến trình không bị IIS ngắt giữa chừng.
    /// </summary>
    public static class GoConnectAutoLogin
    {
        // Chỉ một phiên chạy tại một thời điểm.
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

        // Nơi "nhận" mã OTP từ bên ngoài. null hoặc đã hoàn tất nghĩa là không chờ OTP.
        private static TaskCompletionSource<string> _otpWaiter;

        public static GoConnectState State { get; private set; }
        public static string LastMessage { get; private set; }
        public static DateTime LastChangedAt { get; private set; }

        /// <summary>Token dò được ở lần đăng nhập gần nhất (dạng văn bản thuần để hiển thị).</summary>
        public static string LastTokens { get; private set; }
        public static DateTime LastTokensAt { get; private set; }

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

        // ---- Các bộ chọn phần tử trên trang. Thử lần lượt cho tới khi thấy, để bền với
        //      thay đổi nhỏ của giao diện. Nếu GoConnect đổi giao diện thì chỉ cần sửa ở đây. ----

        private static readonly string[] PhoneSelectors =
        {
            "#input-phone",
            "input[formcontrolname='phone']",
            "input[placeholder*='điện thoại']",
            "input[type='tel']",
            "input[name*='phone']"
        };

        private static readonly string[] SendOtpSelectors =
        {
            "button:has-text('Gửi mã OTP')",
            "button:has-text('Gửi OTP')",
            "text=Gửi mã OTP"
        };

        private static readonly string[] OtpInputSelectors =
        {
            "input[formcontrolname='otp']",
            "#input-otp",
            "input[placeholder*='Mã OTP']",
            "input[placeholder*='OTP']",
            "input[name*='otp']",
            "input[maxlength='6']",
            "input[type='tel']"
        };

        // Checkbox điều khoản kiểu Ant Design: input thật bị ẩn (opacity 0) nên Playwright coi
        // là "không thấy". Click vào ô vuông hiển thị (.ant-checkbox-inner) để tick, tránh click
        // vào chữ vì trong đó có link "Điều khoản sử dụng" sẽ mở trang mới.
        private static readonly string[] TermsSelectors =
        {
            ".ant-checkbox-inner",
            ".ant-checkbox",
            "nz-checkbox",
            "input[type='checkbox']",
            ".ant-checkbox-wrapper"
        };

        private static readonly string[] LoginSelectors =
        {
            "button:text-is('Đăng nhập')",
            "button:has-text('Đăng nhập'):not(:has-text('mật khẩu'))",
            "button:has-text('Đăng nhập')"
        };

        /// <summary>
        /// Chạy một phiên đăng nhập. Trả về true nếu vào được workspace.
        /// <paramref name="notify"/> dùng để nhắn tiến trình qua Telegram (có thể null).
        /// </summary>
        public static async Task<bool> RunAsync(string phone, Action<string> notify, bool stopAtOtp = false)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                if (notify != null) notify("Chưa có số điện thoại để đăng nhập.");
                return false;
            }

            // Không chờ nếu đang có phiên khác — tránh mở nhiều trình duyệt cùng lúc.
            if (!await Gate.WaitAsync(0))
            {
                if (notify != null) notify("Đang có một phiên đăng nhập khác chạy dở, thử lại sau.");
                return false;
            }

            try
            {
                return await RunCoreAsync(phone.Trim(), notify, stopAtOtp);
            }
            catch (Exception ex)
            {
                SetState(GoConnectState.Failed, "Lỗi: " + ex.Message);
                if (notify != null) notify("❌ Đăng nhập lỗi: " + ex.Message);
                return false;
            }
            finally
            {
                _otpWaiter = null;
                Gate.Release();
            }
        }

        private static async Task<bool> RunCoreAsync(string phone, Action<string> notify, bool stopAtOtp)
        {
            SetState(GoConnectState.RequestingOtp, "Đang mở trình duyệt và gửi OTP...");
            ConfigureBrowsersPath();

            using (var playwright = await Playwright.CreateAsync())
            {
                var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = AppSettings.GoConnect.Headless
                });

                IPage page = null;
                try
                {
                    var context = await browser.NewContextAsync();
                    page = await context.NewPageAsync();

                    // Trần thời gian cho MỌI thao tác Playwright, để không có bước nào treo vô hạn.
                    page.SetDefaultTimeout(30000);

                    // SPA có kết nối nền liên tục nên không dùng NetworkIdle (dễ treo tới timeout);
                    // chờ tải xong DOM rồi để các bước sau tự chờ từng phần tử xuất hiện.
                    await page.GotoAsync(AppSettings.GoConnect.LoginUrl,
                        new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

                    await FillFirstAsync(page, PhoneSelectors, phone, "ô số điện thoại");
                    await ClickFirstAsync(page, SendOtpSelectors, "nút Gửi mã OTP");

                    // Gửi OTP thành công thì màn hình chuyển và ô số điện thoại (#input-phone) biến mất.
                    // Nếu sau 15s vẫn còn ô số điện thoại → coi như CHƯA gửi được OTP (thường do VNPT
                    // giới hạn số lần gửi), báo rõ thay vì hỏi OTP một cách mơ hồ.
                    var phoneGone = false;
                    try
                    {
                        await page.Locator("#input-phone").WaitForAsync(new LocatorWaitForOptions
                        {
                            State = WaitForSelectorState.Hidden,
                            Timeout = 15000
                        });
                        phoneGone = true;
                    }
                    catch
                    {
                    }

                    if (!phoneGone)
                    {
                        await SaveDebugAsync(page, "khong-chuyen-man-otp");
                        SetState(GoConnectState.Failed, "Không chuyển sang màn OTP.");
                        if (notify != null)
                            notify("⚠️ Chưa gửi được OTP — màn hình không chuyển sang bước nhập mã. "
                                   + "Thường do VNPT giới hạn số lần gửi OTP trong thời gian ngắn; thử lại sau ít phút.");
                        return false;
                    }

                    // Chuyển sang màn OTP: chờ ô nhập OTP hiện ra.
                    await WaitFirstAsync(page, OtpInputSelectors, 30000, "ô nhập OTP");

                    // Mốc bước 1: chỉ cần tới được màn OTP thì báo và dừng (chưa nhập OTP).
                    if (stopAtOtp)
                    {
                        SetState(GoConnectState.AwaitingOtp, "Đã tới màn hình OTP (dừng theo yêu cầu).");
                        if (notify != null)
                            notify(string.Format(
                                "✅ Đã điền số {0} và chuyển tới màn hình OTP thành công.\nĐã dừng ở đây theo yêu cầu — nhắn tiếp để làm bước sau.",
                                Mask(phone)));
                        return true;
                    }

                    SetState(GoConnectState.AwaitingOtp, "Đã gửi OTP, đang chờ mã.");
                    var minutes = Math.Max(1, AppSettings.GoConnect.OtpTimeoutSeconds / 60);
                    if (notify != null)
                        notify(string.Format(
                            "🔐 Bạn cần nhập OTP của GoConnect (đã gửi tới {0}).\nTrả lời mã vào đây trong ~{1} phút.",
                            Mask(phone), minutes));

                    var otp = await WaitForOtpAsync(TimeSpan.FromSeconds(AppSettings.GoConnect.OtpTimeoutSeconds));
                    if (otp == null)
                    {
                        SetState(GoConnectState.Failed, "Hết thời gian chờ OTP.");
                        if (notify != null) notify("⏰ Hết thời gian chờ OTP, đã huỷ phiên. Gõ /sdt để thử lại.");
                        return false;
                    }

                    SetState(GoConnectState.Verifying, "Đang nhập OTP và đăng nhập...");

                    await FillFirstAsync(page, OtpInputSelectors, otp, "ô nhập OTP");
                    if (notify != null) notify("① Đã nhập OTP, đang tick điều khoản...");

                    await TickTermsAsync(page);
                    if (notify != null) notify("② Đã tick điều khoản, chờ 5 giây rồi bấm Đăng nhập...");

                    // Chờ 5 giây theo yêu cầu, để Angular bật nút và trang ổn định.
                    await Task.Delay(5000);

                    await SubmitLoginAsync(page);
                    if (notify != null) notify("③ Đã bấm Đăng nhập, đang chờ vào workspace (tối đa 15s)...");

                    try
                    {
                        await page.WaitForURLAsync(AppSettings.GoConnect.WorkspaceUrlPattern,
                            new PageWaitForURLOptions { Timeout = 15000 });
                    }
                    catch (TimeoutException)
                    {
                        await SaveDebugAsync(page, "workspace-timeout");
                        SetState(GoConnectState.Failed, "Không vào được workspace (OTP sai hoặc giao diện đổi).");
                        if (notify != null)
                            notify("❌ Đăng nhập không thành công — không tới được màn workspace. "
                                   + "Kiểm tra lại OTP rồi gõ /sdt để thử lại. "
                                   + "(Ảnh gỡ lỗi đã lưu ở App_Data\\goconnect-debug)");
                        return false;
                    }

                    var sessionFile = SessionFilePath();
                    Directory.CreateDirectory(Path.GetDirectoryName(sessionFile));
                    await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = sessionFile });

                    SetState(GoConnectState.Success, "Đăng nhập thành công.");
                    if (notify != null)
                        notify(string.Format("✅ Đã đăng nhập GoConnect thành công lúc {0:HH:mm dd/MM}. "
                                             + "Phiên đã được lưu lại.", DateTime.Now));

                    if (AppSettings.GoConnect.SendToken)
                        await SendTokensAsync(page, context, notify);

                    return true;
                }
                catch
                {
                    // Selector không khớp hoặc lỗi thao tác: lưu ảnh + HTML để soi rồi chỉnh selector.
                    await SaveDebugAsync(page, "loi-thao-tac");
                    throw;
                }
                finally
                {
                    await browser.CloseAsync();
                }
            }
        }

        /// <summary>Chờ mã OTP được gửi từ ngoài vào, hoặc trả null khi quá hạn.</summary>
        private static async Task<string> WaitForOtpAsync(TimeSpan timeout)
        {
            // RunContinuationsAsynchronously: khi nhận OTP, phần còn lại của luồng đăng nhập KHÔNG
            // chạy đè lên luồng nhận tin Telegram (nếu không, vòng lặp nhận tin có thể bị chiếm dụng).
            var waiter = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _otpWaiter = waiter;

            var completed = await Task.WhenAny(waiter.Task, Task.Delay(timeout));
            var otp = completed == waiter.Task ? waiter.Task.Result : null;

            _otpWaiter = null;
            return otp;
        }

        // ---- Tiện ích thao tác trang: thử lần lượt các bộ chọn cho tới khi thành công. ----

        /// <summary>
        /// Chờ tới khi một trong các bộ chọn hiện ra (thấy được) rồi trả về locator đó.
        /// Cần thiết vì trang là Angular SPA: lúc mới tải xong DOM, các ô chưa được render,
        /// phải đợi JS dựng giao diện. Poll từng nhịp ngắn cho tới khi thấy hoặc quá hạn.
        /// </summary>
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
                        // Bộ chọn không hợp lệ với trang hiện tại, thử bộ tiếp theo.
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

        private static async Task CheckFirstAsync(IPage page, string[] selectors, string what)
        {
            // Force=true để tick được cả ô checkbox bị ẩn kiểu Ant Design (input opacity 0).
            var locator = await WaitAnyAsync(page, selectors, 20000, what);
            await locator.CheckAsync(new LocatorCheckOptions { Timeout = 5000, Force = true });
        }

        private static async Task WaitFirstAsync(IPage page, string[] selectors, int timeoutMs, string what)
        {
            await WaitAnyAsync(page, selectors, timeoutMs, what);
        }

        /// <summary>
        /// Tick ô "Đồng ý điều khoản" kiểu Ant Design. Input thật (input.ant-checkbox-input) bị
        /// opacity:0 và bị lớp .ant-checkbox-inner che, nên Playwright coi là "không thấy/không
        /// bấm được". Ta chỉ chờ nó có mặt trong DOM (Attached, không cần thấy) rồi click thẳng
        /// bằng JS và phát sự kiện change để Angular cập nhật, bật nút Đăng nhập.
        /// </summary>
        private static async Task TickTermsAsync(IPage page)
        {
            var checkbox = page.Locator("input.ant-checkbox-input, input[type='checkbox']").First;
            try
            {
                await checkbox.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = 20000
                });
            }
            catch
            {
                throw new Exception("Không tìm thấy ô Đồng ý điều khoản trên trang.");
            }

            // Click vào THẺ LABEL bao ngoài (ant-checkbox-wrapper) để ng-zorro/Angular xử lý.
            // Angular cập nhật trạng thái KHÔNG đồng bộ, nên phải thăm dò cb.checked sau vài nhịp,
            // không đọc ngay. Thử tối đa 3 lần. KHÔNG tự báo lỗi ở đây: để bước bấm Đăng nhập
            // (chờ nút mở khoá) quyết định — đó mới là tín hiệu thật cho biết đã tick thành công.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                await page.EvaluateAsync(
                    "() => { const cb = document.querySelector('input.ant-checkbox-input') || document.querySelector('input[type=checkbox]'); if (cb && !cb.checked) { const label = cb.closest('label'); (label || cb).click(); } }");

                if (await IsTermsCheckedAsync(page, 1500)) return;
            }
        }

        /// <summary>Thăm dò trạng thái đã tick của checkbox điều khoản trong khoảng thời gian cho phép.</summary>
        private static async Task<bool> IsTermsCheckedAsync(IPage page, int timeoutMs)
        {
            var elapsed = 0;
            while (true)
            {
                var isChecked = await page.EvaluateAsync<bool>(
                    "() => { const cb = document.querySelector('input.ant-checkbox-input') || document.querySelector('input[type=checkbox]'); return !!(cb && cb.checked); }");
                if (isChecked) return true;

                if (elapsed >= timeoutMs) return false;
                await Task.Delay(200);
                elapsed += 200;
            }
        }

        /// <summary>
        /// Bấm nút Đăng nhập trên màn OTP. Nút bị khoá cho tới khi Angular ghi nhận đã tick điều
        /// khoản, nên chờ nút hết khoá rồi mới bấm; nếu chờ mãi vẫn khoá thì báo lỗi rõ ràng.
        /// </summary>
        private static async Task SubmitLoginAsync(IPage page)
        {
            var button = page.Locator(
                "button.ant-btn-primary:has-text('Đăng nhập'), button:has-text('Đăng nhập')").First;

            await button.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 20000
            });

            var elapsed = 0;
            while (elapsed < 10000 && !await button.IsEnabledAsync())
            {
                await Task.Delay(250);
                elapsed += 250;
            }

            if (!await button.IsEnabledAsync())
                throw new Exception("Nút Đăng nhập vẫn bị khoá (điều khoản chưa được ghi nhận).");

            await button.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
        }

        /// <summary>
        /// Playwright tải Chromium về một thư mục cache. Dưới danh tính application pool của IIS
        /// thư mục mặc định thường không ghi được, nên trỏ về App_Data để chắc chắn đọc được.
        /// Khi cài Chromium phải cài đúng vào thư mục này (xem README).
        /// </summary>
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
            return Path.Combine(MapAppData(""), "goconnect_session.json");
        }

        /// <summary>
        /// Lưu ảnh chụp toàn trang và mã HTML khi lỗi, giúp soi selector nào chưa khớp
        /// (vì chạy dưới IIS không nhìn thấy cửa sổ trình duyệt). Không được để việc chụp
        /// lỗi này che mất lỗi gốc.
        /// </summary>
        private static async Task SaveDebugAsync(IPage page, string reason)
        {
            if (page == null) return;

            string baseName;
            try
            {
                var dir = MapAppData("goconnect-debug");
                Directory.CreateDirectory(dir);
                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                baseName = Path.Combine(dir, reason + "-" + stamp);
            }
            catch
            {
                return;
            }

            // Ghi HTML TRƯỚC (quan trọng nhất để sửa selector), tách riêng khỏi ảnh để một cái
            // lỗi không kéo cái kia. Ảnh chỉ chụp khung nhìn (không FullPage) cho an toàn với SPA.
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

        /// <summary>
        /// Sau đăng nhập, dò token trong localStorage và cookie của chính tài khoản rồi gửi
        /// lên Telegram. Chỉ gửi những mục trông giống token để tránh lộ dữ liệu thừa.
        /// </summary>
        private static async Task SendTokensAsync(IPage page, IBrowserContext context, Action<string> notify)
        {
            if (notify == null) return;

            try
            {
                var html = new StringBuilder();    // định dạng cho Telegram (parse_mode HTML)
                var plain = new StringBuilder();    // văn bản thuần để hiển thị trên web

                Action<string, string, string> add = (label, name, value) =>
                {
                    html.AppendLine("• " + label + "<b>" + Escape(name) + "</b>:\n<code>" + Escape(value) + "</code>\n");
                    plain.AppendLine(label + name + ":");
                    plain.AppendLine(value);
                    plain.AppendLine();
                };

                // localStorage
                var json = await page.EvaluateAsync<string>(
                    "() => { const o = {}; for (let i = 0; i < localStorage.length; i++) { const k = localStorage.key(i); o[k] = localStorage.getItem(k); } return JSON.stringify(o); }");

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        foreach (var prop in JObject.Parse(json).Properties())
                        {
                            var value = prop.Value != null ? prop.Value.ToString() : null;
                            if (LooksLikeToken(prop.Name, value)) add("", prop.Name, value);
                        }
                    }
                    catch
                    {
                        // localStorage không phải JSON đọc được thì bỏ qua, còn cookie ở dưới.
                    }
                }

                // cookie
                var cookies = await context.CookiesAsync();
                foreach (var cookie in cookies)
                {
                    if (LooksLikeToken(cookie.Name, cookie.Value)) add("cookie ", cookie.Name, cookie.Value);
                }

                LastTokens = plain.Length == 0 ? "(chưa dò thấy token)" : plain.ToString().TrimEnd();
                LastTokensAt = DateTime.Now;

                if (html.Length == 0)
                    notify("ℹ️ Đã đăng nhập nhưng chưa dò thấy token trong localStorage/cookie. "
                           + "Gửi mình danh sách khoá localStorage để chỉnh cách lấy.");
                else
                    notify("🔑 Token sau đăng nhập:\n\n" + html.ToString().TrimEnd());
            }
            catch (Exception ex)
            {
                notify("⚠️ Lấy token lỗi: " + ex.Message);
            }
        }

        /// <summary>Nhận diện một mục trông giống token: theo tên khoá hoặc theo dạng JWT.</summary>
        private static bool LooksLikeToken(string key, string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 20) return false;

            var k = (key ?? string.Empty).ToLowerInvariant();
            if (k.Contains("token") || k.Contains("access") || k.Contains("auth")
                || k.Contains("jwt") || k.Contains("bearer") || k.Contains("session"))
                return true;

            // JWT bắt đầu bằng "eyJ" (phần header base64url của {"alg":...).
            return value.StartsWith("eyJ", StringComparison.Ordinal);
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        /// <summary>Che giữa số điện thoại giống như giao diện GoConnect: 09xxxx****.</summary>
        private static string Mask(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 6) return phone;
            var head = phone.Substring(0, phone.Length - 4);
            return head.Substring(0, Math.Min(6, head.Length)) + new string('*', 4);
        }

        private static void SetState(GoConnectState state, string message)
        {
            State = state;
            LastMessage = message;
            LastChangedAt = DateTime.Now;
        }
    }
}
