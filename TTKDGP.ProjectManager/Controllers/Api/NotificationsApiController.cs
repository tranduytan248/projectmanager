using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Models.Api;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>
    /// Man "Thong bao" cua mobile — tuong duong UserNotificationsController ben web, nhung tra
    /// danh sach phan trang day du (dung PagedList y het cac man khac) vi day la mot man rieng
    /// co the cuon/tai them, khong phai khung tha xuong gioi han so dong nhu Panel ben web.
    /// </summary>
    [ApiAuthorize]
    public class NotificationsApiController : BaseController
    {
        private const int PageSize = 20;

        [HttpGet]
        public ActionResult Index(int page = 1)
        {
            var userId = CurrentUserId;

            // UnreadCount da bao gom sinh thong bao "sap den han" con thieu (EnsureDueSoon) —
            // goi truoc de danh sach ben duoi luon co du, khong can goi rieng lan nua.
            var unreadCount = NotificationService.UnreadCount(userId);

            var all = Repository.UserNotifications.All()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .ToList();

            var paged = PagedList<UserNotification>.From(all, page, PageSize);

            var dto = new NotificationListDto
            {
                Items = paged.Items.Select(ApiMappers.ToDto).ToList(),
                Page = paged.Pager.Page,
                HasMore = paged.Pager.HasNext,
                UnreadCount = unreadCount
            };

            return Json(dto, JsonRequestBehavior.AllowGet);
        }

        /// <summary>Danh dau MOT thong bao la da doc — goi khi nguoi dung bam vao mot dong, truoc
        /// khi dieu huong sang man tuong ung (mobile tu quyet dinh diem den theo Type/ProjectId/
        /// TaskId, khong can may chu redirect nhu ben web).</summary>
        [HttpPost]
        public ActionResult MarkRead(int id)
        {
            var notification = Repository.UserNotifications.Find(id);
            if (notification == null || notification.UserId != CurrentUserId) return HttpNotFound();

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = System.DateTime.Now;
                Repository.UserNotifications.Update(notification);
            }

            return Json(new { ok = true });
        }

        [HttpPost]
        public ActionResult MarkAllRead()
        {
            NotificationService.MarkAllRead(CurrentUserId);
            return Json(new { ok = true });
        }
    }
}
