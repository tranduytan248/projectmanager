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
    /// Tong quan cho man Home cua mobile — tuong duong DashboardController ben web (viec dang can
    /// lam, KPI thang, nghi phep, phan toan To cho Quan ly To), rut gon phan bieu do (doughnut/
    /// trend) vi man dien thoai khong ve Chart.js; thay bang cac con so tong hop.
    ///
    /// Khac voi web: khong co o chon thang — moi con so (KPI, nghi phep) luon la CUA THANG HIEN
    /// TAI, va danh sach "dang can lam" la MOI LUC (khong bo theo thang dang xem) — hop ly hon
    /// cho kieu dung "xem nhanh" tren dien thoai.
    /// </summary>
    [ApiAuthorize]
    public class DashboardApiController : BaseController
    {
        /// <summary>Trung voi DashboardController.DueSoonDays — cung mot dinh nghia "sap den han".</summary>
        private const int DueSoonDays = 7;

        private const int TaskListLimit = 20;

        /// <summary>Trung voi DashboardController.ListLimit — cung so dong cho danh sach nghi phep/du an.</summary>
        private const int ListLimit = 5;

        [HttpGet]
        public ActionResult Index()
        {
            var userId = CurrentUserId;
            var today = DateTime.Today;

            var mine = WorkService.TasksOfUser(userId);
            var open = mine.Where(t => !TaskStates.IsClosed(t.State)).ToList();
            var dueLimit = today.AddDays(DueSoonDays);

            // "Dang can lam": qua han hoac sap den han, viec tam dung khong tinh (dong ho da
            // dung) — y het DashboardController.BuildMyTasks, nhung khong bo theo thang dang
            // xem vi mobile khong co o chon thang.
            var focus = open
                .Where(t => !TaskStates.IsClockStopped(t.State))
                .Where(t => t.IsOverdue || (t.DueDate.HasValue && t.DueDate.Value.Date <= dueLimit))
                .OrderByDescending(t => t.IsOverdue)
                .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                .ToList();

            var tasks = focus.Take(TaskListLimit).Select(ApiMappers.ToDto).ToList();
            var todayCount = open.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date == today);
            var projects = ApiMappers.MyProjects(userId);

            var dto = new DashboardDto
            {
                ProjectCount = projects.Count,
                TodayTaskCount = todayCount,
                Projects = projects,
                Tasks = tasks,
                TaskStats = BuildTaskStats(mine, open, focus),
                Kpi = ApiMappers.ToDto(BuildMyKpi(userId, today)),
                Leave = BuildMyLeave(userId, today),
                CanSeeTeam = IsTeamManager,
                CanApproveLeave = Can(Permissions.Leaves.Perm("approve"))
            };

            if (dto.CanSeeTeam)
            {
                dto.Team = BuildTeam(userId);
            }

            if (dto.CanApproveLeave)
            {
                dto.PendingLeaveApprovalCount = LeaveService.PendingCount();
            }

            return Json(dto, JsonRequestBehavior.AllowGet);
        }

        /// <summary>Y het DashboardController.BuildMyTasks phan dem — chi khong bo theo thang.</summary>
        private static TaskStatsDto BuildTaskStats(List<WorkTask> mine, List<WorkTask> open, List<WorkTask> focus)
        {
            return new TaskStatsDto
            {
                OpenCount = open.Count,
                InProgressCount = open.Count(t => t.State == TaskStates.InProgress),
                NotStartedCount = open.Count(t => t.State == TaskStates.NotStarted),
                OverdueCount = open.Count(t => t.IsOverdue),
                DoneCount = mine.Count(t => t.State == TaskStates.Done),
                DueSoonCount = focus.Count(t => !t.IsOverdue)
            };
        }

        /// <summary>Y het DashboardController.BuildMyKpi — dung ban da chot trong bang neu co, chua
        /// co thi dung tam trong bo nho (KHONG luu, chi de xem).</summary>
        private static KpiMonth BuildMyKpi(int userId, DateTime today)
        {
            var saved = Repository.KpiMonths.FirstOrDefault(
                k => k.Year == today.Year && k.Month == today.Month && k.UserId == userId);
            if (saved != null) return saved;

            var preview = new KpiMonth
            {
                Year = today.Year,
                Month = today.Month,
                UserId = userId,
                UserFullName = WorkService.UserFullName(userId)
            };

            KpiService.Fill(preview, KpiService.TasksOfUserInMonth(userId, today.Year, today.Month));
            return preview;
        }

        /// <summary>Y het DashboardController.BuildMyLeave, co dinh vao thang hien tai.</summary>
        private static LeaveSummaryDto BuildMyLeave(int userId, DateTime today)
        {
            var result = new LeaveSummaryDto
            {
                ApprovedDays = LeaveService.ApprovedDays(userId, today.Year, today.Month)
            };

            var ofUser = LeaveService.OfUser(userId);
            result.PendingCount = ofUser.Count(l => l.State == LeaveStates.Pending);

            var from = new DateTime(today.Year, today.Month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            result.Upcoming = ofUser
                .Where(l => l.IsApproved && l.OverlapsRange(from, to))
                .OrderBy(l => l.FromDate)
                .Take(ListLimit)
                .Select(ApiMappers.ToDto)
                .ToList();

            return result;
        }

        /// <summary>Y het DashboardController.BuildTeam.</summary>
        private static TeamSummaryDto BuildTeam(int userId)
        {
            var allTasks = WorkService.AllTasks();
            var projects = Repository.WorkProjects.All();
            var stats = WorkService.StatsByProject(allTasks);
            var openProjects = projects.Where(p => ProjectStates.IsOpen(p.State)).ToList();

            var result = new TeamSummaryDto
            {
                OpenProjectCount = openProjects.Count,
                MyPmCount = openProjects.Count(p => p.PmUserId == userId),
                OverdueTaskCount = allTasks.Count(t => t.IsOverdue),
                OpenTaskCount = allTasks.Count(t => !TaskStates.IsClosed(t.State))
            };

            result.Projects = openProjects
                .Select(p =>
                {
                    TaskStat stat;
                    stats.TryGetValue(p.Id, out stat);
                    var overdueCount = allTasks.Count(t => t.ProjectId == p.Id && t.IsOverdue);
                    return new { Project = p, Stat = stat, OverdueCount = overdueCount };
                })
                .OrderByDescending(r => r.OverdueCount)
                .ThenBy(r => r.Stat != null ? r.Stat.Percent : 0)
                .ThenBy(r => r.Project.Name, StringComparer.CurrentCulture)
                .Take(ListLimit)
                .Select(r => ApiMappers.ToAttentionDto(r.Project, r.Stat, r.OverdueCount))
                .ToList();

            return result;
        }
    }
}
