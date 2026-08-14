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

        public string AssigneeName { get; set; }
        public int Progress { get; set; }

        /// <summary>Gia tri tho cua TaskKinds (vi du "Checklist", "HoTro", "NgoaiDuAn").</summary>
        public string Kind { get; set; }

        /// <summary>0 = muc goc, khac 0 = viec con cua muc co Id do.</summary>
        public int ParentId { get; set; }
    }

    /// <summary>Mot nguoi co the giao viec — dropdown "Nguoi thuc hien" khi tao dau viec moi,
    /// CHI gom thanh vien dang hoat dong cua du an (khop ProjectMembers ben web).</summary>
    public class AssigneeOptionDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
    }

    /// <summary>Danh sach dau viec cua mot du an — man "Checklist" cua mobile, tuong duong
    /// ChecklistController.Index ben web nhung KHONG dung cay cha/con (BuildTree) — tra danh
    /// sach phang, sap xep qua han truoc nhu WorkService.Sort, mobile tu loc theo tu khoa/trang
    /// thai/han o client vi so luong dau viec moi du an thuong khong lon.</summary>
    public class ChecklistDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string PmName { get; set; }

        /// <summary>Nguoi nay co sua duoc TOAN BO dau viec cua du an khong (PM/Quan ly To) — quyet
        /// dinh co hien nut "Them muc" hay khong. Tung dau viec van co the tu sua duoc du co la
        /// false (neu la nguoi duoc giao chinh viec do) — xem TaskDto rieng khong mang co nay,
        /// mobile ban dau chi doc/them, chua co sua/xoa ngay tren danh sach.</summary>
        public bool CanEdit { get; set; }

        public int TotalCount { get; set; }
        public int DoneCount { get; set; }
        public int OverdueCount { get; set; }
        public int DonePercent { get; set; }

        public List<TaskDto> Tasks { get; set; }

        /// <summary>Danh sach cho dropdown "Nguoi thuc hien" — luon tra du CanEdit hay khong, don
        /// gian hoa phia mobile (khong phai goi rieng khi mo form).</summary>
        public List<AssigneeOptionDto> Assignees { get; set; }

        public ChecklistDto()
        {
            Tasks = new List<TaskDto>();
            Assignees = new List<AssigneeOptionDto>();
        }
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

    public class ProjectMemberDto
    {
        public string UserFullName { get; set; }
        public bool IsPm { get; set; }
        public string Role { get; set; }

        /// <summary>Gia tri tho cua AssignmentPhases.</summary>
        public string Phase { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class WeekReportSummaryDto
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public bool IsSubmitted { get; set; }
        public bool IsOnTime { get; set; }
    }

    /// <summary>
    /// Chi tiet mot du an — tuong duong ProjectDetailsViewModel ben web
    /// (Controllers/WorkProjectsController.cs) nhung CO CHU DICH BO phan "Thong tin trien khai"
    /// (Github/SVN/FTP/DB — ca mat khau) va tai lieu dinh kem: day la du lieu nhay cam, chua nen
    /// dua len thiet bi di dong truoc khi ra soat rieng ve ATTT (xem FileMoTa/Backlog-Mobile.md).
    /// </summary>
    public class ProjectDetailDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Customer { get; set; }
        public string ProjectType { get; set; }
        public string Phase { get; set; }
        public string State { get; set; }
        public bool IsOpen { get; set; }
        public string PmName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        /// <summary>Da chuyen ve van ban thuan (HtmlSanitizer.ToPlainText) — mobile chua co trinh
        /// hien thi HTML rieng.</summary>
        public string Description { get; set; }
        public bool CanEdit { get; set; }

        public int ActiveMemberCount { get; set; }
        public int PastMemberCount { get; set; }
        public int ChecklistDone { get; set; }
        public int ChecklistTotal { get; set; }
        public int ChecklistOverdue { get; set; }
        public int SupportThisWeek { get; set; }

        public List<ProjectMemberDto> Members { get; set; }
        public List<TaskDto> OverdueTasks { get; set; }
        public WeekReportSummaryDto CurrentReport { get; set; }
        public List<WeekReportSummaryDto> RecentReports { get; set; }

        public ProjectDetailDto()
        {
            Members = new List<ProjectMemberDto>();
            OverdueTasks = new List<TaskDto>();
            RecentReports = new List<WeekReportSummaryDto>();
        }
    }

    /// <summary>
    /// Mot dong trong "Du an cua toi" — khop MyProjectRow ben web (Controllers/MyWorkController.cs)
    /// de doi chieu, nhung khong mang PagedList/ViewBag.
    /// </summary>
    public class MyProjectRowDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Customer { get; set; }
        public string PmName { get; set; }

        /// <summary>Gia tri tho cua ProjectPhases.</summary>
        public string Phase { get; set; }

        /// <summary>Gia tri tho cua ProjectStates.</summary>
        public string State { get; set; }
        public bool IsOpen { get; set; }

        public bool IsPm { get; set; }
        public bool IsActiveMember { get; set; }

        /// <summary>Vai tro tu do (vi du "Dev", "Tester") — rong neu chi la PM khong co phan cong.</summary>
        public string Role { get; set; }
        public DateTime? JoinedAt { get; set; }

        /// <summary>Chi co gia tri khi da roi (khong con la thanh vien dang hoat dong).</summary>
        public DateTime? LeftAt { get; set; }

        public int ChecklistDone { get; set; }
        public int ChecklistTotal { get; set; }
        public int ChecklistPercent { get; set; }

        public int MyOpenCount { get; set; }
        public int MyOverdueCount { get; set; }

        /// <summary>"DaNopDungHan" | "DaNopTreHan" | "ConBanNhap" | "ChuaNop" | "KhongApDung"
        /// (KhongApDung = khong phai PM va chua co ban nao) — khop dung logic hien badge ben web
        /// (Views/MyWork/Projects.cshtml).</summary>
        public string ReportStatus { get; set; }
    }

    public class MyProjectsDto
    {
        /// <summary>Tong so du an cua nguoi nay, KHONG theo bo loc (de biet dang giau bao nhieu).</summary>
        public int TotalCount { get; set; }
        public int PmCount { get; set; }
        public int ClosedCount { get; set; }
        public List<MyProjectRowDto> Projects { get; set; }

        public MyProjectsDto()
        {
            Projects = new List<MyProjectRowDto>();
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
