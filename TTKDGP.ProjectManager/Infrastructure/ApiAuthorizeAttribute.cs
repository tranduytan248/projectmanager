using System;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Xac thuc cho cac action API (mobile) — doc header "Authorization: Bearer &lt;token&gt;" thay
    /// vi Forms Authentication cookie ma <see cref="AppAuthorizeAttribute"/> dung cho web.
    ///
    /// Vao duoc thi gan HttpContext.User bang CUNG mot AppPrincipal/AppIdentity ma web dung
    /// (xem <see cref="AuthHelper.BuildPrincipal"/>), nho vay moi ham kiem quyen co san cua
    /// BaseController (CurrentUserId, CanViewProject, CanEditProject...) dung duoc nguyen xi
    /// trong controller API — khong phai viet lai mot bo quyen rieng.
    /// </summary>
    public class ApiAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var header = filterContext.HttpContext.Request.Headers["Authorization"];
            var token = ExtractBearerToken(header);

            if (string.IsNullOrEmpty(token))
            {
                Reject(filterContext, "Thieu token dang nhap.");
                return;
            }

            var apiToken = Repository.ApiTokens.FirstOrDefault(t => t.Token == token);
            if (apiToken == null || apiToken.ExpiresAt <= DateTime.Now)
            {
                Reject(filterContext, "Token khong hop le hoac da het han.");
                return;
            }

            var user = Repository.Users.Find(apiToken.UserId);
            if (user == null || !user.IsActive)
            {
                Reject(filterContext, "Tai khoan khong con hoat dong.");
                return;
            }

            filterContext.HttpContext.User =
                new AppPrincipal(new AppIdentity(user.Id, user.UserName, user.FullName, user.Role));

            base.OnActionExecuting(filterContext);
        }

        private static string ExtractBearerToken(string header)
        {
            const string prefix = "Bearer ";
            if (string.IsNullOrEmpty(header) || !header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return header.Substring(prefix.Length).Trim();
        }

        private static void Reject(ActionExecutingContext filterContext, string message)
        {
            var response = filterContext.HttpContext.Response;
            response.StatusCode = 401;
            response.TrySkipIisCustomErrors = true;

            // Web nay dung Forms Authentication (xem Web.config): FormsAuthenticationModule tu
            // dong doi MOI response ma status = 401 thanh redirect 302 sang /Account/Login o
            // buoc EndRequest, du filter nay da tra JSON that. Dong len thi API nhan token gia
            // hoac thieu token deu thay 302 kem HTML thay vi JSON 401 nhu mong doi. Thuoc tinh
            // nay (co tu .NET 4.5) la duong tat chinh thuc de tat hanh vi do CHI cho response
            // hien tai, khong dung toi <authorization>/web.config.
            response.SuppressFormsAuthenticationRedirect = true;

            filterContext.Result = new JsonResult
            {
                Data = new { error = message },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
    }
}
