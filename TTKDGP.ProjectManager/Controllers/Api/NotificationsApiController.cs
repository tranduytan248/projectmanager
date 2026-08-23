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

        /// <summary>
        /// Đăng ký FCM Device Token mới nhất của tài khoản trên thiết bị này.
        /// Tự động thay thế token cũ để chỉ gửi tới thiết bị mới nhất.
        /// </summary>
        [HttpPost]
        public ActionResult RegisterDevice(string token, string platform = "Android")
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Json(new { ok = false, message = "Token không được để trống" });
            }

            var user = Repository.Users.Find(CurrentUserId);
            if (user == null) return HttpNotFound();

            user.FcmDeviceToken = token.Trim();
            user.FcmDevicePlatform = string.IsNullOrWhiteSpace(platform) ? "Android" : platform.Trim();
            user.FcmTokenUpdatedAt = System.DateTime.Now;
            Repository.Users.Update(user);

            return Json(new { ok = true, message = "Đã cập nhật thiết bị nhận thông báo thành công" });
        }

        /// <summary>
        /// Gửi thử nghiệm một Push Notification tới tài khoản hiện tại hoặc UserId chỉ định.
        /// Hỗ trợ kiểm thử điều hướng và khả năng nhận notification trên mobile.
        /// </summary>
        [HttpPost]
        public async System.Threading.Tasks.Task<ActionResult> SendTestPush(string title = "Thông báo thử nghiệm", string message = "Kiểm tra gửi Push Notification từ máy chủ", string type = "", int projectId = 0, int taskId = 0, int targetUserId = 0)
        {
            var targetId = targetUserId > 0 ? targetUserId : CurrentUserId;
            var user = Repository.Users.Find(targetId);
            if (user == null)
            {
                return Json(new { ok = false, message = "Không tìm thấy người dùng" });
            }

            if (string.IsNullOrWhiteSpace(user.FcmDeviceToken))
            {
                return Json(new { ok = false, message = "Người dùng chưa đăng ký thiết bị nhận thông báo (FcmDeviceToken rỗng)" });
            }

            var success = await FcmPushService.SendDirectAsync(user.FcmDeviceToken, title, message, type, projectId, taskId);
            return Json(new
            {
                ok = success,
                message = success ? "Đã gửi Push Notification thành công tới thiết bị" : "Không thể gửi Push Notification (hãy kiểm tra firebase-service-account.json hoặc log máy chủ)",
                targetUser = user.FullName,
                deviceToken = user.FcmDeviceToken
            });
        }

        /// <summary>
        /// Gửi Push Notification trao đổi dự án cho tất cả thành viên (trừ người gửi).
        /// </summary>
        [HttpPost]
        public ActionResult SendProjectDiscussionNotification(int projectId, string content, string senderName = "")
        {
            if (projectId <= 0 || string.IsNullOrWhiteSpace(content))
            {
                return Json(new { ok = false, message = "Dữ liệu không hợp lệ" });
            }

            var project = Repository.WorkProjects.Find(projectId);
            if (project == null) return HttpNotFound();

            var currentUserId = CurrentUserId;
            var senderDisplayName = string.IsNullOrWhiteSpace(senderName)
                ? (CurrentUser != null ? CurrentUser.FullName : "Đồng nghiệp")
                : senderName.Trim();

            // Lấy danh sách thành viên dự án (trừ người gửi)
            var members = Repository.WorkAssignments.All()
                .Where(a => a.ProjectId == projectId && a.IsActive && a.UserId != currentUserId)
                .Select(a => a.UserId)
                .Distinct()
                .ToList();

            var trimmedContent = content.Trim();

            foreach (var memberId in members)
            {
                NotificationService.Add(
                    userId: memberId,
                    type: "TraoDoiDuAn",
                    message: string.Format("{0} đã nhắn trong dự án \"{1}\": {2}", senderDisplayName, project.Name, trimmedContent),
                    projectId: projectId
                );
            }

            return Json(new { ok = true, recipientCount = members.Count });
        }
    }
}
