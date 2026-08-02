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

            // Tạo bảng SQL nếu chưa có và nạp dữ liệu từ JSON sang khi bảng còn trống.
            Data.JsonToSqlMigration.RunIfNeeded();

            // Chuyển dữ liệu công việc từ khoá nhân sự (WorkStaff cũ) sang khoá người dùng.
            // Phải đứng TRƯỚC mọi truy cập Repository — store nạp xong mới sửa thì bản trong
            // bộ nhớ vẫn là dữ liệu cũ cho tới lần khởi động sau.
            Data.WorkUserMigration.RunIfNeeded();

            // Tạo ba nhóm quyền mặc định (Quản trị / Quản lý / Báo cáo) nếu bảng còn trống.
            Data.RoleGroupSeeder.EnsureDefaults();

            // Cấp chức năng của các màn hình mới cho hai nhóm gốc trên bản đã vận hành.
            Data.RoleGroupSeeder.EnsureNewModulePermissions();

            // Gỡ các chức năng từng cấp nhầm cho nhóm Báo cáo công việc — bước trên chỉ biết cấp thêm.
            Data.RoleGroupSeeder.RevokeMisgrantedPermissions();

            // Bộ lịch nhắc báo cáo qua Telegram (sáng thứ Hai và chiều thứ Sáu).
            Infrastructure.ReminderScheduler.Start();

            // GoConnect: chỉ lắng nghe lệnh /hrm qua Telegram, KHÔNG hẹn giờ tự động.
            Infrastructure.GoConnectTelegramPoller.Start();
        }

        protected void Application_End()
        {
            Infrastructure.ReminderScheduler.Stop();
            Infrastructure.GoConnectTelegramPoller.Stop();
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
