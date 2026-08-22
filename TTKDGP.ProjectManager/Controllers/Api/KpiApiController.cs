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
    /// API Chấm KPI theo tháng cho Mobile — tương đương KpiController trên Web.
    /// Cho phép xem bảng xếp hạng KPI toàn tổ, xem chi tiết 4 trụ cột và từng đầu việc của mỗi thành viên,
    /// và chốt/tính lại KPI theo tháng cho người có quyền kpi.generate.
    /// </summary>
    [ApiAuthorize]
    public class KpiApiController : BaseController
    {
        private bool CanViewKpi
        {
            get
            {
                return Can(Permissions.Kpi.Perm(Permissions.View))
                    || IsTeamManager
                    || Can("*");
            }
        }

        private bool CanGenerateKpi
        {
            get
            {
                return Can(Permissions.Kpi.Perm("generate"))
                    || IsTeamManager
                    || Can("*");
            }
        }

        private bool CanConfigKpi
        {
            get
            {
                return Can(Permissions.Kpi.Perm("config"))
                    || Can("*");
            }
        }

        /// <summary>
        /// GET /KpiApi/Index?year=&amp;month=&amp;userId=
        /// Trả về tổng quan và danh sách KPI toàn tổ trong tháng.
        /// </summary>
        [HttpGet]
        public ActionResult Index(int? year, int? month, int? userId)
        {
            if (!CanViewKpi)
                return new HttpStatusCodeResult(403, "Bạn không có quyền xem KPI theo tháng.");

            var today = DateTime.Today;
            var y = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year.Value : today.Year;
            var m = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : today.Month;
            var selectedUserId = userId ?? 0;

            var rows = BuildRows(y, m, selectedUserId > 0 ? (int?)selectedUserId : null);
            var standardDays = KpiService.StandardWorkingDays(y, m);
            var scaleMax = KpiService.MaxQualityPoint;
            if (scaleMax <= 0) scaleMax = 100;

            var activeUsers = WorkService.ActiveUsers();
            var userOptions = activeUsers.Select(u => new AssigneeOptionDto
            {
                UserId = u.Id,
                FullName = u.FullName
            }).ToList();

            var summary = BuildSummaryDto(rows, standardDays, scaleMax);

            var memberDtos = new List<KpiMemberRowDto>();
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var dto = MapToRowDto(r, standardDays, i + 1);
                memberDtos.Add(dto);
            }

            var result = new KpiIndexDto
            {
                Year = y,
                Month = m,
                IsCurrentMonth = y == today.Year && m == today.Month,
                SelectedUserId = selectedUserId,
                Summary = summary,
                Rows = memberDtos,
                Users = userOptions,
                CanGenerate = CanGenerateKpi,
                CanConfig = CanConfigKpi,
                StandardDays = standardDays,
                ScaleMax = scaleMax
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// GET /KpiApi/Detail?userId=&amp;year=&amp;month=
        /// Trả về chi tiết KPI của 1 nhân sự kèm danh sách công việc 4 trụ cột.
        /// </summary>
        [HttpGet]
        public ActionResult Detail(int userId, int? year, int? month)
        {
            if (!CanViewKpi && CurrentUserId != userId)
                return new HttpStatusCodeResult(403, "Bạn không có quyền xem chi tiết KPI này.");

            var user = Repository.Users.Find(userId);
            if (user == null) return HttpNotFound("Không tìm thấy nhân sự.");

            var today = DateTime.Today;
            var y = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year.Value : today.Year;
            var m = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : today.Month;

            var row = Repository.KpiMonths.FirstOrDefault(k => k.Year == y && k.Month == m && k.UserId == userId);
            var tasks = KpiService.TasksOfUserInMonth(userId, y, m);
            var isSaved = row != null;

            if (row == null)
            {
                row = new KpiMonth { Year = y, Month = m, UserId = userId, UserFullName = user.FullName };
                KpiService.Fill(row, tasks);
            }

            var standardDays = KpiService.StandardWorkingDays(y, m);
            var projects = Repository.WorkProjects.All().ToDictionary(p => p.Id, p => p.Name);
            var loggedHours = TimeLogService.TotalsByTask(tasks.Select(t => t.Id));

            Func<WorkTask, KpiTaskItemDto> mapTask = t =>
            {
                decimal logged = 0;
                loggedHours.TryGetValue(t.Id, out logged);

                string pName = "";
                projects.TryGetValue(t.ProjectId, out pName);

                var isLate = t.CompletedAt.HasValue && t.DueDate.HasValue && t.CompletedAt.Value.Date > t.DueDate.Value.Date;

                return new KpiTaskItemDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    ProjectId = t.ProjectId,
                    ProjectName = pName ?? "",
                    Kind = t.Kind,
                    State = t.State,
                    Progress = t.Progress,
                    StartDate = t.StartDate,
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    IsOverdue = t.IsOverdue,
                    LoggedHours = logged,
                    EstimatedHours = 0,
                    IsLate = isLate
                };
            };

            var supportTasks = tasks.Where(t => t.Kind == TaskKinds.Support)
                .OrderBy(t => t.DueDate)
                .Select(mapTask)
                .ToList();

            var executeTasks = tasks.Where(t => t.Kind == TaskKinds.Checklist || t.Kind == TaskKinds.Standalone)
                .OrderBy(t => t.DueDate)
                .Select(mapTask)
                .ToList();

            var assignedTasks = tasks.Where(t => t.Kind == TaskKinds.Standalone)
                .OrderBy(t => t.DueDate)
                .Select(mapTask)
                .ToList();

            var rowDto = MapToRowDto(row, standardDays, 0);
            rowDto.IsSaved = isSaved;

            var result = new KpiDetailDto
            {
                Row = rowDto,
                SupportTasks = supportTasks,
                ExecuteTasks = executeTasks,
                AssignedTasks = assignedTasks,
                CanGenerate = CanGenerateKpi,
                CanConfig = CanConfigKpi
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// POST /KpiApi/Calculate
        /// Tính &amp; chốt KPI tháng cho toàn bộ nhân sự.
        /// </summary>
        [HttpPost]
        public ActionResult Calculate(int year, int month)
        {
            if (!CanGenerateKpi)
                return new HttpStatusCodeResult(403, "Bạn không có quyền tính/chốt KPI.");

            var rows = KpiService.CalculateAll(year, month);
            return Json(new { ok = true, count = rows.Count, message = string.Format("Đã tính và chốt KPI tháng {0:00}/{1} cho {2} nhân sự.", month, year, rows.Count) });
        }

        /// <summary>
        /// POST /KpiApi/Recalculate
        /// Tính lại KPI của 1 nhân sự trong tháng.
        /// </summary>
        [HttpPost]
        public ActionResult Recalculate(int year, int month, int userId)
        {
            if (!CanGenerateKpi)
                return new HttpStatusCodeResult(403, "Bạn không có quyền tính lại KPI.");

            var user = Repository.Users.Find(userId);
            if (user == null) return HttpNotFound("Không tìm thấy nhân sự.");

            var row = KpiService.CalculateUser(year, month, user);
            var standardDays = KpiService.StandardWorkingDays(year, month);
            var dto = MapToRowDto(row, standardDays, 0);
            dto.IsSaved = true;

            return Json(new { ok = true, kpi = dto, message = string.Format("Đã tính lại KPI cho {0}.", user.FullName) });
        }

        // ---------- Helpers ----------

        private static List<KpiMonth> BuildRows(int year, int month, int? userId)
        {
            var rows = Repository.KpiMonths.All()
                .Where(k => k.Year == year && k.Month == month
                            && (!userId.HasValue || userId.Value <= 0 || k.UserId == userId.Value))
                .ToList();

            var savedUserIds = new HashSet<int>(rows.Select(r => r.UserId));

            foreach (var user in WorkService.TrackedUsers())
            {
                if (userId.HasValue && userId.Value > 0 && user.Id != userId.Value) continue;
                if (savedUserIds.Contains(user.Id)) continue;

                var preview = new KpiMonth
                {
                    Year = year,
                    Month = month,
                    UserId = user.Id,
                    UserFullName = user.FullName
                };
                KpiService.Fill(preview, KpiService.TasksOfUserInMonth(user.Id, year, month));
                rows.Add(preview);
            }

            return rows
                .OrderByDescending(k => k.FinalPoint)
                .ThenBy(k => k.UserFullName, StringComparer.CurrentCulture)
                .ToList();
        }

        private static KpiMonthSummaryDto BuildSummaryDto(List<KpiMonth> rows, int standardDays, decimal scaleMax)
        {
            var summary = new KpiMonthSummaryDto
            {
                Total = rows.Count,
                StandardDays = standardDays,
                ScaleMax = scaleMax
            };

            if (rows.Count == 0) return summary;

            summary.AveragePoint = Math.Round(rows.Average(r => r.FinalPoint), 1);
            summary.PassCount = rows.Count(r => r.Rank != KpiRanks.Fail);
            summary.PassPercent = summary.Total <= 0 ? 0 : (int)Math.Round((decimal)summary.PassCount * 100 / summary.Total);
            summary.FailCount = rows.Count - summary.PassCount;
            summary.ShortHoursCount = rows.Count(r => r.AttendanceRate < 100);

            var best = rows.OrderByDescending(r => r.FinalPoint).FirstOrDefault();
            if (best != null)
            {
                summary.TopName = best.UserFullName;
                summary.TopPoint = best.FinalPoint;
            }

            return summary;
        }

        private static KpiMemberRowDto MapToRowDto(KpiMonth r, int standardDays, int rankIndex)
        {
            var rSupportPenalty = KpiService.SupportLatePenalty(r.SupportLateCount);
            var rExecutePenalty = KpiService.ExecuteLatePenalty(r.ExecuteLateCount);
            var rTotalPenalty = rSupportPenalty + rExecutePenalty;
            var rGrossSupport = r.SupportPoint + rSupportPenalty;
            var rGrossExecute = r.ExecutePoint + rExecutePenalty;

            return new KpiMemberRowDto
            {
                Id = r.Id,
                UserId = r.UserId,
                FullName = r.UserFullName,
                Year = r.Year,
                Month = r.Month,
                SupportPoint = r.SupportPoint,
                SupportGrossPoint = rGrossSupport,
                SupportHours = r.SupportHours,
                SupportCapHours = r.SupportCapHours,
                ExecutePoint = r.ExecutePoint,
                ExecuteGrossPoint = rGrossExecute,
                ExecuteHours = r.ExecuteHours,
                ExecuteTargetHours = r.ExecuteTargetHours,
                AssignedPoint = r.AssignedPoint,
                LatePenalty = rTotalPenalty,
                QualityPoint = r.QualityPoint,
                StandardDays = r.StandardDays > 0 ? r.StandardDays : standardDays,
                LeaveDays = r.LeaveDays,
                RequiredHours = r.RequiredHours,
                WorkedHours = r.WorkedHours,
                AttendanceRate = (int)Math.Round(r.AttendanceRate),
                FinalPoint = r.FinalPoint,
                Rank = r.Rank,
                IsSaved = r.Id > 0,
                RankIndex = rankIndex
            };
        }
    }
}
