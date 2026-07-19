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

        public bool IsInRole(string role)
        {
            return string.Equals(AppIdentity.Role, role, StringComparison.OrdinalIgnoreCase);
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
    /// Yêu cầu đăng nhập; nếu truyền <see cref="RequiredRole"/> thì yêu cầu đúng quyền đó.
    /// Người đã đăng nhập nhưng sai quyền sẽ nhận 403 thay vì bị đá về trang đăng nhập.
    /// </summary>
    public class AppAuthorizeAttribute : ActionFilterAttribute
    {
        public string RequiredRole { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var principal = filterContext.HttpContext.User as AppPrincipal;

            if (principal == null || !principal.Identity.IsAuthenticated)
            {
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

            if (!string.IsNullOrEmpty(RequiredRole) && !principal.IsInRole(RequiredRole))
            {
                var result = new ViewResult { ViewName = "Error" };
                result.ViewData["Title"] = "Không đủ quyền";
                result.ViewData["Message"] = "Bạn không có quyền truy cập chức năng này. "
                                             + "Chức năng quản trị người dùng chỉ dành cho tài khoản Admin.";
                filterContext.Result = result;
                filterContext.HttpContext.Response.StatusCode = 403;
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
