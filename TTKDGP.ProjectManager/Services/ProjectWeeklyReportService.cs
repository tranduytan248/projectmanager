using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Dịch vụ xử lý Báo cáo tuần dự án dạng Timeline theo tuần/năm và Tự động tổng hợp dữ liệu tiến độ.
    /// </summary>
    public static class ProjectWeeklyReportService
    {
        /// <summary>
        /// Dựng ViewModel đầy đủ cho Báo cáo tuần dự án.
        /// </summary>
        public static ProjectWeeklyReportViewModel BuildViewModel(int projectId, int? year, int? week, bool canEdit)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null) return null;

            var currentYear = WeekHelper.CurrentYear;
            var currentWeek = WeekHelper.CurrentWeek;

            var selectedYear = year.HasValue && year.Value >= 2020 && year.Value <= currentYear + 2
                ? year.Value
                : currentYear;

            var selectedWeek = week.HasValue && week.Value >= 1 && week.Value <= 53
                ? week.Value
                : (selectedYear == currentYear ? currentWeek : 1);

            var weekStart = WeekHelper.FirstDayOfWeek(selectedYear, selectedWeek).Date;
            var weekEnd = WeekHelper.LastDayOfWeek(selectedYear, selectedWeek).Date;
            var deadline = WorkService.ReportDeadline(selectedYear, selectedWeek);

            var model = new ProjectWeeklyReportViewModel
            {
                ProjectId = projectId,
                ProjectName = project.Name,
                CanEdit = canEdit,
                SelectedYear = selectedYear,
                SelectedWeek = selectedWeek,
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd,
                Deadline = deadline,
                IsCurrentWeek = (selectedYear == currentYear && selectedWeek == currentWeek)
            };

            // 1. Danh sách các năm có dữ liệu (từ năm tạo dự án đến hiện tại)
            var startYear = project.CreatedAt.Year > 2020 ? project.CreatedAt.Year : 2024;
            for (var y = currentYear + 1; y >= startYear; y--)
            {
                model.AvailableYears.Add(y);
            }
            if (!model.AvailableYears.Contains(selectedYear))
            {
                model.AvailableYears.Add(selectedYear);
                model.AvailableYears.Sort((a, b) => b.CompareTo(a));
            }

            // 2. Dữ liệu công việc & giờ công của dự án
            var tasks = WorkService.TasksOfProject(projectId);
            var taskIds = new HashSet<int>(tasks.Select(t => t.Id));

            var allReports = Repository.WorkWeekReports.All()
                .Where(r => r.ProjectId == projectId && r.Year == selectedYear)
                .ToList();

            var reportsByWeek = allReports.ToDictionary(r => r.Week, r => r);

            // Logtime trong năm đang xét để tính cho timeline
            var yearStart = new DateTime(selectedYear, 1, 1);
            var yearEnd = new DateTime(selectedYear, 12, 31);
            var projectLogsInYear = Repository.WorkTimeLogs.All()
                .Where(l => taskIds.Contains(l.TaskId) && l.WorkDate.Date >= yearStart && l.WorkDate.Date <= yearEnd)
                .ToList();

            // 3. Dựng dải Timeline các tuần trong năm
            // Giới hạn số tuần hiển thị: nếu năm hiện tại thì hiện từ tuần 1 tới tuần hiện tại + 2 tuần tới
            var maxWeek = selectedYear == currentYear ? Math.Min(53, currentWeek + 2) : 52;
            for (var w = 1; w <= maxWeek; w++)
            {
                var wStart = WeekHelper.FirstDayOfWeek(selectedYear, w).Date;
                var wEnd = WeekHelper.LastDayOfWeek(selectedYear, w).Date;

                WorkWeekReport rep;
                reportsByWeek.TryGetValue(w, out rep);

                // Giờ công tuần này
                var wLogs = projectLogsInYear.Where(l => l.WorkDate.Date >= wStart && l.WorkDate.Date <= wEnd).ToList();
                var wHours = wLogs.Sum(l => l.Hours);

                // Đầu việc hoàn thành trong tuần này
                var wDoneCount = tasks.Count(t =>
                    t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= wStart && t.CompletedAt.Value.Date <= wEnd);

                // Đầu việc đang triển khai
                var wDoingCount = tasks.Count(t =>
                    t.State != TaskStates.Done && t.State != TaskStates.Cancelled &&
                    (!t.StartDate.HasValue || t.StartDate.Value.Date <= wEnd));

                var item = new ProjectWeeklyTimelineItem
                {
                    Year = selectedYear,
                    Week = w,
                    StartDate = wStart,
                    EndDate = wEnd,
                    IsCurrentWeek = (selectedYear == currentYear && w == currentWeek),
                    IsSelected = (w == selectedWeek),
                    HasReport = rep != null,
                    IsSubmitted = rep != null && rep.IsSubmitted,
                    IsOnTime = rep != null && rep.IsOnTime,
                    SubmittedAt = rep != null ? rep.SubmittedAt : (DateTime?)null,
                    SubmittedByName = rep != null ? rep.UpdatedBy : null,
                    CompletedTaskCount = wDoneCount,
                    DoingTaskCount = wDoingCount,
                    TotalHours = wHours
                };

                model.Timeline.Add(item);
            }

            // 4. Báo cáo của tuần đang chọn
            WorkWeekReport currentRep;
            reportsByWeek.TryGetValue(selectedWeek, out currentRep);
            model.Report = currentRep;

            // 5. Thống kê tiến độ dự án
            model.TotalTasksInProject = tasks.Count;
            var doneTasks = tasks.Where(t => t.State == TaskStates.Done).ToList();
            model.ProjectOverallProgress = tasks.Count > 0 ? (doneTasks.Count * 100 / tasks.Count) : 0;

            // 6. Chi tiết công việc trong tuần được chọn
            // a) Các đầu việc hoàn thành trong tuần
            var completedTasksThisWeek = tasks.Where(t =>
                (t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= weekStart && t.CompletedAt.Value.Date <= weekEnd) ||
                (t.State == TaskStates.Done && t.UpdatedAt.HasValue && t.UpdatedAt.Value.Date >= weekStart && t.UpdatedAt.Value.Date <= weekEnd)
            ).ToList();

            // b) Các đầu việc đang làm dở dang hoặc mới phát sinh
            var inProgressTasksThisWeek = tasks.Where(t =>
                t.State != TaskStates.Done && t.State != TaskStates.Cancelled
            ).OrderByDescending(t => t.Progress).ThenBy(t => t.DueDate).ToList();

            // c) Giờ công của dự án trong tuần được chọn
            var logsThisWeek = Repository.WorkTimeLogs.All()
                .Where(l => taskIds.Contains(l.TaskId) && l.WorkDate.Date >= weekStart && l.WorkDate.Date <= weekEnd)
                .ToList();

            model.TotalLogHoursThisWeek = logsThisWeek.Sum(l => l.Hours);
            model.CompletedCount = completedTasksThisWeek.Count;
            model.InProgressCount = inProgressTasksThisWeek.Count;

            // Map vào TaskItem
            var hoursByTask = logsThisWeek.GroupBy(l => l.TaskId).ToDictionary(g => g.Key, g => g.Sum(l => l.Hours));

            foreach (var t in completedTasksThisWeek)
            {
                decimal th;
                hoursByTask.TryGetValue(t.Id, out th);
                model.CompletedTasks.Add(new ProjectWeeklyTaskItem
                {
                    Id = t.Id,
                    Code = t.Code,
                    Title = t.Title,
                    Kind = t.Kind,
                    State = t.State,
                    Progress = 100,
                    AssigneeName = t.AssigneeName,
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    HoursThisWeek = th,
                    IsDoneThisWeek = true
                });
            }

            foreach (var t in inProgressTasksThisWeek)
            {
                decimal th;
                hoursByTask.TryGetValue(t.Id, out th);
                model.InProgressTasks.Add(new ProjectWeeklyTaskItem
                {
                    Id = t.Id,
                    Code = t.Code,
                    Title = t.Title,
                    Kind = t.Kind,
                    State = t.State,
                    Progress = t.Progress,
                    AssigneeName = t.AssigneeName,
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    HoursThisWeek = th,
                    IsDoneThisWeek = false
                });
            }

            // d) Giờ công theo từng nhân sự
            var usersMap = WorkService.ActiveUsers().ToDictionary(u => u.Id, u => u);
            var logsByUser = logsThisWeek.GroupBy(l => l.UserId);
            foreach (var g in logsByUser)
            {
                var uid = g.Key;
                string fullName = "Nhân sự #" + uid;
                string userName = "";
                User u;
                if (usersMap.TryGetValue(uid, out u))
                {
                    fullName = u.FullName;
                    userName = u.UserName;
                }

                var userTaskIds = g.Select(l => l.TaskId).Distinct().ToList();
                var taskNames = tasks.Where(t => userTaskIds.Contains(t.Id)).Select(t => t.Title).ToList();

                model.MemberHours.Add(new ProjectWeeklyMemberLog
                {
                    UserId = uid,
                    FullName = fullName,
                    UserName = userName,
                    TotalHours = g.Sum(l => l.Hours),
                    TaskCount = userTaskIds.Count,
                    TaskTitles = taskNames
                });
            }
            model.MemberHours = model.MemberHours.OrderByDescending(m => m.TotalHours).ToList();

            // 7. Auto-tổng hợp gợi ý nội dung (Auto-aggregate text)
            model.SuggestedCurrentWork = GenerateCurrentWorkText(model);
            model.SuggestedNextWeekNote = GenerateNextWeekPlanText(tasks, weekEnd);

            return model;
        }

        /// <summary>
        /// Tự động sinh nội dung phần "Công việc đang thực hiện / đã hoàn thành" từ dữ liệu thực tế.
        /// </summary>
        public static string GenerateCurrentWorkText(ProjectWeeklyReportViewModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("I. TIẾN ĐỘ VÀ KẾT QUẢ ĐẠT ĐƯỢC TRONG TUẦN {0}/{1} ({2:dd/MM} - {3:dd/MM}):",
                model.SelectedWeek, model.SelectedYear, model.WeekStartDate, model.WeekEndDate));
            sb.AppendLine(string.Format("- Tiến độ tổng thể dự án: {0}% ({1}/{2} đầu việc hoàn thành).",
                model.ProjectOverallProgress, model.CompletedTasks.Count, model.TotalTasksInProject));
            sb.AppendLine(string.Format("- Tổng giờ công ghi nhận trong tuần: {0:0.#} giờ.", model.TotalLogHoursThisWeek));
            sb.AppendLine();

            if (model.CompletedTasks.Count > 0)
            {
                sb.AppendLine(string.Format("1. Các đầu việc đã hoàn thành ({0} việc):", model.CompletedTasks.Count));
                foreach (var t in model.CompletedTasks)
                {
                    var codeStr = string.IsNullOrWhiteSpace(t.Code) ? "" : "[" + t.Code + "] ";
                    var whoStr = string.IsNullOrWhiteSpace(t.AssigneeName) ? "" : " (" + t.AssigneeName + ")";
                    var hoursStr = t.HoursThisWeek > 0 ? string.Format(" — {0:0.#}h", t.HoursThisWeek) : "";
                    sb.AppendLine(string.Format("  + {0}{1}{2}{3}", codeStr, t.Title, whoStr, hoursStr));
                }
                sb.AppendLine();
            }

            if (model.InProgressTasks.Count > 0)
            {
                sb.AppendLine(string.Format("2. Các đầu việc đang triển khai ({0} việc):", model.InProgressTasks.Count));
                foreach (var t in model.InProgressTasks.Take(8))
                {
                    var codeStr = string.IsNullOrWhiteSpace(t.Code) ? "" : "[" + t.Code + "] ";
                    var whoStr = string.IsNullOrWhiteSpace(t.AssigneeName) ? "" : " (" + t.AssigneeName + ")";
                    var dueStr = t.DueDate.HasValue ? string.Format(" — Hạn: {0:dd/MM}", t.DueDate.Value) : "";
                    sb.AppendLine(string.Format("  + {0}{1} ({2}%){3}{4}", codeStr, t.Title, t.Progress, whoStr, dueStr));
                }
                if (model.InProgressTasks.Count > 8)
                {
                    sb.AppendLine(string.Format("  + ... và {0} đầu việc khác đang xử lý.", model.InProgressTasks.Count - 8));
                }
                sb.AppendLine();
            }

            if (model.MemberHours.Count > 0)
            {
                sb.AppendLine("3. Giờ công theo nhân sự tham gia:");
                foreach (var m in model.MemberHours)
                {
                    sb.AppendLine(string.Format("  + {0}: {1:0.#} giờ ({2} đầu việc)", m.FullName, m.TotalHours, m.TaskCount));
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Tự động sinh nội dung phần "Kế hoạch tuần tiếp theo" từ hạn đầu việc.
        /// </summary>
        public static string GenerateNextWeekPlanText(List<WorkTask> tasks, DateTime weekEnd)
        {
            var nextWeekStart = weekEnd.AddDays(1).Date;
            var nextWeekEnd = nextWeekStart.AddDays(6).Date;

            var nextTasks = tasks.Where(t =>
                t.State != TaskStates.Done && t.State != TaskStates.Cancelled &&
                t.DueDate.HasValue && t.DueDate.Value.Date >= nextWeekStart && t.DueDate.Value.Date <= nextWeekEnd
            ).OrderBy(t => t.DueDate).ToList();

            var sb = new StringBuilder();
            sb.AppendLine(string.Format("II. KẾ HOẠCH TRỌNG TÂM TUẦN TIẾP THEO ({0:dd/MM} - {1:dd/MM}):", nextWeekStart, nextWeekEnd));

            if (nextTasks.Count > 0)
            {
                sb.AppendLine(string.Format("- Dự kiến hoàn thành các mục tiêu đến hạn ({0} việc):", nextTasks.Count));
                foreach (var t in nextTasks)
                {
                    var codeStr = string.IsNullOrWhiteSpace(t.Code) ? "" : "[" + t.Code + "] ";
                    var whoStr = string.IsNullOrWhiteSpace(t.AssigneeName) ? "" : " (" + t.AssigneeName + ")";
                    sb.AppendLine(string.Format("  + {0}{1}{2} (Hạn: {3:dd/MM})", codeStr, t.Title, whoStr, t.DueDate.Value));
                }
            }
            else
            {
                sb.AppendLine("- Tiếp tục bám sát tiến độ các đầu việc đang mở và phân công hỗ trợ theo checklist.");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Lưu hoặc Nộp báo cáo tuần của dự án.
        /// </summary>
        public static WorkWeekReport SaveReport(int projectId, int year, int week, string currentWork, string difficulties, string nextWeekNote, bool isSubmit, int userId, string userName)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null) return null;

            var existing = WorkService.FindReport(projectId, year, week);
            var now = DateTime.Now;
            var deadline = WorkService.ReportDeadline(year, week);

            if (existing != null)
            {
                existing.CurrentWork = currentWork ?? "";
                existing.Difficulties = difficulties ?? "";
                existing.NextWeekNote = nextWeekNote ?? "";
                existing.UpdatedAt = now;
                existing.UpdatedBy = userName;

                if (isSubmit)
                {
                    // Nếu chưa từng nộp thì chốt thời điểm nộp và cờ đúng hạn
                    if (!existing.IsSubmitted)
                    {
                        existing.SubmittedAt = now;
                        existing.SubmittedByUserId = userId;
                        existing.IsOnTime = now <= deadline;
                    }
                    else
                    {
                        // Đã nộp rồi thì cập nhật lại thông tin nhưng giữ nguyên trạng thái IsOnTime ban đầu
                        existing.SubmittedAt = now;
                        existing.SubmittedByUserId = userId;
                    }
                }

                Repository.WorkWeekReports.Update(existing);
                return existing;
            }
            else
            {
                var report = new WorkWeekReport
                {
                    ProjectId = projectId,
                    ProjectName = project.Name,
                    Year = year,
                    Week = week,
                    CurrentWork = currentWork ?? "",
                    Difficulties = difficulties ?? "",
                    NextWeekNote = nextWeekNote ?? "",
                    CreatedAt = now,
                    UpdatedAt = now,
                    UpdatedBy = userName
                };

                if (isSubmit)
                {
                    report.SubmittedAt = now;
                    report.SubmittedByUserId = userId;
                    report.IsOnTime = now <= deadline;
                }

                Repository.WorkWeekReports.Insert(report);
                return report;
            }
        }
    }
}
