using System;
using System.Web.Hosting;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Màn hình bấm tay để khởi động đăng nhập GoConnect. Khi bấm, hệ thống gửi lệnh y như
    /// gõ /sdt: bot Telegram nhắn yêu cầu OTP, người dùng trả lời OTP trên Telegram. Chỉ Admin dùng.
    /// </summary>
    [AppAuthorize(RequiredRole = Roles.Admin)]
    public class GoConnectController : BaseController
    {
        public ActionResult Index()
        {
            ViewBag.Enabled = AppSettings.GoConnect.Enabled;
            ViewBag.HasToken = AppSettings.GoConnect.HasToken;
            ViewBag.HasChatId = AppSettings.GoConnect.HasChatId;
            ViewBag.Phone = AppSettings.GoConnect.DefaultPhone;
            ViewBag.State = GoConnectAutoLogin.State.ToString();
            ViewBag.LastMessage = GoConnectAutoLogin.LastMessage;
            ViewBag.LastChangedAt = GoConnectAutoLogin.LastChangedAt;
            ViewBag.IsAwaitingOtp = GoConnectAutoLogin.IsAwaitingOtp;
            ViewBag.LastTokens = GoConnectAutoLogin.LastTokens;
            ViewBag.LastTokensAt = GoConnectAutoLogin.LastTokensAt;
            return View();
        }

        /// <summary>Bấm nút: gửi lệnh đăng nhập, OTP sẽ được hỏi và trả lời qua Telegram.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Start()
        {
            // Đăng nhập cần trình duyệt (Chromium) nên chỉ chạy ở máy đăng nhập; bản chạy thật
            // chặn lại, giống lệnh /hrm trên Telegram.
            if (!AppSettings.GoConnect.IsLoginMachine)
            {
                NotifyError("Đăng nhập GoConnect chỉ chạy được ở máy local (có trình duyệt). "
                            + "Trên bản chạy thật chỉ dùng chức năng đồng bộ.");
                return RedirectToAction("Index");
            }

            var phone = AppSettings.GoConnect.DefaultPhone;
            if (string.IsNullOrWhiteSpace(phone))
            {
                NotifyError("Chưa cấu hình GoConnect:DefaultPhone.");
                return RedirectToAction("Index");
            }

            if (!AppSettings.GoConnect.HasToken || !AppSettings.GoConnect.HasChatId)
            {
                NotifyError("Chưa cấu hình bot Telegram (GoConnect:BotToken/ChatId) nên không gửi được lệnh OTP.");
                return RedirectToAction("Index");
            }

            if (GoConnectAutoLogin.State == GoConnectState.RequestingOtp
                || GoConnectAutoLogin.State == GoConnectState.AwaitingOtp
                || GoConnectAutoLogin.State == GoConnectState.Verifying)
            {
                Notify("Đang có một phiên đăng nhập chạy dở. Kiểm tra Telegram để nhập OTP.");
                return RedirectToAction("Index");
            }

            Action<string> notify = m => TelegramClient.SendMessage(
                AppSettings.GoConnect.ActiveBotToken, AppSettings.GoConnect.ChatId, m);

            // Chạy nền có đăng ký với ASP.NET; OTP sẽ được hỏi/trả lời qua Telegram.
            HostingEnvironment.QueueBackgroundWorkItem(async ct => await GoConnectAutoLogin.RunAsync(phone, notify));

            Notify("Đã gửi lệnh đăng nhập. Mở Telegram, chờ bot hỏi OTP rồi trả lời mã.");
            return RedirectToAction("Index");
        }
    }
}
