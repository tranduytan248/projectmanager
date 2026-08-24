using System;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>Thông tin người dùng lấy từ vé đăng nhập, gắn vào HttpContext.User.</summary>
    public class AppIdentity : IIdentity
    {
        public int UserId { get; private set; }
        public string FullName { get; private set; }
        public string Role { get; private set; }

        public AppIdentity(int userId, string userName, string fullName, string role)
        {
            UserId = userId;
            Name = userName;
            FullName = fullName;
            Role = role;
        }

        public string Name { get; private set; }
        public string AuthenticationType { get { return "Forms"; } }
        public bool IsAuthenticated { get { return UserId > 0; } }
    }

    public class AppPrincipal : IPrincipal
    {
        public AppPrincipal(AppIdentity identity)
        {
            AppIdentity = identity;
        }

        public AppIdentity AppIdentity { get; private set; }
        public IIdentity Identity { get { return AppIdentity; } }

        /// <summary>Tài khoản có thể mang nhiều quyền nên phải tách chuỗi ra rồi đối chiếu.</summary>
        public bool IsInRole(string role)
        {
            return Models.Roles.Has(AppIdentity.Role, role);
        }
    }

    public static class AuthHelper
    {
        private const char Separator = '|';

        /// <summary>Phát hành vé đăng nhập, nhồi Id/họ tên/quyền vào phần UserData.</summary>
        public static void SignIn(HttpContextBase context, User user, bool persistent)
        {
            var userData = string.Join(Separator.ToString(),
                user.Id.ToString(),
                user.FullName ?? string.Empty,
                user.Role ?? Models.Roles.Manager);

            var ticket = new FormsAuthenticationTicket(
                1,
                user.UserName,
                DateTime.Now,
                DateTime.Now.AddHours(persistent ? 24 * 14 : 8),
                persistent,
                userData,
                FormsAuthentication.FormsCookiePath);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Path = FormsAuthentication.FormsCookiePath,
                Secure = FormsAuthentication.RequireSSL
            };
            if (persistent) cookie.Expires = ticket.Expiration;

            context.Response.Cookies.Add(cookie);
        }

        public static void SignOut()
        {
            FormsAuthentication.SignOut();
        }

        /// <summary>
        /// Dựng lại principal từ vé đăng nhập. Trả về null nếu vé không hợp lệ.
        /// Họ tên và quyền được đọc lại từ dữ liệu chứ không lấy trong vé, để việc khoá tài khoản,
        /// đổi quyền hay đổi họ tên có hiệu lực ngay mà không cần chờ người dùng đăng nhập lại.
        /// </summary>
        public static AppPrincipal BuildPrincipal(FormsAuthenticationTicket ticket)
        {
            if (ticket == null) return null;

            var parts = (ticket.UserData ?? string.Empty).Split(Separator);
            if (parts.Length < 1) return null;

            int userId;
            if (!int.TryParse(parts[0], out userId) || userId <= 0) return null;

            var user = Data.Repository.Users.Find(userId);
            if (user == null || !user.IsActive) return null;

            return new AppPrincipal(new AppIdentity(userId, user.UserName, user.FullName, user.Role));
        }
    }

    /// <summary>
    /// Yêu cầu đăng nhập; thêm <see cref="RequiredRole"/> để bắt đúng một quyền, hoặc
    /// <see cref="AllowedRoles"/> (danh sách ngăn bởi dấu phẩy) để cho phép nhiều quyền.
    /// Không khai gì thì mọi tài khoản đã đăng nhập đều vào được.
    /// Người đã đăng nhập nhưng sai quyền sẽ nhận 403 thay vì bị đá về trang đăng nhập.
    /// </summary>
    public class AppAuthorizeAttribute : ActionFilterAttribute
    {
        public string RequiredRole { get; set; }

        /// <summary>Danh sách quyền được phép, ngăn nhau bởi dấu phẩy (ví dụ <c>Roles.Management</c>).</summary>
        public string AllowedRoles { get; set; }

        /// <summary>
        /// Mã chức năng bắt buộc, dạng "module.action" (ví dụ "projects.edit"). Đây là cách
        /// đánh dấu chức năng mới: tài khoản phải thuộc nhóm quyền có cấp mã này mới vào được.
        /// Có thể khai nhiều mã ngăn phẩy — chỉ cần có MỘT là qua (dùng cho form gộp Thêm/Sửa).
        /// </summary>
        public string Permission { get; set; }

        private bool IsAllowed(AppPrincipal principal)
        {
            // Ưu tiên kiểm theo mã chức năng nếu có khai.
            if (!string.IsNullOrEmpty(Permission))
            {
                var role = principal.AppIdentity.Role;
                foreach (var code in Permission.Split(','))
                {
                    var c = code.Trim();
                    if (c.Length > 0 && Models.Permissions.UserHas(role, c)) return true;
                }
                return false;
            }

            if (!string.IsNullOrEmpty(RequiredRole)) return principal.IsInRole(RequiredRole);
            if (string.IsNullOrEmpty(AllowedRoles)) return true;

            foreach (var role in AllowedRoles.Split(','))
            {
                var name = role.Trim();
                if (name.Length > 0 && principal.IsInRole(name)) return true;
            }
            return false;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Đây là ActionFilter chứ không phải AuthorizeFilter nên phải tự kiểm tra
            // AllowAnonymous, dùng cho các endpoint máy gọi máy (ví dụ Task Scheduler).
            if (filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true) ||
                filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true))
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            var principal = filterContext.HttpContext.User as AppPrincipal;

            if (principal == null || !principal.Identity.IsAuthenticated)
            {
                if (filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    filterContext.Result = new JsonResult
                    {
                        Data = new { success = false, error = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.", unauthenticated = true },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                    filterContext.HttpContext.Response.StatusCode = 401;
                    filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                    return;
                }

                var returnUrl = filterContext.HttpContext.Request.Url == null
                    ? "/"
                    : filterContext.HttpContext.Request.Url.PathAndQuery;

                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Account" },
                    { "action", "Login" },
                    { "returnUrl", returnUrl }
                });
                return;
            }

            if (!IsAllowed(principal))
            {
                var result = new ViewResult { ViewName = "Error" };
                result.ViewData["Title"] = "Không đủ quyền";
                result.ViewData["Message"] = "Tài khoản của bạn không có quyền truy cập chức năng này. "
                                             + "Nếu bạn dùng tài khoản Báo cáo công việc thì chỉ vào được "
                                             + "màn hình Tổng hợp và Báo cáo của tôi.";
                filterContext.Result = result;
                filterContext.HttpContext.Response.StatusCode = 403;
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
