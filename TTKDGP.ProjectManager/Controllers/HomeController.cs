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

                MemberLoads = BuildMemberLoads(FilterForWorkload(rows)),
                ByMember = GroupByMember(rows)
            };

            FillCurrentWeek(model);
            return View(model);
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

        /// <summary>
        /// Gom các dòng tham gia theo từng thành viên, sắp theo độ ưu tiên của trạng thái dự án
        /// (Đang thực hiện lên đầu). Trong mỗi khối, các dự án ở mức ưu tiên cao nhất được hiện
        /// ngay, phần còn lại gập sau nút "Xem thêm".
        /// </summary>
        private static List<MemberParticipation> GroupByMember(IEnumerable<SummaryRow> rows)
        {
            var groups = rows
                .GroupBy(r => new { r.MemberId, r.MemberName })
                .Select(g =>
                {
                    var sorted = g
                        .OrderBy(r => r.ProjectStatusPriority)
                        .ThenByDescending(r => r.IsActive)
                        .ThenBy(r => r.ProjectName, StringComparer.CurrentCulture)
                        .ToList();

                    // Mức ưu tiên cao nhất mà người này đang có — thường là "Đang thực hiện".
                    var topPriority = sorted.Count == 0 ? 0 : sorted[0].ProjectStatusPriority;

                    return new MemberParticipation
                    {
                        MemberId = g.Key.MemberId,
                        MemberName = g.Key.MemberName,
                        Rows = sorted,
                        VisibleRows = sorted.Where(r => r.ProjectStatusPriority == topPriority).ToList(),
                        HiddenRows = sorted.Where(r => r.ProjectStatusPriority != topPriority).ToList()
                    };
                })
                .ToList();

            // Người có dự án ưu tiên cao nhất xếp trước; cùng mức thì ai nhiều dự án hơn lên trước.
            return groups
                .OrderBy(m => m.Rows.Count == 0 ? int.MaxValue : m.Rows[0].ProjectStatusPriority)
                .ThenByDescending(m => m.ActiveCount)
                .ThenBy(m => m.MemberName, StringComparer.CurrentCulture)
                .ToList();
        }

        /// <summary>
        /// Điền thông tin tuần đang diễn ra và tình hình báo cáo của tuần đó.
        /// Số liệu này tính trên toàn bộ dữ liệu, không phụ thuộc bộ lọc đang chọn,
        /// để luôn phản ánh đúng tình hình chung.
        /// </summary>
        private static void FillCurrentWeek(SummaryViewModel model)
        {
            var year = WeekHelper.CurrentYear;
            var week = WeekHelper.CurrentWeek;

            model.CurrentYear = year;
            model.CurrentWeek = week;
            model.CurrentWeekFrom = WeekHelper.FirstDayOfWeek(year, week);
            model.CurrentWeekTo = WeekHelper.LastDayOfWeek(year, week);

            var report = Services.ReminderService.Build(Models.ReminderKind.FridayCurrentWeek, DateTime.Today);
            model.MissingThisWeek = report.MissingProjectCount;
            model.ReportedThisWeek = report.ReportedProjects;
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
