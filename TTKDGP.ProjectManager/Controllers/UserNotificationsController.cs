using System;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Thông báo cá nhân — panel thả xuống từ chuông trên thanh đầu trang. Không cần quyền riêng:
    /// ai đăng nhập cũng có thông báo của chính mình, và chỉ đọc được của chính mình.
    /// </summary>
    [AppAuthorize]
    public class UserNotificationsController : BaseController
    {
        /// <summary>Danh sách thông báo mới nhất, nạp vào panel bằng AJAX khi bấm chuông.</summary>
        [HttpGet]
        public ActionResult Panel()
        {
            return PartialView("_Panel", NotificationService.Recent(CurrentUserId));
        }

        /// <summary>
        /// Mở một thông báo: đánh dấu đã đọc rồi chuyển tới màn tương ứng với loại thông báo —
        /// vào/rút dự án đưa về "Dự án của tôi", nhắc tên và sắp đến hạn đưa vào checklist của
        /// dự án chứa đầu việc.
        /// </summary>
        public ActionResult Open(int id)
        {
            var notification = Repository.UserNotifications.Find(id);
            if (notification == null || notification.UserId != CurrentUserId) return HttpNotFound();

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                Repository.UserNotifications.Update(notification);
            }

            if (notification.Type == NotificationTypes.ProjectAdded
                || notification.Type == NotificationTypes.ProjectRemoved)
            {
                return RedirectToAction("Projects", "MyWork");
            }

            if (notification.ProjectId > 0)
            {
                return RedirectToAction("Index", "Checklist", new { projectId = notification.ProjectId });
            }

            // Việc ngoài dự án không có checklist — rơi về trang chi tiết của chính đầu việc.
            if (notification.TaskId > 0)
            {
                return RedirectToAction("Detail", "MyWork", new { id = notification.TaskId });
            }

            return RedirectToAction("Tasks", "MyWork");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllRead()
        {
            NotificationService.MarkAllRead(CurrentUserId);
            return Json(new { ok = true });
        }
    }
}
