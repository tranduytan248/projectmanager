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

        // Đơn vị hiện theo cây phân cấp (xem SortHierarchically) — chia trang nhỏ sẽ cắt ngang
        // cây giữa cha và con, khó đọc. Số đơn vị thực tế chỉ vài trăm nên gộp một trang cho trọn cây.
        private const int DepartmentPageSize = 500;

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

            Dictionary<int, int> depthById;
            var ordered = SortHierarchically(all, out depthById);

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? ordered
                : ordered.Where(d => (d.DepartmentName ?? string.Empty)
                    .IndexOf(keyword.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();

            ViewBag.Keyword = keyword;
            ViewBag.ParentNameById = nameById;
            ViewBag.EmployeeCountById = counts;
            ViewBag.DepthById = depthById;

            // KHÔNG sắp lại theo tên — filtered đã ở đúng thứ tự cây (cha trước con, anh em xếp
            // theo tên) từ SortHierarchically, PagedList.From chỉ cắt trang giữ nguyên thứ tự đó.
            return View(PagedList<DepartmentHrm>.From(filtered, page, DepartmentPageSize));
        }

        /// <summary>
        /// Xếp lại danh sách đơn vị theo thứ tự CÂY (duyệt sâu trước): đơn vị gốc rồi tới từng
        /// nhánh con, anh em cùng cấp xếp theo tên. <paramref name="depthById"/> trả về cấp của
        /// từng đơn vị (0 = gốc) để view thụt lề. Đơn vị chưa suy được cha (DepartmentParentId
        /// null nhưng không phải gốc) coi như một gốc riêng để không bị rơi mất khỏi danh sách.
        /// </summary>
        private static List<DepartmentHrm> SortHierarchically(List<DepartmentHrm> all, out Dictionary<int, int> depthById)
        {
            var byParent = all
                .GroupBy(d => d.DepartmentParentId ?? -1)
                .ToDictionary(g => g.Key, g => g.OrderBy(d => d.DepartmentName, StringComparer.CurrentCulture).ToList());

            var result = new List<DepartmentHrm>();
            var depth = new Dictionary<int, int>();
            var visited = new HashSet<int>();

            List<DepartmentHrm> roots;
            foreach (var root in byParent.TryGetValue(-1, out roots) ? roots : new List<DepartmentHrm>())
            {
                VisitDepartment(root, 0, byParent, result, depth, visited);
            }

            // An toàn: đơn vị nào chưa được thăm (vòng lặp cha-con hỏng dữ liệu) vẫn phải hiện ra.
            foreach (var d in all)
            {
                if (visited.Contains(d.DepartmentId)) continue;
                result.Add(d);
                depth[d.DepartmentId] = 0;
            }

            depthById = depth;
            return result;
        }

        private static void VisitDepartment(
            DepartmentHrm node, int level, Dictionary<int, List<DepartmentHrm>> byParent,
            List<DepartmentHrm> result, Dictionary<int, int> depth, HashSet<int> visited)
        {
            if (!visited.Add(node.DepartmentId)) return; // chặn vòng lặp vô hạn nếu dữ liệu cha-con hỏng

            result.Add(node);
            depth[node.DepartmentId] = level;

            List<DepartmentHrm> children;
            if (byParent.TryGetValue(node.DepartmentId, out children))
            {
                foreach (var child in children) VisitDepartment(child, level + 1, byParent, result, depth, visited);
            }
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
