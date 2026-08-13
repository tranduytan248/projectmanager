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

            // Đọc số thập phân nhận cả dấu chấm lẫn dấu phẩy. Ứng dụng chạy culture vi-VN nhưng
            // ô <input type="number"> luôn gửi dấu chấm, nên thiếu bước này thì 0,25 lặng lẽ
            // thành 0 — xem DecimalModelBinder.
            DecimalModelBinder.Register(ModelBinders.Binders);

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

            // Cắm nguồn ngưỡng xếp loại KPI cho lớp Models (Models không gọi ngược lên Services).
            Models.KpiRanks.ConfigProvider = () => Services.KpiConfigService.Current;

            // Cắm nguồn trạng thái bật/tắt chức năng — màn "Chức năng" tắt mã nào thì mã đó
            // không ai dùng được, kể cả tài khoản toàn quyền.
            Models.Permissions.EnabledProvider = Services.PermissionSettingService.IsEnabled;

            // Bộ lịch nhắc báo cáo qua Telegram (sáng thứ Hai và chiều thứ Sáu).
            Infrastructure.ReminderScheduler.Start();

            // GoConnect: chỉ lắng nghe lệnh /hrm qua Telegram, KHÔNG hẹn giờ tự động.
            Infrastructure.GoConnectTelegramPoller.Start();

            // Hrm: đăng nhập CAS VNPT (id.vnpt.com.vn), lắng nghe lệnh /signin qua Telegram.
            // Hệ thống hoàn toàn khác GoConnect ở trên, có bot và cấu hình riêng.
            Infrastructure.HrmTelegramPoller.Start();
        }

        protected void Application_End()
        {
            Infrastructure.ReminderScheduler.Stop();
            Infrastructure.GoConnectTelegramPoller.Stop();
            Infrastructure.HrmTelegramPoller.Stop();
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

        /// <summary>
        /// Ghi lại mọi lỗi chưa bắt được. customErrors đang bật nên người dùng chỉ thấy trang chủ,
        /// còn form gửi bằng AJAX thì chỉ thấy "Không lưu được" — không có bước này thì nguyên nhân
        /// thật sự không để lại dấu vết nào. Xem App_Data\errors.log.
        /// </summary>
        protected void Application_Error(object sender, EventArgs e)
        {
            ErrorLog.Write(Server.GetLastError(), Context);
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
