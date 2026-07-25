using System;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Infrastructure;

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
