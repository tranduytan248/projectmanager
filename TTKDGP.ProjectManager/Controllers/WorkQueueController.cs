using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Dashboard theo vai trò: "Việc cần xử lý" cho Quản lý và "Tình trạng hệ thống" cho Quản trị.
    /// Chỉ tổng hợp và điều hướng — mọi số liệu lấy từ dữ liệu thật, không bịa.
    /// </summary>
    [AppAuthorize]
    public class WorkQueueController : BaseController
    {
        /// <summary>Số tuần không có báo cáo thì coi là "dự án im lặng" cần chú ý.</summary>
        private const int SilentWeeks = 3;

        [AppAuthorize(Permission = "teamreports.view")]
        public ActionResult Index()
        {
            var today = DateTime.Today;
            var year = WeekHelper.CurrentYear;
            var week = WeekHelper.CurrentWeek;

            var report = ReminderService.Build(ReminderKind.FridayCurrentWeek, today);
            var rows = Repository.BuildSummaryRows();

            var model = new ManagerDashboardViewModel
            {
                Year = year,
                Week = week,
                WeekLabel = WeekHelper.LabelWithYear(year, week),
                WeekFrom = WeekHelper.FirstDayOfWeek(year, week),
                WeekTo = WeekHelper.LastDayOfWeek(year, week),

                MissingMembers = ReminderService.BuildMemberReminders(today),

                ReportedProjects = report.ReportedProjects,
                MissingProjects = report.MissingProjectCount,
                TotalOpenProjects = report.TotalOpenProjects,

                SilentProjects = BuildSilentProjects(rows, year, week),

                OpenProjectCount = report.TotalOpenProjects,
                ActiveMemberCount = rows.Where(r => r.IsActive).Select(r => r.MemberId).Distinct().Count(),
                AssignmentCount = rows.Count(r => r.IsActive),

                TelegramReady = AppSettings.Telegram.IsConfigured,
                EmailReady = AppSettings.Email.IsConfigured
            };

            return View(model);
        }

        /// <summary>
        /// Dự án đang chạy nhưng báo cáo mới nhất đã cũ ít nhất <see cref="SilentWeeks"/> tuần
        /// (hoặc chưa có báo cáo nào). Chỉ xét trên các phân công đang tham gia.
        /// </summary>
        private static List<DashboardSilentProject> BuildSilentProjects(List<SummaryRow> rows, int curYear, int curWeek)
        {
            var openStatuses = new HashSet<string>(
                AppSettings.Dashboard.WorkloadProjectStatuses, StringComparer.CurrentCultureIgnoreCase);
            var curStart = WeekHelper.FirstDayOfWeek(curYear, curWeek);

            var result = new List<DashboardSilentProject>();

            var byProject = rows
                .Where(r => r.IsActive && r.ProjectStatus != null && openStatuses.Contains(r.ProjectStatus))
                .GroupBy(r => r.ProjectId);

            foreach (var g in byProject)
            {
                var withLog = g.Where(r => r.LatestYear.HasValue && r.LatestWeek.HasValue).ToList();

                int weeksSilent;
                string label;
                if (withLog.Count == 0)
                {
                    weeksSilent = int.MaxValue;
                    label = null;
                }
                else
                {
                    // Tuần báo cáo mới nhất trong toàn bộ phân công của dự án.
                    var latest = withLog
                        .OrderByDescending(r => r.LatestYear.Value)
                        .ThenByDescending(r => r.LatestWeek.Value)
                        .First();
                    var lastStart = WeekHelper.FirstDayOfWeek(latest.LatestYear.Value, latest.LatestWeek.Value);
                    weeksSilent = (int)Math.Floor((curStart - lastStart).TotalDays / 7.0);
                    label = latest.WeekLabel;
                }

                if (weeksSilent < SilentWeeks) continue;

                var first = g.First();
                result.Add(new DashboardSilentProject
                {
                    ProjectId = g.Key,
                    ProjectName = first.ProjectName,
                    Customer = first.Customer,
                    PmName = first.Pm,
                    Status = first.ProjectStatus,
                    LastReportLabel = label,
                    WeeksSilent = weeksSilent == int.MaxValue ? 0 : weeksSilent
                });
            }

            // Im lặng lâu nhất (chưa có báo cáo) lên đầu; cùng mức thì theo tên.
            return result
                .OrderByDescending(p => p.LastReportLabel == null)
                .ThenByDescending(p => p.WeeksSilent)
                .ThenBy(p => p.ProjectName, StringComparer.CurrentCulture)
                .ToList();
        }
    }
}
