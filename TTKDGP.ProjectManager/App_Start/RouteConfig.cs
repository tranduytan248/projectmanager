using System.Web.Mvc;
using System.Web.Routing;

namespace TTKDGP.ProjectManager
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // API tích hợp cho đối tác lấy dữ liệu HRM. Đặt trước route mặc định để URL gọn (/api/hrm).
            routes.MapRoute(
                name: "IntegrationApiHrm",
                url: "api/hrm",
                defaults: new { controller = "IntegrationApi", action = "Hrm" }
            );

            // API tiện ích cho đối tác (gửi SMS). Giữ đúng đường dẫn quen thuộc của HRM.
            routes.MapRoute(
                name: "UtilApiSendSms",
                url: "api/Util/SendSms",
                defaults: new { controller = "UtilApi", action = "SendSms" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
