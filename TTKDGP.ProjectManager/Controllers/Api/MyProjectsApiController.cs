using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Models.Api;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>
    /// Du an nguoi dung dang tham gia hoac lam PM — man "Du an" cua mobile, tuong duong
    /// MyWorkController.Projects ben web nhung bo phan tim kiem/loc nang (phase/state/role,
    /// PagedList) — mobile chi can showClosed + tim theo chu (q), tra het mot lan.
    /// </summary>
    [ApiAuthorize]
    public class MyProjectsApiController : BaseController
    {
        /// <summary>
        /// scope="team": liet ke TOAN BO du an cua Ca To (dung cho the "Du an dang chay" tren
        /// Dashboard) thay vi chi du an cua rieng minh — CHI hieu luc khi IsTeamManager, khong
        /// co quyen thi am tham coi nhu "mine" (giong nguyen tac o MyWorkApiController.Index).
        /// </summary>
        [HttpGet]
        public ActionResult Index(string q = null, bool showClosed = false, string scope = "mine")
        {
            var userId = CurrentUserId;
            var year = WeekHelper.CurrentYear;
            var week = WeekHelper.CurrentWeek;
            var teamScope = scope == "team" && IsTeamManager;

            var projects = Repository.WorkProjects.All();
            var myAssignments = Repository.WorkAssignments.All().Where(a => a.UserId == userId).ToList();
            var tasks = WorkService.AllTasks();
            var stats = WorkService.StatsByProject(tasks);
            var reports = Repository.WorkWeekReports.All()
                .Where(r => r.Year == year && r.Week == week).ToList();

            var rows = new List<MyProjectRowDto>();
            int pmCount = 0, closedCount = 0;

            foreach (var project in projects)
            {
                var isPm = project.PmUserId == userId;
                var terms = myAssignments.Where(a => a.ProjectId == project.Id).ToList();
                if (!teamScope && !isPm && terms.Count == 0) continue;

                if (isPm) pmCount++;
                if (!project.IsOpen) closedCount++;

                TaskStat stat;
                stats.TryGetValue(project.Id, out stat);

                var open = terms.FirstOrDefault(t => t.IsActive);
                var report = reports.FirstOrDefault(r => r.ProjectId == project.Id);

                rows.Add(new MyProjectRowDto
                {
                    Id = project.Id,
                    Code = project.Code,
                    Name = project.Name,
                    Customer = project.Customer,
                    PmName = project.PmName,
                    Phase = project.Phase,
                    State = project.State,
                    IsOpen = project.IsOpen,
                    IsPm = isPm,
                    IsActiveMember = open != null,
                    Role = open != null ? open.Role : terms.Select(t => t.Role).FirstOrDefault(),
                    JoinedAt = terms.Count > 0 ? terms.Min(t => t.JoinedAt) : (DateTime?)null,
                    LeftAt = open != null || terms.Count == 0 ? null : terms.Max(t => t.LeftAt),
                    ChecklistDone = stat != null ? stat.Done : 0,
                    ChecklistTotal = stat != null ? stat.Total : 0,
                    ChecklistPercent = stat != null ? stat.Percent : 0,
                    MyOpenCount = tasks.Count(t => t.ProjectId == project.Id
                        && t.AssigneeUserId == userId && !TaskStates.IsClosed(t.State)),
                    MyOverdueCount = tasks.Count(t => t.ProjectId == project.Id
                        && t.AssigneeUserId == userId && t.IsOverdue),
                    ReportStatus = ReportStatusOf(report, isPm)
                });
            }

            // Y het thu tu ben web: PM truoc, roi thanh vien dang hoat dong, roi theo ten.
            rows = rows
                .OrderByDescending(r => r.IsPm)
                .ThenByDescending(r => r.IsActiveMember)
                .ThenBy(r => r.Name, StringComparer.CurrentCulture)
                .ToList();

            var items = rows.AsEnumerable();

            // Mac dinh giau du an da dong, y het web — tranh danh sach dai dan theo nam thang.
            if (!showClosed) items = items.Where(r => r.IsOpen);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                items = items.Where(r =>
                    (r.Name ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || (r.Code ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || (r.Customer ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || (r.PmName ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0);
            }

            var dto = new MyProjectsDto
            {
                TotalCount = rows.Count,
                PmCount = pmCount,
                ClosedCount = closedCount,
                Projects = items.ToList()
            };

            return Json(dto, JsonRequestBehavior.AllowGet);
        }

        /// <summary>Y het logic hien badge o Views/MyWork/Projects.cshtml.</summary>
        private static string ReportStatusOf(WorkWeekReport report, bool isPm)
        {
            if (report != null && report.IsSubmitted) return report.IsOnTime ? "DaNopDungHan" : "DaNopTreHan";
            if (report != null) return "ConBanNhap";
            return isPm ? "ChuaNop" : "KhongApDung";
        }

        /// <summary>
        /// Chi tiet mot du an — tuong duong WorkProjectsController.Details ben web, cung mot phep
        /// kiem quyen CanViewProject (thanh vien/PM deu xem duoc, khong can wprojects.view chi
        /// Quan ly To moi co). CO CHU DICH BO "Thong tin trien khai" (Github/SVN/FTP/DB) va tai
        /// lieu dinh kem — xem ghi chu tren ProjectDetailDto.
        /// </summary>
        [HttpGet]
        public ActionResult Detail(int id)
        {
            var project = Repository.WorkProjects.Find(id);
            if (project == null || !CanViewProject(id)) return HttpNotFound();

            var userId = CurrentUserId;
            var year = WeekHelper.CurrentYear;
            var week = WeekHelper.CurrentWeek;

            var tasks = WorkService.TasksOfProject(id);
            var checklist = tasks.Where(t => t.Kind == TaskKinds.Checklist).ToList();
            var support = tasks.Where(t => t.Kind == TaskKinds.Support).ToList();
            var checklistStat = new TaskStat
            {
                Total = checklist.Count,
                Done = checklist.Count(t => t.State == TaskStates.Done),
                Overdue = checklist.Count(t => t.IsOverdue)
            };

            var assignments = WorkService.AssignmentsOfProject(id);

            var overdueTasks = tasks
                .Where(t => t.IsOverdue)
                .OrderBy(t => t.DueDate)
                .Take(5)
                .Select(ApiMappers.ToDto)
                .ToList();

            // Y het WorkProjectsController.Details: dem theo dung cot Year/Week co san tren
            // WorkTask, khong tu suy dien tu CreatedAt.
            var supportThisWeek = support.Count(t => t.Year == year && t.Week == week);

            var recentReports = Repository.WorkWeekReports.All()
                .Where(r => r.ProjectId == id)
                .OrderByDescending(r => r.Year).ThenByDescending(r => r.Week)
                .Take(5)
                .Select(ApiMappers.ToDto)
                .ToList();

            var dto = new ProjectDetailDto
            {
                Id = project.Id,
                Code = project.Code,
                Name = project.Name,
                Customer = project.Customer,
                ProjectType = project.ProjectType,
                Phase = project.Phase,
                State = project.State,
                IsOpen = project.IsOpen,
                PmName = project.PmName,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Description = HtmlSanitizer.ToPlainText(project.Description),
                CanEdit = CanEditProject(id),
                ActiveMemberCount = assignments.Count(a => a.IsActive),
                PastMemberCount = assignments.Count(a => !a.IsActive),
                ChecklistDone = checklistStat.Done,
                ChecklistTotal = checklistStat.Total,
                ChecklistOverdue = checklistStat.Overdue,
                SupportThisWeek = supportThisWeek,
                Members = assignments.Select(ApiMappers.ToDto).ToList(),
                OverdueTasks = overdueTasks,
                CurrentReport = ApiMappers.ToDto(WorkService.FindReport(id, year, week), year, week),
                RecentReports = recentReports
            };

            return Json(dto, JsonRequestBehavior.AllowGet);
        }
    }
}
