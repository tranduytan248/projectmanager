using System;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>Cấu hình và theo dõi việc nhắc báo cáo qua Telegram. Chỉ Admin dùng được.</summary>
    [AppAuthorize]
    public class NotificationsController : BaseController
    {
        [AppAuthorize(Permission = "notifications.view")]
        public ActionResult Index()
        {
            return View(BuildModel());
        }

        /// <summary>Gửi ngay mail nhắc cho từng thành viên, không chờ tới sáng thứ Sáu.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "notifications.send")]
        public ActionResult SendMemberEmails()
        {
            var log = ReminderService.RunMemberEmails(DateTime.Now, true, CurrentUser.Name);

            if (log.PmCount == 0)
            {
                Notify("Không có thành viên nào thiếu báo cáo nên chưa gửi mail nào.");
            }
            else if (log.Success && log.NoEmailCount > 0)
            {
                Notify(string.Format("Đã gửi {0} mail. Còn {1} người thiếu báo cáo nhưng chưa điền email.",
                    log.SentCount, log.NoEmailCount));
            }
            else if (log.Success)
            {
                Notify(string.Format("Đã gửi {0} mail nhắc báo cáo.", log.SentCount));
            }
            else
            {
                NotifyError("Gửi mail thất bại: " + log.Error);
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Gửi ngay lượt nhắc hạn công việc bằng SMS, không chờ tới giờ hẹn.
        ///
        /// Ghi nhật ký là lượt THỦ CÔNG nên không chiếm chỗ của lượt tự động trong ngày —
        /// bấm thử lúc 10h thì chiều 17h lịch vẫn chạy như thường.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "notifications.send")]
        public ActionResult SendTaskDueSms()
        {
            var now = DateTime.Now;
            var kind = now.Hour < AppSettings.Reminder.TaskSmsAfternoonHour
                ? ReminderKind.TaskDueSmsMorning
                : ReminderKind.TaskDueSmsAfternoon;

            var log = TaskDueSmsService.Run(kind, now, true, CurrentUser.Name);

            if (log.PmCount == 0)
            {
                Notify(log.Error ?? "Không có ai cần nhắc hôm nay.");
            }
            else if (log.Success && log.NoEmailCount > 0)
            {
                Notify(string.Format("Đã gửi {0} tin. Còn {1} người có việc đến hạn nhưng chưa có số điện thoại.",
                    log.SentCount, log.NoEmailCount));
            }
            else if (log.Success)
            {
                Notify(string.Format("Đã gửi {0} tin nhắc hạn công việc.", log.SentCount));
            }
            else
            {
                NotifyError("Gửi tin thất bại: " + log.Error);
            }

            return RedirectToAction("Index");
        }

        /// <summary>Gửi một mail thử để kiểm tra cấu hình SMTP.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "notifications.send")]
        public ActionResult SendTestEmail(string to)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                NotifyError("Vui lòng nhập địa chỉ nhận mail thử.");
                return RedirectToAction("Index");
            }

            var result = EmailClient.SendTest(to.Trim());

            if (result.Ok)
            {
                Notify("Đã gửi mail thử tới " + to.Trim() + ".");
            }
            else
            {
                NotifyError("Gửi mail thử thất bại: " + result.Error);
            }

            return RedirectToAction("Index");
        }

        /// <summary>Dò chat id của các nhóm mà bot đang tham gia.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "notifications.send")]
        public ActionResult Discover()
        {
            var model = BuildModel();

            TelegramResult result;
            model.DiscoveredChats = TelegramClient.DiscoverChats(AppSettings.Telegram.BotToken, out result);

            if (!result.Ok)
            {
                NotifyError("Không dò được: " + result.Error);
            }
            else if (model.DiscoveredChats.Count == 0)
            {
                NotifyError("Chưa thấy nhóm nào. Hãy thêm bot vào nhóm rồi gửi một tin bất kỳ trong nhóm, "
                            + "sau đó bấm dò lại. Telegram chỉ giữ lịch sử này trong khoảng 24 giờ.");
            }
            else
            {
                Notify(string.Format("Tìm thấy {0} cuộc trò chuyện. Chép chat id vào App_Config\\secrets.config.",
                    model.DiscoveredChats.Count));
            }

            return View("Index", model);
        }

        /// <summary>
        /// Điểm gọi cho Task Scheduler bên ngoài. Bảo vệ bằng khoá trong cấu hình
        /// chứ không dùng đăng nhập, vì Task Scheduler không có phiên làm việc.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Trigger(string key)
        {
            var expected = AppSettings.Reminder.TriggerKey;

            if (string.IsNullOrWhiteSpace(expected))
            {
                Response.StatusCode = 503;
                return Content("Chưa đặt Reminder:TriggerKey trong cấu hình.", "text/plain");
            }

            if (!FixedTimeEquals(key, expected))
            {
                Response.StatusCode = 403;
                return Content("Khoá không hợp lệ.", "text/plain");
            }

            var sent = ReminderScheduler.RunDue(DateTime.Now);
            return Content(string.Format("Đã xử lý {0} kỳ nhắc lúc {1:dd/MM/yyyy HH:mm:ss}.",
                sent, DateTime.Now), "text/plain");
        }

        private NotificationsViewModel BuildModel()
        {
            var now = DateTime.Now;

            var model = new NotificationsViewModel
            {
                TelegramEnabled = AppSettings.Telegram.Enabled,
                HasToken = AppSettings.Telegram.HasToken,
                HasChatId = AppSettings.Telegram.HasChatId,
                MaskedToken = AppSettings.Telegram.MaskedToken,
                ChatId = AppSettings.Telegram.HasChatId ? AppSettings.Telegram.ChatId : "(chưa đặt)",
                GoConnectEnabled = AppSettings.GoConnect.Enabled,
                GoConnectHasToken = AppSettings.GoConnect.HasToken,
                GoConnectHasChatId = AppSettings.GoConnect.HasChatId,
                GoConnectMaskedToken = AppSettings.GoConnect.MaskedToken,
                GoConnectChatId = AppSettings.GoConnect.HasChatId
                    ? AppSettings.GoConnect.ChatId
                    : "(chưa đặt)",
                SharesBotWithReminder = AppSettings.GoConnect.SharesBotWithReminder,
                SharesChatIdWithReminder = AppSettings.GoConnect.SharesChatIdWithReminder,
                EmailEnabled = AppSettings.Email.Enabled,
                EmailHasHost = AppSettings.Email.HasHost,
                EmailHasAccount = AppSettings.Email.HasAccount,
                EmailHost = AppSettings.Email.HasHost ? AppSettings.Email.Host : "(chưa đặt)",
                EmailPort = AppSettings.Email.Port,
                EmailUser = AppSettings.Email.User,
                EmailFrom = AppSettings.Email.From,
                EmailDisplayName = AppSettings.Email.DisplayName,
                EmailMaskedPassword = AppSettings.Email.MaskedPassword,
                MemberReminders = ReminderService.BuildMemberReminders(now),
                MemberEmailHour = AppSettings.Reminder.MemberEmailHour,
                AutoSend = AppSettings.Reminder.AutoSend,
                TaskSmsEnabled = AppSettings.Reminder.TaskSmsEnabled,
                TaskSmsMorningHour = AppSettings.Reminder.TaskSmsMorningHour,
                TaskSmsAfternoonHour = AppSettings.Reminder.TaskSmsAfternoonHour,
                SmsReady = AppSettings.Sms.IsConfigured,
                TaskSmsPreview = TaskDueSmsService.Build(now),
                History = Repository.ReminderLogs.All()
                    .OrderByDescending(l => l.SentAt)
                    .Take(20)
                    .ToList()
            };

            if (model.HasToken)
            {
                string error;
                model.BotName = ResolveBotName(AppSettings.Telegram.BotToken, out error);
                model.ConnectionError = error;
            }

            if (model.GoConnectHasToken)
            {
                // Dùng chung token thì khỏi hỏi Telegram lần nữa — vẫn đúng tên bot mà đỡ một
                // lần gọi mạng cho mỗi lượt mở trang.
                if (model.SharesBotWithReminder)
                {
                    model.GoConnectBotName = model.BotName;
                    model.GoConnectConnectionError = model.ConnectionError;
                }
                else
                {
                    string error;
                    model.GoConnectBotName = ResolveBotName(AppSettings.GoConnect.BotToken, out error);
                    model.GoConnectConnectionError = error;
                }
            }

            return model;
        }

        /// <summary>Hỏi Telegram tên tài khoản của bot ứng với một token. Trả null nếu hỏng.</summary>
        private static string ResolveBotName(string botToken, out string error)
        {
            error = null;

            var me = TelegramClient.GetMe(botToken);
            if (!me.Ok)
            {
                error = me.Error;
                return null;
            }

            try
            {
                return (string)JObject.Parse(me.RawResponse)["result"]["username"];
            }
            catch
            {
                return "(không đọc được tên bot)";
            }
        }

        /// <summary>So sánh không phụ thuộc thời gian, tránh dò khoá qua thời gian phản hồi.</summary>
        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;

            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
