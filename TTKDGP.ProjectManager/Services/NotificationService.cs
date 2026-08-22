using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Phát và đọc thông báo cá nhân (chuông trên thanh đầu trang).
    ///
    /// Nguyên tắc: thông báo là thứ PHỤ — mọi hàm phát đều nuốt lỗi hạ tầng, tuyệt đối không để
    /// việc ghi thông báo làm hỏng thao tác chính (thêm nhân sự, gửi trao đổi...).
    /// </summary>
    public static class NotificationService
    {
        // ---------- Phát thông báo ----------

        public static UserNotification Add(int userId, string type, string message, int projectId = 0, int taskId = 0)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(message)) return null;

            try
            {
                var n = Repository.UserNotifications.Insert(new UserNotification
                {
                    UserId = userId,
                    Type = type,
                    Message = message,
                    ProjectId = projectId,
                    TaskId = taskId,
                    CreatedAt = DateTime.Now
                });

                // Tự động gửi Push Notification tới thiết bị đăng ký mới nhất của nhân viên
                try
                {
                    var title = ResolveTitle(type);
                    FcmPushService.SendToUser(userId, title, message, type, projectId, taskId);
                }
                catch
                {
                    // Nuốt lỗi FCM
                }

                return n;
            }
            catch (Exception)
            {
                // SQL chập chờn thì thôi thông báo này — không làm hỏng thao tác chính.
                return null;
            }
        }

        private static string ResolveTitle(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "Thông báo mới";
            switch (type)
            {
                case NotificationTypes.TaskAssigned:
                case NotificationTypes.ProjectTaskAssigned:
                    return "Giao việc mới";
                case NotificationTypes.TodoAdded:
                    return "Việc con mới";
                case NotificationTypes.TodoToggled:
                    return "Cập nhật việc con";
                case NotificationTypes.CommentAdded:
                    return "Trao đổi mới";
                case NotificationTypes.Mentioned:
                    return "Được nhắc trong trao đổi";
                case NotificationTypes.DueSoon:
                    return "Nhắc việc đến hạn";
                case NotificationTypes.ProjectAdded:
                    return "Thêm vào dự án";
                case NotificationTypes.ProjectRemoved:
                    return "Rút khỏi dự án";
                case NotificationTypes.LeaveRequested:
                    return "Đơn nghỉ phép mới";
                case NotificationTypes.LeaveResult:
                    return "Kết quả duyệt nghỉ phép";
                default:
                    return "Thông báo mới";
            }
        }

        /// <summary>
        /// Liên kết đặt trong mail: mở thông báo (tự đánh dấu đã đọc rồi chuyển tới đích).
        /// Action đích có [AppAuthorize] nên chưa đăng nhập sẽ bị đưa qua màn đăng nhập trước.
        /// </summary>
        private static string OpenLink(UserNotification notification)
        {
            var baseUrl = Infrastructure.AppSettings.PublicLink ?? string.Empty;
            return notification == null ? baseUrl : baseUrl + "/UserNotifications/Open/" + notification.Id;
        }

        public static void ProjectAdded(int userId, WorkProject project)
        {
            if (project == null) return;
            var n = Add(userId, NotificationTypes.ProjectAdded,
                string.Format("Bạn được thêm vào dự án \"{0}\".", project.Name), project.Id);

            EmailTemplateService.Send(userId, NotificationTypes.ProjectAdded,
                new Dictionary<string, string>
                {
                    { "TenDuAn", project.Name },
                    { "LienKet", OpenLink(n) }
                });
        }

        public static void ProjectRemoved(int userId, string projectName, int projectId)
        {
            var n = Add(userId, NotificationTypes.ProjectRemoved,
                string.Format("Bạn đã được rút khỏi dự án \"{0}\".", projectName), projectId);

            EmailTemplateService.Send(userId, NotificationTypes.ProjectRemoved,
                new Dictionary<string, string>
                {
                    { "TenDuAn", projectName },
                    { "LienKet", OpenLink(n) }
                });
        }

        /// <summary>
        /// Báo cho những người bị nhắc tên (@Họ Tên) trong một lượt trao đổi. Chỉ nhắc được người
        /// đang tham gia dự án của đầu việc — đúng bằng danh sách gợi ý khi gõ @; người tự nhắc
        /// chính mình thì không báo. Trao đổi có file đính kèm thì mail kèm liên kết tải (liên
        /// kết đi qua action có kiểm quyền nên bắt buộc đăng nhập mới xem được).
        /// </summary>
        public static void Mentions(WorkTask task, WorkComment comment, int actorUserId, string actorName)
        {
            if (task == null || comment == null || string.IsNullOrWhiteSpace(comment.Content)) return;

            var content = comment.Content;
            var actor = string.IsNullOrWhiteSpace(actorName) ? "Một người" : actorName;

            // Dò tên trên bản ĐÃ GỠ THẺ. Nội dung trao đổi soạn bằng trình soạn thảo nên thẻ có
            // thể nằm xen giữa một cái tên (bôi đậm mỗi chữ cuối chẳng hạn) — so trên chuỗi HTML
            // thô sẽ trượt và người được nhắc lặng lẽ không nhận được gì.
            var plain = Infrastructure.HtmlSanitizer.ToPlainText(content);

            // Mảnh HTML liên kết file đính kèm — do hệ thống dựng nên mới được thay nguyên vẹn.
            var attachmentHtml = comment.HasAttachment
                ? string.Format("<p>File đính kèm: <a href=\"{0}\">{1}</a> <em>(đăng nhập để tải)</em></p>",
                    System.Web.HttpUtility.HtmlEncode(Infrastructure.AppSettings.PublicLink
                        + "/Checklist/Attachment?commentId=" + comment.Id),
                    System.Web.HttpUtility.HtmlEncode(comment.AttachmentName))
                : string.Empty;

            foreach (var person in WorkService.TaskParticipants(task))
            {
                if (person.Id == actorUserId || string.IsNullOrWhiteSpace(person.FullName)) continue;

                if (plain.IndexOf("@" + person.FullName.Trim(),
                        StringComparison.CurrentCultureIgnoreCase) < 0) continue;

                var n = Add(person.Id, NotificationTypes.Mentioned,
                    string.Format("{0} đã nhắc bạn trong trao đổi của công việc \"{1}\".", actor, task.Title),
                    task.ProjectId, task.Id);

                EmailTemplateService.Send(person.Id, NotificationTypes.Mentioned,
                    new Dictionary<string, string>
                    {
                        { "NguoiNhac", actor },
                        { "TenCongViec", task.Title },
                        { "TenDuAn", string.IsNullOrWhiteSpace(task.ProjectName) ? "(ngoài dự án)" : task.ProjectName },
                        { "LienKet", OpenLink(n) }
                    },
                    // NoiDung: dua vao textValues se bi RenderHtml ma hoa THEM MOT LAN, hien ra
                    // the tho (&lt;div&gt;...) trong mail thay vi dinh dang that. Phai o htmlValues
                    // (khong ma hoa) giong TepDinhKem; qua ToDisplay truoc — cung ham web dung de
                    // hien "Trao doi" — de chan luon truong hop noi dung chua duoc loc tu luc luu.
                    new Dictionary<string, string>
                    {
                        { "TepDinhKem", attachmentHtml },
                        { "NoiDung", Infrastructure.HtmlSanitizer.ToDisplay(content) }
                    });
            }
        }

        /// <summary>
        /// Báo cho người tạo việc và người đang được giao việc khi có lượt trao đổi mới (web +
        /// email). Không báo cho chính người vừa viết trao đổi; người tạo trùng người được giao
        /// thì chỉ báo một lần.
        /// </summary>
        public static void CommentAdded(WorkTask task, WorkComment comment, int actorUserId, string actorName)
        {
            if (task == null || comment == null) return;

            var actor = string.IsNullOrWhiteSpace(actorName) ? "Một người" : actorName;

            var attachmentHtml = comment.HasAttachment
                ? string.Format("<p>File đính kèm: <a href=\"{0}\">{1}</a> <em>(đăng nhập để tải)</em></p>",
                    System.Web.HttpUtility.HtmlEncode(Infrastructure.AppSettings.PublicLink
                        + "/Checklist/Attachment?commentId=" + comment.Id),
                    System.Web.HttpUtility.HtmlEncode(comment.AttachmentName))
                : string.Empty;

            var recipients = new[] { task.AssignedByUserId, task.AssigneeUserId }
                .Where(userId => userId > 0 && userId != actorUserId)
                .Distinct();

            foreach (var userId in recipients)
            {
                var n = Add(userId, NotificationTypes.CommentAdded,
                    string.Format("{0} vừa gửi trao đổi mới trong công việc \"{1}\".", actor, task.Title),
                    task.ProjectId, task.Id);

                EmailTemplateService.Send(userId, NotificationTypes.CommentAdded,
                    new Dictionary<string, string>
                    {
                        { "NguoiTraoDoi", actor },
                        { "TenCongViec", task.Title },
                        { "TenDuAn", string.IsNullOrWhiteSpace(task.ProjectName) ? "(ngoài dự án)" : task.ProjectName },
                        { "LienKet", OpenLink(n) }
                    },
                    // Xem ghi chu tuong tu trong Mentions() o tren: NoiDung phai o htmlValues va
                    // qua ToDisplay, khong thi mail hien the tho.
                    new Dictionary<string, string>
                    {
                        { "TepDinhKem", attachmentHtml },
                        { "NoiDung", Infrastructure.HtmlSanitizer.ToDisplay(comment.Content) }
                    });
            }
        }

        /// <summary>
        /// Báo cho người tạo việc và người đang được giao việc khi một việc con trong todolist
        /// vừa được tick xong hoặc bỏ tick (web + email). Không báo cho chính người vừa bấm;
        /// người tạo trùng người được giao thì chỉ báo một lần — cùng khuôn với
        /// <see cref="CommentAdded"/>.
        /// </summary>
        public static void TodoToggled(WorkTask task, WorkTaskTodo todo, int actorUserId, string actorName)
        {
            if (task == null || todo == null) return;

            var actor = string.IsNullOrWhiteSpace(actorName) ? "Một người" : actorName;
            var action = todo.IsDone ? "đã hoàn thành" : "bỏ đánh dấu hoàn thành";

            var recipients = new[] { task.AssignedByUserId, task.AssigneeUserId }
                .Where(userId => userId > 0 && userId != actorUserId)
                .Distinct();

            foreach (var userId in recipients)
            {
                var n = Add(userId, NotificationTypes.TodoToggled,
                    string.Format("{0} {1} việc con \"{2}\" trong công việc \"{3}\".",
                        actor, action, todo.Content, task.Title),
                    task.ProjectId, task.Id);

                EmailTemplateService.Send(userId, NotificationTypes.TodoToggled,
                    new Dictionary<string, string>
                    {
                        { "NguoiThucHien", actor },
                        { "HanhDong", action },
                        { "TenViecCon", todo.Content },
                        { "TenCongViec", task.Title },
                        { "TenDuAn", string.IsNullOrWhiteSpace(task.ProjectName) ? "(ngoài dự án)" : task.ProjectName },
                        { "LienKet", OpenLink(n) }
                    });
            }
        }

        /// <summary>
        /// Báo cho người tạo việc và người đang được giao việc khi một việc con MỚI vừa được
        /// thêm vào todolist (web + email). Cùng khuôn với <see cref="TodoToggled"/>.
        /// </summary>
        public static void TodoAdded(WorkTask task, WorkTaskTodo todo, int actorUserId, string actorName)
        {
            if (task == null || todo == null) return;

            var actor = string.IsNullOrWhiteSpace(actorName) ? "Một người" : actorName;

            var recipients = new[] { task.AssignedByUserId, task.AssigneeUserId }
                .Where(userId => userId > 0 && userId != actorUserId)
                .Distinct();

            foreach (var userId in recipients)
            {
                var n = Add(userId, NotificationTypes.TodoAdded,
                    string.Format("{0} vừa thêm việc con mới \"{1}\" trong công việc \"{2}\".",
                        actor, todo.Content, task.Title),
                    task.ProjectId, task.Id);

                EmailTemplateService.Send(userId, NotificationTypes.TodoAdded,
                    new Dictionary<string, string>
                    {
                        { "NguoiThucHien", actor },
                        { "TenViecCon", todo.Content },
                        { "TenCongViec", task.Title },
                        { "TenDuAn", string.IsNullOrWhiteSpace(task.ProjectName) ? "(ngoài dự án)" : task.ProjectName },
                        { "LienKet", OpenLink(n) }
                    });
            }
        }

        /// <summary>
        /// Báo cho người vừa được giao một việc riêng (web + email). Mail kèm liên kết file đính
        /// kèm của việc nếu có — liên kết đi qua action có kiểm quyền nên phải đăng nhập mới tải.
        /// </summary>
        public static void TaskAssigned(WorkTask task, string assignerName)
        {
            if (task == null || task.AssigneeUserId <= 0) return;

            var assigner = Assigner(assignerName);
            var due = DueText(task);

            var n = Add(task.AssigneeUserId, NotificationTypes.TaskAssigned,
                string.Format("{0} giao cho bạn việc riêng \"{1}\" — hạn {2}.", assigner, task.Title, due),
                task.ProjectId, task.Id);

            var attachmentHtml = task.HasAttachment
                ? string.Format("<p>File đính kèm: <a href=\"{0}\">{1}</a> <em>(đăng nhập để tải)</em></p>",
                    System.Web.HttpUtility.HtmlEncode(Infrastructure.AppSettings.PublicLink
                        + "/MyWork/Attachment/" + task.Id),
                    System.Web.HttpUtility.HtmlEncode(task.AttachmentName))
                : string.Empty;

            EmailTemplateService.Send(task.AssigneeUserId, NotificationTypes.TaskAssigned,
                new Dictionary<string, string>
                {
                    { "NguoiGiao", assigner },
                    { "TenCongViec", task.Title },
                    { "HanHoanThanh", due },
                    { "DiemCong", task.BonusPercent.ToString("0.#") + "%" },
                    { "LienKet", OpenLink(n) }
                },
                // Xem ghi chu tuong tu trong CommentAdded() o tren: NoiDung phai o htmlValues va
                // qua ToDisplay, khong thi mail hien the tho.
                new Dictionary<string, string>
                {
                    { "TepDinhKem", attachmentHtml },
                    { "NoiDung", Infrastructure.HtmlSanitizer.ToDisplay(task.Description) }
                });
        }

        /// <summary>
        /// Báo cho người vừa được giao một việc THUỘC DỰ ÁN, hoặc việc vừa chuyển sang cho họ.
        ///
        /// Tách khỏi <see cref="TaskAssigned"/> vì nội dung khác hẳn: việc dự án cần nêu tên dự án
        /// và loại việc, còn điểm cộng thì không có — việc dự án chấm theo bộ 30/70.
        /// </summary>
        public static void ProjectTaskAssigned(WorkTask task, string assignerName)
        {
            if (task == null || task.AssigneeUserId <= 0) return;

            var assigner = Assigner(assignerName);
            var due = DueText(task);
            var project = string.IsNullOrWhiteSpace(task.ProjectName) ? "—" : task.ProjectName;

            var n = Add(task.AssigneeUserId, NotificationTypes.ProjectTaskAssigned,
                string.Format("{0} giao cho bạn công việc \"{1}\" (dự án {2}) — hạn {3}.",
                    assigner, task.Title, project, due),
                task.ProjectId, task.Id);

            EmailTemplateService.Send(task.AssigneeUserId, NotificationTypes.ProjectTaskAssigned,
                new Dictionary<string, string>
                {
                    { "NguoiGiao", assigner },
                    { "TenCongViec", task.Title },
                    { "TenDuAn", project },
                    { "LoaiViec", TaskKinds.Display(task.Kind) },
                    { "HanHoanThanh", due },
                    { "LienKet", OpenLink(n) }
                },
                // Xem ghi chu tuong tu trong CommentAdded() o tren: NoiDung phai o htmlValues va
                // qua ToDisplay, khong thi mail hien the tho.
                new Dictionary<string, string>
                {
                    { "NoiDung", Infrastructure.HtmlSanitizer.ToDisplay(task.Description) }
                });
        }

        /// <summary>Tên người giao; trống thì gọi chung là Quản lý Tổ.</summary>
        private static string Assigner(string assignerName)
        {
            return string.IsNullOrWhiteSpace(assignerName) ? "Quản lý Tổ" : assignerName;
        }

        /// <summary>Hạn hoàn thành dạng chữ; việc chưa đặt hạn thì để dấu gạch.</summary>
        private static string DueText(WorkTask task)
        {
            return task.DueDate.HasValue ? task.DueDate.Value.ToString("dd/MM/yyyy") : "—";
        }

        // ---------- Việc sắp đến hạn ----------

        /// <summary>
        /// Lần quét gần nhất theo từng người, để mỗi người chỉ quét lại sau một quãng — quét mỗi
        /// request là đọc thừa vô ích trong khi hạn chỉ đổi theo NGÀY.
        /// </summary>
        private static readonly ConcurrentDictionary<int, DateTime> _lastDueSweep =
            new ConcurrentDictionary<int, DateTime>();

        private static readonly TimeSpan DueSweepEvery = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Sinh thông báo cho việc của một người đang SẮP đến hạn (còn tối đa 1 ngày) hoặc đã quá
        /// hạn mà chưa từng được báo. Mỗi đầu việc báo đúng MỘT lần — báo lại mỗi ngày sẽ thành
        /// tiếng ồn và người dùng sẽ tắt luôn thói quen đọc thông báo.
        /// </summary>
        private static void EnsureDueSoon(int userId)
        {
            var now = DateTime.Now;
            var last = _lastDueSweep.GetOrAdd(userId, DateTime.MinValue);
            if (now - last < DueSweepEvery) return;
            _lastDueSweep[userId] = now;

            var alreadyNotified = new HashSet<int>(Repository.UserNotifications.All()
                .Where(n => n.UserId == userId && n.Type == NotificationTypes.DueSoon)
                .Select(n => n.TaskId));

            foreach (var task in WorkService.TasksOfUser(userId))
            {
                if (TaskStates.IsClosed(task.State) || !task.DueDate.HasValue) continue;
                if (!task.IsDueSoon && !task.IsOverdue) continue;
                if (alreadyNotified.Contains(task.Id)) continue;

                var message = task.IsOverdue
                    ? string.Format("Công việc \"{0}\" đã QUÁ HẠN (hạn {1:dd/MM/yyyy}).",
                        task.Title, task.DueDate.Value)
                    : string.Format("Công việc \"{0}\" sắp đến hạn ({1:dd/MM/yyyy}).",
                        task.Title, task.DueDate.Value);

                var n = Add(userId, NotificationTypes.DueSoon, message, task.ProjectId, task.Id);

                EmailTemplateService.Send(userId, NotificationTypes.DueSoon,
                    new Dictionary<string, string>
                    {
                        { "TenCongViec", task.Title },
                        { "TenDuAn", string.IsNullOrWhiteSpace(task.ProjectName) ? "(ngoài dự án)" : task.ProjectName },
                        { "HanHoanThanh", task.DueDate.Value.ToString("dd/MM/yyyy") },
                        { "TinhTrang", task.IsOverdue
                            ? string.Format("đã quá hạn {0} ngày", -(task.DaysLeft ?? 0))
                            : (task.DaysLeft == 0 ? "hết hạn hôm nay" : "sắp đến hạn (còn 1 ngày)") },
                        { "LienKet", OpenLink(n) }
                    });
            }
        }

        // ---------- Đọc ----------

        /// <summary>Số thông báo chưa đọc — hiện trên chuông. Lỗi hạ tầng thì coi như 0.</summary>
        public static int UnreadCount(int userId)
        {
            if (userId <= 0) return 0;

            try
            {
                EnsureDueSoon(userId);
                return Repository.UserNotifications.All().Count(n => n.UserId == userId && !n.IsRead);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>Các thông báo mới nhất của một người, cho panel thả xuống.</summary>
        public static List<UserNotification> Recent(int userId, int take = 15)
        {
            if (userId <= 0) return new List<UserNotification>();

            EnsureDueSoon(userId);
            return Repository.UserNotifications.All()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(take)
                .ToList();
        }

        public static void MarkAllRead(int userId)
        {
            var now = DateTime.Now;
            Repository.UserNotifications.UpdateWhere(
                n => n.UserId == userId && !n.IsRead,
                n => { n.IsRead = true; n.ReadAt = now; });
        }
    }
}
