using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Models.Api;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>
    /// Doi tu model nghiep vu (WorkTask/WorkProject) sang DTO JSON — dung chung cho ca ba
    /// controller API (Dashboard/MyWork/MyProjects) de mot cho duy nhat quyet dinh cac truong
    /// nay tra ve nhu the nao, khong lap lai o tung noi.
    /// </summary>
    internal static class ApiMappers
    {
        /// <summary>
        /// Du an nguoi dung dang tham gia hoac lam PM — Y HET logic loc cua
        /// MyWorkController.Projects (PM hoac co WorkAssignment), viet lai o day vi khong co san
        /// mot service method dung chung.
        /// </summary>
        public static List<ProjectSummaryDto> MyProjects(int userId)
        {
            var projects = Repository.WorkProjects.All();
            var myAssignments = Repository.WorkAssignments.All().Where(a => a.UserId == userId).ToList();
            var stats = WorkService.StatsByProject(WorkService.AllTasks());
            var memberCounts = Repository.WorkAssignments.All()
                .Where(a => !a.LeftAt.HasValue)
                .GroupBy(a => a.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.UserId).Distinct().Count());

            var result = new List<ProjectSummaryDto>();
            foreach (var project in projects)
            {
                var isPm = project.PmUserId == userId;
                var isMember = myAssignments.Any(a => a.ProjectId == project.Id);
                if (!isPm && !isMember) continue;

                TaskStat stat;
                stats.TryGetValue(project.Id, out stat);
                int memberCount;
                memberCounts.TryGetValue(project.Id, out memberCount);

                result.Add(ToDto(project, stat, memberCount));
            }

            return result;
        }

        public static TaskDto ToDto(WorkTask task)
        {
            return new TaskDto
            {
                Id = task.Id,
                Code = task.Code,
                Title = task.Title,
                ProjectName = task.ProjectName,
                State = task.State,
                Priority = task.Priority,
                DueDate = task.DueDate,
                IsOverdue = task.IsOverdue,
                IsDueToday = task.DueDate.HasValue && task.DueDate.Value.Date == DateTime.Today
            };
        }

        public static ProjectSummaryDto ToDto(WorkProject project, TaskStat stat, int memberCount)
        {
            return new ProjectSummaryDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                ProgressPercent = stat != null ? stat.Percent : 0,
                MemberCount = memberCount
            };
        }

        /// <summary>
        /// Rut gon KpiMonth (day du cot cho bang chi tiet ben web) thanh vai con so chinh cho man
        /// dien thoai. Cong thuc ScorePercent/HoursPercent/HoursShort lay Y HET
        /// DashboardViewModel ben web (Controllers/DashboardController.cs) de hai noi khong bao
        /// gio le%ch nhau ve cung mot thang diem.
        /// </summary>
        public static KpiSummaryDto ToDto(KpiMonth kpi)
        {
            if (kpi == null) return null;

            var scoreMax = KpiService.MaxQualityPoint > 0 ? KpiService.MaxQualityPoint : 100;
            var scorePercent = (int)Math.Round(kpi.FinalPoint * 100 / scoreMax);
            scorePercent = scorePercent < 0 ? 0 : (scorePercent > 100 ? 100 : scorePercent);

            var hoursPercent = 100;
            if (kpi.RequiredHours > 0)
            {
                var p = (int)Math.Round(kpi.WorkedHours * 100 / kpi.RequiredHours);
                hoursPercent = p > 100 ? 100 : p;
            }

            var missing = kpi.RequiredHours - kpi.WorkedHours;

            return new KpiSummaryDto
            {
                FinalPoint = kpi.FinalPoint,
                Rank = kpi.Rank,
                ScoreMax = scoreMax,
                ScorePercent = scorePercent,
                WorkedHours = kpi.WorkedHours,
                RequiredHours = kpi.RequiredHours,
                HoursPercent = hoursPercent,
                HoursShort = missing > 0 ? Math.Round(missing, 1) : 0
            };
        }

        public static UpcomingLeaveDto ToDto(LeaveRequest leave)
        {
            return new UpcomingLeaveDto
            {
                FromDate = leave.FromDate,
                ToDate = leave.ToDate,
                Days = leave.Days,
                Kind = leave.Kind
            };
        }

        public static ProjectAttentionDto ToAttentionDto(WorkProject project, TaskStat stat, int overdueCount)
        {
            return new ProjectAttentionDto
            {
                Id = project.Id,
                Name = project.Name,
                Customer = project.Customer,
                PmName = project.PmName,
                ProgressPercent = stat != null ? stat.Percent : 0,
                OverdueCount = overdueCount
            };
        }
    }
}
