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
    /// FrontEnd công khai: màn hình tổng hợp thành viên đang tham gia các dự án và trạng thái tham gia.
    /// Không yêu cầu đăng nhập.
    /// </summary>
    public class HomeController : BaseController
    {
        public ActionResult Index(string keyword, int? memberId, int? projectId, int? pmId, string workStatus, bool? activeOnly)
        {
            var showActiveOnly = activeOnly ?? true;
            var rows = Repository.BuildSummaryRows();

            if (showActiveOnly)
            {
                rows = rows.Where(r => r.IsActive).ToList();
            }

            if (memberId.HasValue && memberId.Value > 0)
            {
                rows = rows.Where(r => r.MemberId == memberId.Value).ToList();
            }

            if (projectId.HasValue && projectId.Value > 0)
            {
                rows = rows.Where(r => r.ProjectId == projectId.Value).ToList();
            }

            if (pmId.HasValue && pmId.Value > 0)
            {
                rows = rows.Where(r => r.PmMemberId == pmId.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(workStatus))
            {
                rows = rows.Where(r => string.Equals(r.WorkStatus, workStatus, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                rows = rows.Where(r =>
                    Contains(r.MemberName, k) ||
                    Contains(r.ProjectName, k) ||
                    Contains(r.Customer, k) ||
                    Contains(r.Role, k) ||
                    Contains(r.WorkContent, k)).ToList();
            }

            rows = rows
                .OrderBy(r => r.MemberName, StringComparer.CurrentCulture)
                .ThenBy(r => r.ProjectName, StringComparer.CurrentCulture)
                .ToList();

            var model = new SummaryViewModel
            {
                Rows = rows,
                Keyword = keyword,
                MemberId = memberId,
                ProjectId = projectId,
                PmId = pmId,
                WorkStatus = workStatus,
                ActiveOnly = showActiveOnly,

                Members = Repository.Members.All()
                    .OrderBy(m => m.FullName, StringComparer.CurrentCulture).ToList(),
                Projects = Repository.Projects.All()
                    .OrderBy(p => p.Name, StringComparer.CurrentCulture).ToList(),
                Pms = BuildPmList(),
                WorkStatuses = Repository.FilterableWorkStatuses(),

                TotalAssignments = rows.Count,
                DistinctMembers = rows.Select(r => r.MemberId).Distinct().Count(),
                DistinctProjects = rows.Select(r => r.ProjectId).Distinct().Count(),
                ActiveAssignments = rows.Count(r => r.IsActive),

                // Phân bố đếm theo dự án riêng biệt, gom theo trạng thái của chính dự án.
                StatusBreakdown = rows
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.ProjectStatus) ? "(chưa đặt)" : r.ProjectStatus)
                    .Select(g => new StatusCount
                    {
                        Status = g.Key,
                        Count = g.Select(r => r.ProjectId).Distinct().Count()
                    })
                    .OrderByDescending(s => s.Count)
                    .ToList(),

                MemberLoads = BuildMemberLoads(FilterForWorkload(rows))
            };

            FillCurrentWeek(model);
            FillMonthTasks(model);
            return View(model);
        }

        /// <summary>
        /// Chính sách quyền riêng tư của ứng dụng BrewTask (yêu cầu bắt buộc của Google Play Console).
        /// Công khai, không yêu cầu đăng nhập.
        /// </summary>
        [AllowAnonymous]
        public ActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Trang tài liệu tích hợp API — công khai như trang tổng hợp, ai cần tích hợp
        /// cũng xem được mà không phải đăng nhập.
        /// </summary>
        [AllowAnonymous]
        public ActionResult Docs()
        {
            return View();
        }

        /// <summary>
        /// Tải tài liệu tích hợp API (hướng dẫn và bộ Postman). Công khai như trang tổng hợp;
        /// tài liệu không chứa khoá bí mật (khoá cấp riêng cho từng hệ thống). Chỉ nhận đúng vài
        /// tên tài liệu định sẵn để không cho đọc file tuỳ ý.
        /// </summary>
        [AllowAnonymous]
        public ActionResult Download(string doc)
        {
            string fileName, contentType;
            switch ((doc ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "guide":
                    fileName = "Huong-dan-tich-hop-API-HRM.md";
                    contentType = "text/markdown; charset=utf-8";
                    break;
                case "postman":
                    fileName = "HRM-API.postman_collection.json";
                    contentType = "application/json; charset=utf-8";
                    break;
                case "sms-guide":
                    fileName = "Huong-dan-tich-hop-API-SMS.md";
                    contentType = "text/markdown; charset=utf-8";
                    break;
                case "sms-postman":
                    fileName = "SMS-API.postman_collection.json";
                    contentType = "application/json; charset=utf-8";
                    break;
                case "cas-guide":
                    fileName = "Huong-dan-tich-hop-API-CAS-HRM.md";
                    contentType = "text/markdown; charset=utf-8";
                    break;
                case "cas-postman":
                    fileName = "CAS-HRM-API.postman_collection.json";
                    contentType = "application/json; charset=utf-8";
                    break;
                default:
                    return HttpNotFound();
            }

            var path = Server.MapPath("~/App_Data/docs/" + fileName);
            if (!System.IO.File.Exists(path)) return HttpNotFound();

            return File(System.IO.File.ReadAllBytes(path), contentType, fileName);
        }

        /// <summary>Chi tiết một dự án kèm danh sách thành viên — công khai, chỉ xem.</summary>
        public ActionResult Project(int id)
        {
            var project = Repository.Projects.Find(id);
            if (project == null)
            {
                Response.StatusCode = 404;
                ViewData["Title"] = "Không tìm thấy dự án";
                ViewData["Message"] = "Dự án bạn tìm không tồn tại hoặc đã bị xoá.";
                return View("Error");
            }

            ViewBag.PmName = Repository.MemberName(project.PmMemberId);

            var model = new ProjectDetailViewModel
            {
                Project = project,
                Members = Repository.BuildSummaryRows()
                    .Where(r => r.ProjectId == id)
                    .OrderByDescending(r => r.IsActive)
                    .ThenBy(r => r.MemberName, StringComparer.CurrentCulture)
                    .ToList()
            };

            return View(model);
        }

        /// <summary>Điền thông tin tuần đang diễn ra.</summary>
        private static void FillCurrentWeek(SummaryViewModel model)
        {
            var year = WeekHelper.CurrentYear;
            var week = WeekHelper.CurrentWeek;

            model.CurrentYear = year;
            model.CurrentWeek = week;
            model.CurrentWeekFrom = WeekHelper.FirstDayOfWeek(year, week);
            model.CurrentWeekTo = WeekHelper.LastDayOfWeek(year, week);
        }

        /// <summary>
        /// Tổng hợp công việc của nhân sự trong tháng hiện tại, đếm theo trạng thái — dữ liệu từ
        /// bộ quản lý công việc mới (WorkTasks). Số liệu toàn cục, không phụ thuộc bộ lọc đang chọn.
        /// </summary>
        private static void FillMonthTasks(SummaryViewModel model)
        {
            var today = DateTime.Today;
            var from = new DateTime(today.Year, today.Month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            model.TaskMonth = today.Month;
            model.TaskMonthYear = today.Year;

            List<WorkTask> tasks;
            try
            {
                tasks = Repository.WorkTasks.All()
                    .Where(t => TaskInRange(t, from, to))
                    .ToList();
            }
            catch (Exception)
            {
                // SQL chập chờn: trang tổng hợp vẫn phải mở được, chỉ thiếu bảng công việc.
                return;
            }

            model.MonthTaskCount = tasks.Count;
            model.MonthTaskProjectCount = tasks
                .Where(t => t.ProjectId > 0)
                .Select(t => t.ProjectId)
                .Distinct()
                .Count();

            model.MonthTasks = tasks
                .GroupBy(t => t.AssigneeUserId)
                .Select(g =>
                {
                    var name = g.Select(t => t.AssigneeName)
                        .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                    var row = new MemberTaskSummary
                    {
                        UserId = g.Key,
                        UserName = g.Key <= 0 ? "(Chưa giao)" : (name ?? "(Không rõ)"),
                        Overdue = g.Count(t => t.IsOverdue),
                        Total = g.Count()
                    };
                    foreach (var byState in g.GroupBy(t => t.State))
                    {
                        row.ByState[byState.Key ?? string.Empty] = byState.Count();
                    }
                    return row;
                })
                // Người chưa giao xếp cuối; còn lại ai nhiều việc hơn lên trước.
                .OrderBy(r => r.UserId <= 0 ? 1 : 0)
                .ThenByDescending(r => r.Total)
                .ThenBy(r => r.UserName, StringComparer.CurrentCulture)
                .ToList();

            var total = new MemberTaskSummary
            {
                UserName = "Tổng cộng",
                Overdue = model.MonthTasks.Sum(r => r.Overdue),
                Total = model.MonthTasks.Sum(r => r.Total)
            };
            foreach (var state in TaskStates.All)
            {
                total.ByState[state] = model.MonthTasks.Sum(r => r.Count(state));
            }
            model.MonthTaskTotal = total;
        }

        /// <summary>
        /// Đầu việc có thuộc khoảng ngày này không. Ưu tiên hạn hoàn thành (cùng quy tắc với bộ
        /// chấm KPI: việc đến hạn trong kỳ); việc hỗ trợ không có hạn thì tính theo tuần được phân;
        /// còn lại dựa vào mốc hoàn thành.
        /// </summary>
        private static bool TaskInRange(WorkTask task, DateTime from, DateTime to)
        {
            if (task.DueDate.HasValue)
            {
                var d = task.DueDate.Value.Date;
                return d >= from && d <= to;
            }

            if (task.Year > 0 && task.Week > 0)
            {
                return WeekHelper.FirstDayOfWeek(task.Year, task.Week) <= to
                    && WeekHelper.LastDayOfWeek(task.Year, task.Week) >= from;
            }

            return task.CompletedAt.HasValue
                && task.CompletedAt.Value.Date >= from
                && task.CompletedAt.Value.Date <= to;
        }

        /// <summary>
        /// Lọc các dòng dùng để tính khối lượng thành viên: chỉ công việc đang thực hiện,
        /// và chỉ trên những dự án đang chạy thật (đang thực hiện hoặc đang hỗ trợ).
        /// Danh sách trạng thái lấy từ Web.config để đổi được mà không phải build lại.
        /// </summary>
        private static List<SummaryRow> FilterForWorkload(IEnumerable<SummaryRow> rows)
        {
            var workStatuses = new HashSet<string>(
                AppSettings.Dashboard.WorkloadWorkStatuses, StringComparer.CurrentCultureIgnoreCase);
            var projectStatuses = new HashSet<string>(
                AppSettings.Dashboard.WorkloadProjectStatuses, StringComparer.CurrentCultureIgnoreCase);

            return rows
                .Where(r => r.WorkStatus != null && workStatuses.Contains(r.WorkStatus))
                .Where(r => r.ProjectStatus != null && projectStatuses.Contains(r.ProjectStatus))
                .ToList();
        }

        private static List<MemberLoad> BuildMemberLoads(IEnumerable<SummaryRow> rows)
        {
            return rows
                .GroupBy(r => new { r.MemberId, r.MemberName })
                .Select(g => new MemberLoad
                {
                    MemberId = g.Key.MemberId,
                    MemberName = g.Key.MemberName,
                    ProjectCount = g.Select(r => r.ProjectId).Distinct().Count(),
                    ActiveProjectCount = g.Where(r => r.IsActive).Select(r => r.ProjectId).Distinct().Count()
                })
                .OrderByDescending(m => m.ProjectCount)
                .ThenBy(m => m.MemberName, StringComparer.CurrentCulture)
                .ToList();
        }

        /// <summary>
        /// Danh sách nhân sự đang làm PM của ít nhất một dự án, để đổ vào dropdown lọc PM.
        /// Lấy từ chính các dự án (không phải từ dòng đã lọc) để không sót PM của dự án
        /// chưa có phân công nào.
        /// </summary>
        private static List<Member> BuildPmList()
        {
            var pmIds = new HashSet<int>(
                Repository.Projects.All()
                    .Select(p => p.PmMemberId)
                    .Where(id => id > 0));

            return Repository.Members.All()
                .Where(m => pmIds.Contains(m.Id))
                .OrderBy(m => m.FullName, StringComparer.CurrentCulture)
                .ToList();
        }

        private static bool Contains(string source, string keyword)
        {
            return !string.IsNullOrEmpty(source)
                   && source.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
