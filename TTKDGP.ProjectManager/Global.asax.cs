using System;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using TTKDGP.ProjectManager.Infrastructure;

namespace TTKDGP.ProjectManager
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Ẩn header lộ phiên bản ASP.NET MVC.
            MvcHandler.DisableMvcResponseHeader = true;
        }

        /// <summary>Dựng lại thông tin người dùng từ cookie đăng nhập cho mỗi request.</summary>
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null || string.IsNullOrEmpty(cookie.Value)) return;

            try
            {
                var ticket = FormsAuthentication.Decrypt(cookie.Value);
                if (ticket == null || ticket.Expired) return;

                var principal = AuthHelper.BuildPrincipal(ticket);
                if (principal == null) return;

                Context.User = principal;
                Thread.CurrentPrincipal = principal;
            }
            catch (ArgumentException)
            {
                // Cookie hỏng hoặc được mã hoá bằng machineKey khác — coi như chưa đăng nhập.
                FormsAuthentication.SignOut();
            }
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Dữ liệu và giao diện đều bằng tiếng Việt, cố định culture để sắp xếp và định dạng nhất quán.
            var culture = new CultureInfo("vi-VN");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}
