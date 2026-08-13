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

    /// <summary>Dem viec cua ca nhan theo trang thai — khop DashboardMyTasks ben web (khong ke Focus,
    /// da co rieng o DashboardDto.Tasks).</summary>
    public class TaskStatsDto
    {
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int NotStartedCount { get; set; }
        public int OverdueCount { get; set; }
        public int DueSoonCount { get; set; }
        public int DoneCount { get; set; }
    }

    /// <summary>
    /// Diem KPI thang hien tai, rut gon so voi web (bo bang chi tiet Support/Execute/Assigned —
    /// man dien thoai chi can con so tong va trang thai thieu gio, xem chi tiet thi mo trang web).
    /// </summary>
    public class KpiSummaryDto
    {
        public decimal FinalPoint { get; set; }

        /// <summary>Gia tri tho cua KpiRanks (vi du "Tot", "Dat"...).</summary>
        public string Rank { get; set; }
        public decimal ScoreMax { get; set; }
        public int ScorePercent { get; set; }
        public decimal WorkedHours { get; set; }
        public decimal RequiredHours { get; set; }
        public int HoursPercent { get; set; }

        /// <summary>So gio con thieu so voi yeu cau thang; 0 khi da du.</summary>
        public decimal HoursShort { get; set; }
    }

    public class UpcomingLeaveDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal Days { get; set; }

        /// <summary>Gia tri tho cua LeaveKinds (vi du "PhepNam").</summary>
        public string Kind { get; set; }
    }

    public class LeaveSummaryDto
    {
        public decimal ApprovedDays { get; set; }
        public int PendingCount { get; set; }
        public List<UpcomingLeaveDto> Upcoming { get; set; }

        public LeaveSummaryDto()
        {
            Upcoming = new List<UpcomingLeaveDto>();
        }
    }

    /// <summary>Mot du an trong danh sach "Du an can chu y" — nhieu viec qua han nhat len dau.</summary>
    public class ProjectAttentionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Customer { get; set; }
        public string PmName { get; set; }
        public int ProgressPercent { get; set; }
        public int OverdueCount { get; set; }
    }

    /// <summary>Phan toan To, chi tra khi tai khoan la Quan ly To (xem DashboardDto.CanSeeTeam).</summary>
    public class TeamSummaryDto
    {
        public int OpenProjectCount { get; set; }
        public int MyPmCount { get; set; }
        public int OpenTaskCount { get; set; }
        public int OverdueTaskCount { get; set; }
        public List<ProjectAttentionDto> Projects { get; set; }

        public TeamSummaryDto()
        {
            Projects = new List<ProjectAttentionDto>();
        }
    }

    public class DashboardDto
    {
        public int ProjectCount { get; set; }
        public int TodayTaskCount { get; set; }
        public List<ProjectSummaryDto> Projects { get; set; }

        /// <summary>Viec dang can lam: qua han hoac sap den han, gioi han 20 dong.</summary>
        public List<TaskDto> Tasks { get; set; }

        public TaskStatsDto TaskStats { get; set; }

        /// <summary>Diem KPI thang hien tai; null neu chua dang nhap (khong xay ra qua ApiAuthorize).</summary>
        public KpiSummaryDto Kpi { get; set; }
        public LeaveSummaryDto Leave { get; set; }

        public bool CanSeeTeam { get; set; }
        public bool CanApproveLeave { get; set; }

        /// <summary>So don nghi phep toan he thong dang cho duyet; chi co nghia khi CanApproveLeave.</summary>
        public int PendingLeaveApprovalCount { get; set; }

        /// <summary>Phan toan To; null khi CanSeeTeam = false.</summary>
        public TeamSummaryDto Team { get; set; }

        public DashboardDto()
        {
            Projects = new List<ProjectSummaryDto>();
            Tasks = new List<TaskDto>();
        }
    }
}
