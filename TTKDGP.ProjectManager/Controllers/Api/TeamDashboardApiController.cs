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
    /// API Bảng điều khiển Tổ cho Mobile — tương đương TeamDashboardController trên Web.
    /// Dành cho Quản lý Tổ (quyền wteam.view hoặc IsTeamManager/Admin) xem hôm nay ai đang làm gì,
    /// xếp hạng KPI tạm tính của từng người, và mức độ phân bổ dự án triển khai / hỗ trợ.
    /// </summary>
    [ApiAuthorize]
    public class TeamDashboardApiController : BaseController
    {
        private bool CanViewTeam
        {
            get
            {
                return Can(Permissions.Team.Perm(Permissions.View))
                    || IsTeamManager
                    || Can("*");
            }
        }

        [HttpGet]
        public ActionResult Index(int? year, int? month)
        {
            if (!CanViewTeam)
                return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ mới có quyền truy cập Bảng điều khiển Tổ.");

            var today = DateTime.Today;
            var y = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year.Value : today.Year;
            var m = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : today.Month;

            var users = WorkService.TrackedUsers();
            var allTasks = WorkService.AllTasks();
            var projects = Repository.WorkProjects.All();
            var byProject = projects.ToDictionary(p => p.Id, p => p);
            var taskById = allTasks.ToDictionary(t => t.Id, t => t);
            var todayLogs = Repository.WorkTimeLogs.All()
                .Where(l => l.WorkDate.Date == today)
                .ToList();

            var result = new TeamDashboardDto
            {
                Today = today,
                Year = y,
                Month = m,
                IsCurrentMonth = y == today.Year && m == today.Month,
                TotalMembers = users.Count
            };

            var memberRows = new List<TeamMemberRowDto>();

            foreach (var user in users)
            {
                var mine = allTasks.Where(t => t.AssigneeUserId == user.Id).ToList();
                var userTodayLogs = todayLogs.Where(l => l.UserId == user.Id).ToList();
                var todayTasks = BuildToday(mine, today, y, m, byProject);
                var workedTodayTasks = BuildWorkedToday(user.Id, mine, userTodayLogs, taskById, today, y, m, byProject);
                var todayLoggedHours = userTodayLogs.Sum(l => l.Hours);
                var kpi = BuildKpi(user, y, m);
                var implement = CountProjects(mine, y, m, TaskKinds.Checklist);
                var support = CountProjects(mine, y, m, TaskKinds.Support);

                var penalty = KpiService.SupportLatePenalty(kpi.SupportLateCount) + KpiService.ExecuteLatePenalty(kpi.ExecuteLateCount);
                var totalTasks = kpi.SupportTotal + kpi.ExecuteTotal + kpi.AssignedTotal;

                memberRows.Add(new TeamMemberRowDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    TodayTasks = todayTasks,
                    TodayTaskCount = todayTasks.Count,
                    OverdueTodayCount = todayTasks.Count(t => t.IsOverdue),
                    WorkedTodayTasks = workedTodayTasks,
                    TodayLoggedHours = todayLoggedHours,
                    Kpi = ApiMappers.ToDto(kpi),
                    TotalPenalty = penalty,
                    TotalTasks = totalTasks,
                    Implement = implement,
                    Support = support
                });
            }

            // Người chưa có việc hôm nay lên đầu
            result.Members = memberRows
                .OrderBy(r => r.TodayTaskCount > 0)
                .ThenBy(r => r.FullName, StringComparer.CurrentCulture)
                .ToList();

            result.IdleCount = result.Members.Count(x => x.TodayTaskCount == 0);
            result.OverdueTodayCount = result.Members.Sum(x => x.OverdueTodayCount);
            result.TotalTodayLoggedHours = result.Members.Sum(x => x.TodayLoggedHours);
            result.TotalWorkedTodayCount = result.Members.Sum(x => x.WorkedTodayTasks.Count);

            return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult MemberTasks(int userId, int year, int month, string kind)
        {
            if (!CanViewTeam)
                return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ mới có quyền truy cập.");

            var user = Repository.Users.Find(userId);
            if (user == null)
                return new HttpStatusCodeResult(404, "Không tìm thấy nhân sự.");

            var wanted = kind == TaskKinds.Support ? TaskKinds.Support : TaskKinds.Checklist;
            var kindLabel = wanted == TaskKinds.Support ? "Hỗ trợ" : "Triển khai";

            var tasks = WorkService.Sort(WorkService.AllTasks()
                .Where(t => t.AssigneeUserId == userId
                            && t.Kind == wanted
                            && t.State != TaskStates.Cancelled
                            && KpiService.TaskInMonth(t, year, month))).ToList();

            var taskIds = tasks.Select(t => t.Id).ToList();
            var loggedTotals = TimeLogService.TotalsByTask(taskIds);

            var items = tasks.Select(t =>
            {
                decimal logged = 0;
                loggedTotals.TryGetValue(t.Id, out logged);
                return new TeamMemberTaskItemDto
                {
                    Id = t.Id,
                    Code = t.Code,
                    Title = t.Title,
                    ProjectId = t.ProjectId,
                    ProjectName = t.ProjectName,
                    State = t.State,
                    Priority = t.Priority,
                    Progress = t.Progress,
                    StartDate = t.StartDate,
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    IsOverdue = t.IsOverdue,
                    LoggedHours = logged
                };
            }).ToList();

            var result = new TeamMemberTasksResultDto
            {
                UserId = user.Id,
                MemberName = user.FullName,
                Year = year,
                Month = month,
                Kind = wanted,
                KindLabel = kindLabel,
                TotalProjects = items.Select(t => t.ProjectId).Where(id => id > 0).Distinct().Count(),
                TotalTasks = items.Count,
                Tasks = items
            };

            return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
        }

        private static List<TeamTodayTaskDto> BuildToday(List<WorkTask> mine, DateTime today,
            int year, int month, Dictionary<int, WorkProject> byProject)
        {
            var result = new List<TeamTodayTaskDto>();

            foreach (var task in mine)
            {
                if (TaskStates.IsClosed(task.State)) continue;
                if (task.State == TaskStates.Paused) continue;
                if (!task.DueDate.HasValue) continue;
                if (!KpiService.TaskInMonth(task, year, month)) continue;

                var from = task.StartDate.HasValue ? task.StartDate.Value.Date : task.CreatedAt.Date;
                var to = task.DueDate.Value.Date;
                if (from > to) from = to;

                var covers = from <= today && (today <= to || task.IsOverdue);
                if (!covers) continue;

                WorkProject project;
                byProject.TryGetValue(task.ProjectId, out project);

                result.Add(new TeamTodayTaskDto
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    ProjectId = task.ProjectId,
                    ProjectName = project != null ? project.Name : task.ProjectName,
                    State = task.State,
                    Progress = task.Progress,
                    IsOverdue = task.IsOverdue
                });
            }

            return result
                .OrderByDescending(t => t.IsOverdue)
                .ThenBy(t => t.ProjectName, StringComparer.CurrentCulture)
                .ToList();
        }

        private static List<TeamWorkedTodayTaskDto> BuildWorkedToday(
            int userId,
            List<WorkTask> mine,
            List<WorkTimeLog> userTodayLogs,
            Dictionary<int, WorkTask> taskById,
            DateTime today,
            int year,
            int month,
            Dictionary<int, WorkProject> byProject)
        {
            var result = new List<TeamWorkedTodayTaskDto>();
            var processedTaskIds = new HashSet<int>();

            // Nhóm 1: Các công việc mà người này có ghi giờ (logtime) hôm nay
            foreach (var logGroup in userTodayLogs.GroupBy(l => l.TaskId))
            {
                var taskId = logGroup.Key;
                WorkTask task;
                if (!taskById.TryGetValue(taskId, out task))
                {
                    task = Repository.WorkTasks.Find(taskId);
                }
                if (task == null) continue;

                processedTaskIds.Add(task.Id);

                WorkProject project;
                byProject.TryGetValue(task.ProjectId, out project);

                var hoursToday = logGroup.Sum(l => l.Hours);
                var notes = string.Join("; ", logGroup
                    .Where(l => !string.IsNullOrWhiteSpace(l.Note))
                    .Select(l => l.Note.Trim())
                    .Distinct());

                var isCompletedToday = (task.CompletedAt.HasValue && task.CompletedAt.Value.Date == today)
                    || (task.State == TaskStates.Done && task.UpdatedAt.HasValue && task.UpdatedAt.Value.Date == today);

                result.Add(new TeamWorkedTodayTaskDto
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    ProjectId = task.ProjectId,
                    ProjectName = project != null ? project.Name : task.ProjectName,
                    State = task.State,
                    Progress = task.Progress,
                    IsOverdue = task.IsOverdue,
                    IsCompletedToday = isCompletedToday,
                    LoggedHoursToday = hoursToday,
                    TodayLogNote = notes
                });
            }

            // Nhóm 2: Các công việc được giao cho người này và đã Hoàn thành trong hôm nay (chưa có logtime ở trên)
            var doneToday = mine.Where(t => !processedTaskIds.Contains(t.Id) &&
                ((t.CompletedAt.HasValue && t.CompletedAt.Value.Date == today)
                 || (t.State == TaskStates.Done && t.UpdatedAt.HasValue && t.UpdatedAt.Value.Date == today)));

            foreach (var task in doneToday)
            {
                processedTaskIds.Add(task.Id);
                WorkProject project;
                byProject.TryGetValue(task.ProjectId, out project);

                result.Add(new TeamWorkedTodayTaskDto
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    ProjectId = task.ProjectId,
                    ProjectName = project != null ? project.Name : task.ProjectName,
                    State = task.State,
                    Progress = task.Progress,
                    IsOverdue = task.IsOverdue,
                    IsCompletedToday = true,
                    LoggedHoursToday = 0,
                    TodayLogNote = null
                });
            }

            // Nhóm 3: Các công việc Đang làm (InProgress) trong hạn hôm nay (chưa được thêm ở các nhóm trên)
            var inProgressToday = mine.Where(t => !processedTaskIds.Contains(t.Id) &&
                t.State == TaskStates.InProgress &&
                KpiService.TaskInMonth(t, year, month));

            foreach (var task in inProgressToday)
            {
                if (!task.DueDate.HasValue) continue;
                var from = task.StartDate.HasValue ? task.StartDate.Value.Date : task.CreatedAt.Date;
                var to = task.DueDate.Value.Date;
                if (from > to) from = to;

                var covers = from <= today && (today <= to || task.IsOverdue);
                if (!covers) continue;

                processedTaskIds.Add(task.Id);
                WorkProject project;
                byProject.TryGetValue(task.ProjectId, out project);

                result.Add(new TeamWorkedTodayTaskDto
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    ProjectId = task.ProjectId,
                    ProjectName = project != null ? project.Name : task.ProjectName,
                    State = task.State,
                    Progress = task.Progress,
                    IsOverdue = task.IsOverdue,
                    IsCompletedToday = false,
                    LoggedHoursToday = 0,
                    TodayLogNote = null
                });
            }

            return result
                .OrderByDescending(t => t.LoggedHoursToday > 0)
                .ThenByDescending(t => t.IsCompletedToday)
                .ThenByDescending(t => t.IsOverdue)
                .ThenBy(t => t.ProjectName, StringComparer.CurrentCulture)
                .ThenBy(t => t.Title, StringComparer.CurrentCulture)
                .ToList();
        }

        private static KpiMonth BuildKpi(User user, int year, int month)
        {
            var saved = Repository.KpiMonths.FirstOrDefault(
                k => k.Year == year && k.Month == month && k.UserId == user.Id);
            if (saved != null) return saved;

            var preview = new KpiMonth
            {
                Year = year,
                Month = month,
                UserId = user.Id,
                UserFullName = user.FullName
            };

            KpiService.Fill(preview, KpiService.TasksOfUserInMonth(user.Id, year, month));
            return preview;
        }

        private static TeamProjectCountDto CountProjects(List<WorkTask> mine, int year, int month, string kind)
        {
            var tasks = mine
                .Where(t => t.ProjectId > 0
                            && t.Kind == kind
                            && t.State != TaskStates.Cancelled
                            && KpiService.TaskInMonth(t, year, month))
                .ToList();

            return new TeamProjectCountDto
            {
                Projects = tasks.Select(t => t.ProjectId).Distinct().Count(),
                Tasks = tasks.Count
            };
        }
    }
}
