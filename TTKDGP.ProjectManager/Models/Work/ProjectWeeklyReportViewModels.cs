using System;
using System.Collections.Generic;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Một nút trên dải timeline tuần của năm.
    /// </summary>
    public class ProjectWeeklyTimelineItem
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrentWeek { get; set; }
        public bool IsSelected { get; set; }

        public bool HasReport { get; set; }
        public bool IsSubmitted { get; set; }
        public bool IsOnTime { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string SubmittedByName { get; set; }

        public int CompletedTaskCount { get; set; }
        public int DoingTaskCount { get; set; }
        public decimal TotalHours { get; set; }
    }

    /// <summary>
    /// Giờ công trong tuần gom theo thành viên dự án.
    /// </summary>
    public class ProjectWeeklyMemberLog
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public decimal TotalHours { get; set; }
        public int TaskCount { get; set; }
        public List<string> TaskTitles { get; set; }

        public ProjectWeeklyMemberLog()
        {
            TaskTitles = new List<string>();
        }
    }

    /// <summary>
    /// Tóm tắt đầu việc trong tuần (hoàn thành hoặc đang thực hiện).
    /// </summary>
    public class ProjectWeeklyTaskItem
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Kind { get; set; }
        public string State { get; set; }
        public int Progress { get; set; }
        public string AssigneeName { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public decimal HoursThisWeek { get; set; }
        public bool IsDoneThisWeek { get; set; }
    }

    /// <summary>
    /// Dữ liệu toàn diện cho Báo cáo tuần dự án: dải timeline, nội dung báo cáo và tự động tổng hợp.
    /// </summary>
    public class ProjectWeeklyReportViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public bool CanEdit { get; set; }

        public int SelectedYear { get; set; }
        public int SelectedWeek { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public DateTime Deadline { get; set; }
        public bool IsCurrentWeek { get; set; }

        public List<int> AvailableYears { get; set; }
        public List<ProjectWeeklyTimelineItem> Timeline { get; set; }

        public WorkWeekReport Report { get; set; }
        public bool HasExistingReport { get { return Report != null; } }
        public bool IsSubmitted { get { return Report != null && Report.IsSubmitted; } }

        // Dữ liệu tự động tổng hợp từ hệ thống (Auto-aggregate)
        public int CompletedCount { get; set; }
        public int InProgressCount { get; set; }
        public int TotalTasksInProject { get; set; }
        public int ProjectOverallProgress { get; set; }
        public decimal TotalLogHoursThisWeek { get; set; }

        public List<ProjectWeeklyTaskItem> CompletedTasks { get; set; }
        public List<ProjectWeeklyTaskItem> InProgressTasks { get; set; }
        public List<ProjectWeeklyMemberLog> MemberHours { get; set; }

        // Gợi ý nội dung tự động tổng hợp (dùng khi bấm "Tự động tổng hợp" hoặc chưa có báo cáo)
        public string SuggestedCurrentWork { get; set; }
        public string SuggestedNextWeekNote { get; set; }

        public ProjectWeeklyReportViewModel()
        {
            AvailableYears = new List<int>();
            Timeline = new List<ProjectWeeklyTimelineItem>();
            CompletedTasks = new List<ProjectWeeklyTaskItem>();
            InProgressTasks = new List<ProjectWeeklyTaskItem>();
            MemberHours = new List<ProjectWeeklyMemberLog>();
        }
    }
}
