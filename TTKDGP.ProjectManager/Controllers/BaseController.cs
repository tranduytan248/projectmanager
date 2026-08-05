using System;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    public abstract class BaseController : Controller
    {
        /// <summary>Người dùng đang đăng nhập, null nếu khách vãng lai.</summary>
        protected AppIdentity CurrentUser
        {
            get
            {
                var principal = HttpContext == null ? null : HttpContext.User as AppPrincipal;
                return principal == null || !principal.Identity.IsAuthenticated ? null : principal.AppIdentity;
            }
        }

        /// <summary>
        /// Id tài khoản đang đăng nhập; 0 nếu khách vãng lai. Công việc và dự án gán thẳng theo
        /// tài khoản (bảng Users) nên đây là mấu chốt để biết "việc này có phải của tôi không".
        /// </summary>
        protected int CurrentUserId
        {
            get { return CurrentUser == null ? 0 : CurrentUser.UserId; }
        }

        /// <summary>Tài khoản đang đăng nhập có mã chức năng này không.</summary>
        protected bool Can(string permission)
        {
            return CurrentUser != null && Permissions.UserHas(CurrentUser.Role, permission);
        }

        /// <summary>
        /// Là Quản lý Tổ — vai được xem và sửa MỌI dự án, MỌI đầu việc của tổ.
        ///
        /// Nay là một quyền riêng (<c>wteam.manage</c>), tích được trên màn Nhóm quyền. Trước đây
        /// vai này suy ra từ quyền sửa dự án: cấp <c>wprojects.edit</c> cho ai là vô tình cho họ
        /// thấy việc của cả tổ, mà nhìn màn Nhóm quyền không đoán ra — chính chỗ này gây rối.
        ///
        /// Vẫn chấp nhận <c>wprojects.edit</c> làm lối cũ để bản đang vận hành không ai mất quyền
        /// giữa chừng. Seeder cấp <c>wteam.manage</c> cho nhóm Quản lý ngay lần khởi động đầu;
        /// khi các nhóm đã có quyền mới thì bỏ vế thứ hai đi.
        /// </summary>
        protected bool IsTeamManager
        {
            get
            {
                return Can(Permissions.Team.Perm("manage"))
                    || Can(Permissions.WorkProjects.Perm(Permissions.Edit));
            }
        }

        /// <summary>
        /// Tài khoản đang đăng nhập có phải PM của dự án này không.
        ///
        /// "PM" KHÔNG phải một nhóm quyền — cùng một người là PM ở dự án này nhưng chỉ là thành
        /// viên ở dự án khác. Vì vậy quyền của PM luôn phải kiểm hai lớp: thuộc tính AppAuthorize
        /// chặn ở mức chức năng, còn hàm này chặn theo đúng dự án đang thao tác. Thiếu lớp thứ hai
        /// thì PM dự án A sửa được dự án B chỉ bằng cách đổi id trên thanh địa chỉ.
        /// </summary>
        protected bool IsPmOf(int projectId)
        {
            if (projectId <= 0 || CurrentUserId <= 0) return false;

            var project = Repository.WorkProjects.Find(projectId);
            return project != null && project.PmUserId == CurrentUserId;
        }

        /// <summary>
        /// Được sửa nội dung của dự án này không: hoặc là PM của chính nó, hoặc là Quản lý Tổ.
        /// </summary>
        protected bool CanEditProject(int projectId)
        {
            return IsTeamManager || IsPmOf(projectId);
        }

        /// <summary>
        /// Được sửa đầu việc này không.
        ///
        /// Hai đường: PM của dự án và Quản lý Tổ sửa mọi việc trong dự án; người còn lại chỉ sửa
        /// được việc giao cho CHÍNH MÌNH. Xem thêm <see cref="CanEditAllOfTask"/> — người thực hiện
        /// sửa được tiến độ việc của mình nhưng không được tự dời hạn hay đẩy việc sang người khác.
        /// </summary>
        protected bool CanEditTask(WorkTask task)
        {
            if (task == null) return false;
            if (CanEditProject(task.ProjectId)) return true;

            return CurrentUserId > 0 && task.AssigneeUserId == CurrentUserId;
        }

        /// <summary>
        /// Được nhìn thấy đầu việc này không. Trả về false thì nơi gọi phải trả 404 chứ không phải
        /// 403 — để không lộ ra là đầu việc đó có tồn tại.
        ///
        /// Việc ngoài dự án có ProjectId bằng 0 nên không thể chỉ dựa vào quyền xem dự án; phải xét
        /// người được giao trước, nếu không người nhận việc lẻ lại không mở được việc của chính mình.
        /// </summary>
        protected bool CanSeeTask(WorkTask task)
        {
            if (task == null) return false;
            if (CurrentUserId > 0 && task.AssigneeUserId == CurrentUserId) return true;
            if (task.ProjectId > 0) return CanViewProject(task.ProjectId);

            return IsTeamManager;
        }

        /// <summary>
        /// Được sửa MỌI trường của đầu việc không — tức là PM của dự án hoặc Quản lý Tổ.
        ///
        /// Người thực hiện chỉ được đụng vào phần báo cáo (trạng thái, tiến độ, mô tả). Hạn hoàn
        /// thành và người thực hiện phải nằm ngoài tầm tay họ: hạn là căn cứ DUY NHẤT để chấm đúng
        /// hay trễ hạn, tự dời được thì điểm chất lượng không còn ý nghĩa.
        /// </summary>
        protected bool CanEditAllOfTask(WorkTask task)
        {
            return task != null && CanEditProject(task.ProjectId);
        }

        /// <summary>
        /// Được xem dự án này không: Quản lý Tổ xem tất cả, còn lại phải đang hoặc đã từng tham gia.
        /// </summary>
        protected bool CanViewProject(int projectId)
        {
            if (IsTeamManager) return true;
            if (CurrentUserId <= 0) return false;
            if (IsPmOf(projectId)) return true;

            var userId = CurrentUserId;
            return Repository.WorkAssignments.FirstOrDefault(
                a => a.ProjectId == projectId && a.UserId == userId) != null;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = CurrentUser;
            ViewBag.CurrentUser = user;
            ViewBag.IsAdmin = user != null && Models.Roles.Has(user.Role, Models.Roles.Admin);

            // Hàm kiểm quyền cho view: dùng để ẩn/hiện nút Thêm/Sửa/Xóa theo chức năng.
            var role = user == null ? null : user.Role;
            ViewBag.Can = (Func<string, bool>)(perm => Models.Permissions.UserHas(role, perm));

            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Chuỗi truy vấn của danh sách người dùng đang xem — gồm cả bộ lọc lẫn số trang.
        /// Các form và liên kết trên màn danh sách gửi kèm ô ẩn "returnQuery" để sau khi lưu/xoá
        /// còn quay lại đúng chỗ cũ.
        /// </summary>
        protected string ListQuery
        {
            get
            {
                var q = Request == null ? null : Request["returnQuery"];
                return string.IsNullOrWhiteSpace(q) ? string.Empty : q;
            }
        }

        /// <summary>
        /// Quay về màn danh sách, giữ nguyên bộ lọc và trang đang xem.
        ///
        /// Không giữ thì mỗi lần sửa một dòng ở trang 5 là người dùng bị ném về trang 1 và mất hết
        /// điều kiện lọc — phải lọc lại từ đầu cho từng dòng.
        ///
        /// Số trang được màn danh sách tự kéo về trang cuối nếu trang cũ không còn (xoá hết dòng ở
        /// trang cuối chẳng hạn), nên ở đây không cần xử lý gì thêm.
        /// </summary>
        protected ActionResult BackToList(string action = "Index", object routeValues = null)
        {
            var url = routeValues == null ? Url.Action(action) : Url.Action(action, routeValues);
            var query = ListQuery.TrimStart('?');

            if (query.Length > 0)
            {
                url += (url.IndexOf('?') >= 0 ? "&" : "?") + query;
            }

            return Redirect(url);
        }

        /// <summary>
        /// Kết quả sau khi lưu thành công.
        ///
        /// Gửi từ hộp thoại thì chỉ trả JSON báo xong, để trình duyệt tự nạp lại đúng địa chỉ đang
        /// đứng — nhờ vậy bộ lọc và số trang giữ nguyên. Gửi kiểu thường thì chuyển trang như cũ.
        /// </summary>
        /// <param name="goToTarget">
        /// Đặt true khi lưu xong phải sang hẳn màn khác chứ không ở lại chỗ cũ (ví dụ thêm dự án
        /// xong thì sang màn phân công nhân sự). Lúc đó JSON mang kèm địa chỉ đích để hộp thoại
        /// biết đường đi, thay vì nạp lại trang đang đứng.
        /// </param>
        protected ActionResult Saved(string action, object routeValues = null, bool goToTarget = false)
        {
            if (Request.IsAjaxRequest())
            {
                return goToTarget
                    ? Json(new { ok = true, redirect = Url.Action(action, routeValues) })
                    : Json(new { ok = true });
            }

            return routeValues == null
                ? RedirectToAction(action)
                : RedirectToAction(action, routeValues);
        }

        /// <summary>Thông báo thành công hiển thị sau khi chuyển trang.</summary>
        protected void Notify(string message)
        {
            TempData["Flash"] = message;
        }

        protected void NotifyError(string message)
        {
            TempData["FlashError"] = message;
        }
    }
}
