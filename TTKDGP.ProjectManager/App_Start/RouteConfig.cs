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

            // API tích hợp lấy dữ liệu danh bạ CAS VNPT (HRM) — nguồn hoàn toàn khác api/hrm ở
            // trên (đó là dữ liệu đồng bộ từ GoConnect). Dùng chung cơ chế checksum + hệ thống
            // tích hợp, chỉ khác resource và tầng dữ liệu bên dưới.
            routes.MapRoute(
                name: "IntegrationApiHrmCas",
                url: "api/hrmcas",
                defaults: new { controller = "IntegrationApi", action = "HrmCas" }
            );

            // API tiện ích cho đối tác (gửi SMS). Giữ đúng đường dẫn quen thuộc của HRM.
            routes.MapRoute(
                name: "UtilApiSendSms",
                url: "api/Util/SendSms",
                defaults: new { controller = "UtilApi", action = "SendSms" }
            );

            // API KPI tháng, dùng chung phiên đăng nhập của web. Hai route hành động phải đứng
            // TRƯỚC route {userId}, nếu không "calculate" bị nuốt làm userId.
            routes.MapRoute(
                name: "KpiApiCalculate",
                url: "api/kpi/calculate",
                defaults: new { controller = "Kpi", action = "ApiCalculate" }
            );

            routes.MapRoute(
                name: "KpiApiRecalculate",
                url: "api/kpi/recalculate",
                defaults: new { controller = "Kpi", action = "ApiRecalculate" }
            );

            routes.MapRoute(
                name: "KpiApiDetail",
                url: "api/kpi/{userId}",
                defaults: new { controller = "Kpi", action = "ApiDetail" },
                constraints: new { userId = @"\d+" }
            );

            routes.MapRoute(
                name: "KpiApiList",
                url: "api/kpi",
                defaults: new { controller = "Kpi", action = "ApiList" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
