using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Xem dữ liệu danh bạ lấy từ cổng CAS VNPT (id.vnpt.com.vn → hrm.vnpt.vn) qua lệnh "/signin"
    /// trên bot Telegram, xem <see cref="HrmCasAutoLogin"/>. Chỉ để tra cứu — dữ liệu bị GHI ĐÈ
    /// TOÀN BỘ ở mỗi lần đăng nhập lại (xem <see cref="HrmDirectorySync"/>), sửa tay ở đây vô nghĩa.
    ///
    /// HOÀN TOÀN KHÁC với <see cref="HrmController"/> (dữ liệu đồng bộ từ GoConnect). Hai nguồn
    /// độc lập, không dùng chung bảng.
    ///
    /// Giới hạn ở Admin (xem seed quyền "hrmdirectory" trong <see cref="Permissions"/>): đây là
    /// thông tin cá nhân của toàn bộ nhân sự công ty, không riêng tổ mình.
    /// </summary>
    [AppAuthorize(Permission = "hrmdirectory.view")]
    public class HrmDirectoryController : BaseController
    {
        private const int PageSize = 25;

        public ActionResult Index()
        {
            return RedirectToAction("Employees");
        }

        public ActionResult Employees(int? page, string keyword, int? departmentId, int? jobId)
        {
            ViewBag.DepartmentOptions = DepartmentHrmStore.All();
            ViewBag.Keyword = keyword;
            ViewBag.DepartmentId = departmentId;

            return View(EmployeeHrmStore.Page(page ?? 1, PageSize, keyword, departmentId, jobId));
        }

        public ActionResult Departments(int? page, string keyword)
        {
            var all = DepartmentHrmStore.All();
            var nameById = all.ToDictionary(d => d.DepartmentId, d => d.DepartmentName);
            var counts = EmployeeHrmStore.CountsByDepartment();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? all
                : all.Where(d => (d.DepartmentName ?? string.Empty)
                    .IndexOf(keyword.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();

            ViewBag.Keyword = keyword;
            ViewBag.ParentNameById = nameById;
            ViewBag.EmployeeCountById = counts;

            return View(PagedList<DepartmentHrm>.From(
                filtered.OrderBy(d => d.DepartmentName, StringComparer.CurrentCulture), page, PageSize));
        }

        public ActionResult Jobs(int? page, string keyword)
        {
            var all = JobHrmStore.All();
            var counts = EmployeeHrmStore.CountsByJob();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? all
                : all.Where(j => (j.JobName ?? string.Empty)
                        .IndexOf(keyword.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0
                    || (j.JobCode ?? string.Empty)
                        .IndexOf(keyword.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();

            ViewBag.Keyword = keyword;
            ViewBag.EmployeeCountById = counts;

            return View(PagedList<JobHrm>.From(
                filtered.OrderBy(j => j.JobName, StringComparer.CurrentCulture), page, PageSize));
        }
    }
}
