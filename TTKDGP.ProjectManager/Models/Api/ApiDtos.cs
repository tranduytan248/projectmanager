using System;
using System.Collections.Generic;

namespace TTKDGP.ProjectManager.Models.Api
{
    /// <summary>
    /// DTO phang rieng cho JSON tra ve mobile — KHONG dung lai cac ViewModel MVC hien co
    /// (MyTasksViewModel, DashboardViewModel...) vi chung mang theo PagedList, thuoc tinh chi
    /// danh cho Razor. Ten truong khop voi model goc (WorkTask/WorkProject) de de doi chieu.
    /// </summary>
    public class LoginResultDto
    {
        public string Token { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
    }

    public class TaskDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string ProjectName { get; set; }

        /// <summary>Gia tri tho cua TaskStates (vi du "DangLam") — mobile tu dich nhan hien thi.</summary>
        public string State { get; set; }

        /// <summary>Gia tri tho cua TaskPriorities (vi du "Cao").</summary>
        public string Priority { get; set; }

        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsDueToday { get; set; }
    }

    public class ProjectSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int ProgressPercent { get; set; }
        public int MemberCount { get; set; }
    }

    public class DashboardDto
    {
        public int ProjectCount { get; set; }
        public int TodayTaskCount { get; set; }
        public List<ProjectSummaryDto> Projects { get; set; }
        public List<TaskDto> Tasks { get; set; }

        public DashboardDto()
        {
            Projects = new List<ProjectSummaryDto>();
            Tasks = new List<TaskDto>();
        }
    }
}
