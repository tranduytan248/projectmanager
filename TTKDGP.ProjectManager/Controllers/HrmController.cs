using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Xem dữ liệu nhân sự đồng bộ từ GoConnect: nhân sự, đơn vị, chức danh. Chỉ để tra cứu —
    /// dữ liệu do lệnh /hrm trên Telegram kéo về, không sửa tay ở đây (sửa cũng bị ghi đè ở lần
    /// đồng bộ sau).
    ///
    /// Giới hạn ở Admin như màn hình GoConnect, vì đây là thông tin cá nhân của toàn bộ nhân sự
    /// (họ tên, email, số điện thoại) chứ không riêng tổ mình.
    /// </summary>
    [AppAuthorize(RequiredRole = Roles.Admin)]
    public class HrmController : BaseController
    {
        private const int PageSize = 25;

        public ActionResult Index()
        {
            return RedirectToAction("Employees");
        }

        public ActionResult Employees(int? page, string keyword, string wpId, string posId)
        {
            var model = HrEmployeeStore.Page(
                page ?? 1, PageSize, keyword, wpId, posId);

            // Danh sách lọc lấy trọn (129 đơn vị, 103 chức danh) — đủ nhỏ để đổ hết vào dropdown.
            ViewBag.WorkplaceOptions = HrWorkplaceStore.All();
            ViewBag.PositionOptions = HrPositionStore.All();
            ViewBag.Keyword = keyword;
            ViewBag.WpId = wpId;
            ViewBag.PosId = posId;

            return View(model);
        }

        public ActionResult Workplaces(int? page, string keyword)
        {
            ViewBag.Keyword = keyword;
            return View(HrWorkplaceStore.Page(page ?? 1, PageSize, keyword));
        }

        public ActionResult Positions(int? page, string keyword)
        {
            ViewBag.Keyword = keyword;
            return View(HrPositionStore.Page(page ?? 1, PageSize, keyword));
        }
    }
}
