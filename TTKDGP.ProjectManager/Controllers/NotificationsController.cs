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
    [AppAuthorize(RequiredRole = Roles.Admin)]
    public class NotificationsController : BaseController
    {
        public ActionResult Index()
        {
            return View(BuildModel());
        }

        /// <summary>Gửi ngay một kỳ nhắc, không chờ tới lịch.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Send(ReminderKind kind)
        {
            var log = ReminderService.Run(kind, DateTime.Now, true, CurrentUser.Name);

            if (log.Success && log.MissingProjectCount == 0)
            {
                Notify("Không có dự án nào thiếu báo cáo nên chưa gửi tin nào.");
            }
            else if (log.Success)
            {
                Notify(string.Format("Đã gửi nhắc {0} dự án của {1} PM lên nhóm Telegram.",
                    log.MissingProjectCount, log.PmCount));
            }
            else
            {
                NotifyError("Gửi thất bại: " + log.Error);
            }

            return RedirectToAction("Index");
        }

        /// <summary>Dò chat id của các nhóm mà bot đang tham gia.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                MondayHour = AppSettings.Reminder.MondayHour,
                FridayHour = AppSettings.Reminder.FridayHour,
                SaturdayHour = AppSettings.Reminder.SaturdayHour,
                MondayPreview = ReminderService.Build(ReminderKind.MondayPreviousWeek, now),
                FridayPreview = ReminderService.Build(ReminderKind.FridayCurrentWeek, now),
                SaturdayPreview = ReminderService.Build(ReminderKind.SaturdayAdminSummary, now),
                History = Repository.ReminderLogs.All()
                    .OrderByDescending(l => l.SentAt)
                    .Take(20)
                    .ToList()
            };

            if (model.HasToken)
            {
                var me = TelegramClient.GetMe(AppSettings.Telegram.BotToken);
                if (me.Ok)
                {
                    try
                    {
                        model.BotName = (string)JObject.Parse(me.RawResponse)["result"]["username"];
                    }
                    catch
                    {
                        model.BotName = "(không đọc được tên bot)";
                    }
                }
                else
                {
                    model.ConnectionError = me.Error;
                }
            }

            return model;
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
