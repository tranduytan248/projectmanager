using System;
using System.Collections.Generic;
using System.Web.Mvc;

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

        /// <summary>Danh sach ma quyen mobile can biet ngay luc dang nhap (hien chi gom
        /// "wteam.manage" khi tai khoan la Quan ly To) — de trong neu khong co. Xem
        /// AuthApiController.Login, gop dung ca hai loi cap Quan ly To (o tich rieng tai khoan HOAC
        /// quyen nhom) nhu BaseController.IsTeamManager dang lam cho web.</summary>
        public List<string> Permissions { get; set; }
    }

    /// <summary>Man "Thong tin ca nhan" cua mobile — tuong duong ProfilePageViewModel ben web
    /// (Account/Profile), chi giu phan hien thi (chinh sua FullName/doi mat khau la hai action
    /// rieng tren AccountApiController, khong nam trong DTO nay).</summary>
    public class ProfileDto
    {
        public string UserName { get; set; }
        public string FullName { get; set; }

        /// <summary>Da qua Roles.Display — ten (cac) nhom quyen, tra san server-side giong het
        /// ProfilePageViewModel.RoleDisplay, mobile khong can tu tra lai Ma nhom.</summary>
        public string RoleDisplay { get; set; }

        /// <summary>Vai Quan ly To (User.IsTeamManager) nam NGOAI nhom quyen (RoleDisplay) nen phai
        /// tra rieng — y het Views/Users/Index.cshtml hien them badge "Quan ly To" ben canh (cac)
        /// nhom quyen, khong gop chung vao RoleDisplay.</summary>
        public bool IsTeamManager { get; set; }

        public bool IsAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Mot thong bao ca nhan — khop UserNotification (Models/Work/UserNotification.cs),
    /// bo truong UserId (ngu y la cua nguoi dang goi). ProjectId/TaskId de mobile tu quyet dinh
    /// diem den khi bam vao, cung mot bo du lieu UserNotificationsController.Open dung ben web.</summary>
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public int ProjectId { get; set; }
        public int TaskId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Man "Thong bao" cua mobile — mot trang du lieu (khong phai panel "Recent" gioi
    /// han cho khung tha xuong ben web) kem tong so chua doc de hien/an nut "Doc tat ca".</summary>
    public class NotificationListDto
    {
        public List<NotificationDto> Items { get; set; }
        public int Page { get; set; }
        public bool HasMore { get; set; }
        public int UnreadCount { get; set; }
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
        public int AssigneeUserId { get; set; }
        public int Progress { get; set; }

        /// <summary>Gia tri tho cua TaskKinds (vi du "Checklist", "HoTro", "NgoaiDuAn").</summary>
        public string Kind { get; set; }

        /// <summary>0 = muc goc, khac 0 = viec con cua muc co Id do.</summary>
        public int ParentId { get; set; }

        /// <summary>Ngay tao dau viec — man danh sach (vi du Kanban Checklist mobile) dung de sap
        /// xep moi den cu trong tung cot.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Nguoi nay co sua duoc cac truong rieng cua minh (trang thai/tien do) khong —
        /// tinh san o server (CanEditTask), de man danh sach (vi du Kanban Checklist mobile) biet
        /// tung the co doi trang thai duoc hay khong ma khong phai goi rieng Detail.</summary>
        public bool CanEdit { get; set; }
    }

    /// <summary>Cac truong tinh cua rieng mot dau viec — phan "Task" trong TaskFullDetailDto tra
    /// ve cho man "Chi tiet cong viec" day du cua mobile (xem TaskFullDetailDto ben duoi cho khoi
    /// Gio cong/Viec can lam/Trao doi). Ke thua het truong cua TaskDto, cong them cac truong chi
    /// can khi xem rieng mot dau viec. CanEdit/CanEditAll tra san du man hien chua co form Cap
    /// nhat rieng (chi doc/ghi Gio cong-Viec can lam-Trao doi), de sau nay man biet co nen dan
    /// nguoi dung ra web sua cac truong con lai hay khong.</summary>
    public class TaskDetailDto : TaskDto
    {
        /// <summary>Da qua HtmlSanitizer.ToPlainText — cung cach ProjectDetailDto.Description dang
        /// lam voi WorkProject.Description, khong can them goi hien thi HTML rieng cho mobile.</summary>
        public string Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int ProjectId { get; set; }
        public int Week { get; set; }
        public int Year { get; set; }

        /// <summary>So ngay con lai toi han; am = da tre. Null khi khong co DueDate.</summary>
        public int? DaysLeft { get; set; }
        public bool IsOnTime { get; set; }
        public string ParentTitle { get; set; }
        public decimal BonusPercent { get; set; }
        public bool HasAttachment { get; set; }
        public string AttachmentName { get; set; }

        /// <summary>Nguoi nay co sua duoc TOAN BO truong cua dau viec (PM/Quan ly To) khong.</summary>
        public bool CanEditAll { get; set; }
    }

    /// <summary>Mot lan ghi gio cong — khop WorkTimeLog, kem san CanDelete (tinh o server: chi
    /// chu cua dong va viec chua dong duoc xoa) de mobile khong tu doan quyen.</summary>
    public class TimeLogEntryDto
    {
        public int Id { get; set; }
        public DateTime WorkDate { get; set; }
        public decimal Hours { get; set; }
        public string Note { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public bool CanDelete { get; set; }
    }

    /// <summary>Khoi "Gio cong" cua man chi tiet — gom tran/da dung/con lai theo ca viec lan theo
    /// ngay (cung cong thuc TimeLogService dang dung ben web), CanLog/BlockedReason de mobile
    /// biet co nen hien form ghi them hay khong ma khong phai tu suy luan lai luat.</summary>
    public class TimeLogSummaryDto
    {
        /// <summary>Null khi viec thieu ngay bat dau/han nen khong tinh duoc tran.</summary>
        public decimal? TaskCap { get; set; }
        public decimal TaskTotal { get; set; }
        public decimal? TaskRemaining { get; set; }
        public decimal TodayTotal { get; set; }
        public decimal TodayRemaining { get; set; }
        public decimal MaxPerDay { get; set; }
        public bool CanLog { get; set; }
        public string BlockedReason { get; set; }
        public List<TimeLogEntryDto> Logs { get; set; }

        public TimeLogSummaryDto()
        {
            Logs = new List<TimeLogEntryDto>();
        }
    }

    /// <summary>Mot dong "Viec can lam" (todolist con cua dau viec) — khop WorkTaskTodo.</summary>
    public class TodoItemDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsDone { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Khoi "Viec can lam" — CanManage tinh san o server (CanManageTodos) de mobile biet
    /// co hien nut them/sua/xoa hay khong.</summary>
    public class TodoSummaryDto
    {
        public bool CanManage { get; set; }
        public int DoneCount { get; set; }
        public int TotalCount { get; set; }
        public List<TodoItemDto> Items { get; set; }

        public TodoSummaryDto()
        {
            Items = new List<TodoItemDto>();
        }
    }

    /// <summary>Mot nguoi co the bi nhac ten bang @ trong Trao doi — khop TaskMentionOption
    /// (Models/Work/ChecklistModels.cs), nguon tu WorkService.TaskParticipants.</summary>
    public class TaskMentionOptionDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
    }

    /// <summary>Mot dong "Trao doi" (binh luan) — Content la HTML DA QUA HtmlSanitizer.Clean luc
    /// luu (danh sach the trang p/div/br/b/strong/i/em/u/s/strike/ul/ol/li/blockquote/h3/h4/a),
    /// mobile tu hien co dinh dang (dam/nghieng/danh sach...) qua bo doc rieng, KHONG con phang
    /// ve chu thuong nhu truoc — trao doi tren mobile gio cung la rich text giong web. Noi dung da
    /// thu hoi thi Content la null. CanRecall tinh san o server: dung cua chinh minh, hoac la
    /// PM/Quan ly To (CanModerate).</summary>
    public class TaskCommentDto
    {
        public int Id { get; set; }
        public string AuthorName { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool HasAttachment { get; set; }
        public string AttachmentName { get; set; }
        public bool CanRecall { get; set; }
    }

    public class CommentsSummaryDto
    {
        public List<TaskCommentDto> Comments { get; set; }

        /// <summary>Danh sach nguoi co the @nhac trong khoi Trao doi nay — y het
        /// TaskCommentsViewModel.Participants ben web, de mobile dung goi y khi go @.</summary>
        public List<TaskMentionOptionDto> Participants { get; set; }

        public CommentsSummaryDto()
        {
            Comments = new List<TaskCommentDto>();
            Participants = new List<TaskMentionOptionDto>();
        }
    }

    /// <summary>Wrapper tra ve cho man "Chi tiet cong viec" day du tren mobile — gop ca 4 khoi
    /// (thong tin, gio cong, viec can lam, trao doi) trong MOT lan goi API duy nhat.</summary>
    public class TaskFullDetailDto
    {
        public TaskDetailDto Task { get; set; }
        public TimeLogSummaryDto TimeLog { get; set; }
        public TodoSummaryDto Todo { get; set; }
        public CommentsSummaryDto Comments { get; set; }
    }

    /// <summary>Mot dong trong "Lich su thao tac" cua mot dau viec — khop TaskActivityLog
    /// (Models/Work/TaskActivityLog.cs). Description da dung san cau tieng Viet luc ghi, mobile
    /// chi viec hien thang, khong phai tu ghep chuoi.</summary>
    public class TaskActivityLogDto
    {
        public int Id { get; set; }
        public string ActorName { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
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
        /// <summary>Id cua WorkAssignment — can de goi SetPm/EndMember/RemoveMember.</summary>
        public int Id { get; set; }
        public string UserFullName { get; set; }
        public bool IsPm { get; set; }
        public string Role { get; set; }

        /// <summary>Gia tri tho cua AssignmentPhases.</summary>
        public string Phase { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public bool IsActive { get; set; }
        public string Note { get; set; }
    }

    /// <summary>Danh sach chon san cho form "Them nhan su vao du an" — khop ViewBag.UserOptions/
    /// ViewBag.Roles cua WorkProjectsController.AddMemberForm ben web.</summary>
    public class ProjectMemberFormOptionsDto
    {
        public List<AssigneeOptionDto> Users { get; set; }
        public List<string> Roles { get; set; }

        public ProjectMemberFormOptionsDto()
        {
            Users = new List<AssigneeOptionDto>();
            Roles = new List<string>();
        }
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
    /// Tham so form "Them du an" tren mobile (MyProjectsApiController.Create) — bind qua model
    /// thay vi tham so phang de dung duoc [AllowHtml] rieng cho Description, KHONG can
    /// [ValidateInput(false)] cho ca action (se tat validate luon ca Github/SVN/FTP/Database, cho
    /// phep client gui HTML/script vao nhung truong khong can HTML). Y het cach
    /// WorkProject.Description ben web dung [AllowHtml] — xem Models/Work/WorkProject.cs.
    /// </summary>
    public class CreateProjectRequestDto
    {
        public string Name { get; set; }
        public string Customer { get; set; }
        public string ProjectType { get; set; }
        public string Phase { get; set; }
        public string State { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [AllowHtml]
        public string Description { get; set; }

        public string GithubLink { get; set; }
        public string SvnLink { get; set; }
        public string FtpAccount { get; set; }
        public string FtpPassword { get; set; }
        public string DbType { get; set; }
        public string DbServer { get; set; }
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
    }

    /// <summary>Danh sach chon san cho form "Them du an" tren mobile — khop ViewBag.ProjectTypes
    /// cua WorkProjectsController.Edit ben web. Giai doan/Trang thai co dinh trong ma nguon
    /// (ProjectPhases/ProjectStates) nen mobile tu liet ke, khong can tra qua API.</summary>
    public class ProjectCreateFormOptionsDto
    {
        public List<string> ProjectTypes { get; set; }

        public ProjectCreateFormOptionsDto()
        {
            ProjectTypes = new List<string>();
        }
    }

    /// <summary>Ket qua tao du an moi — mobile chi can Id/Code/Name de bao thanh cong va tu reload
    /// lai danh sach qua MyProjectsApiController.Index, khong can tra day du nhu ProjectDetailDto.</summary>
    public class CreateProjectResultDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }

        /// <summary>Ten cac file gui kem bi tu choi (qua co/sai duoi) kem ly do — rong neu khong
        /// co file nao hoac tat ca deu luu duoc. Xem MyProjectsApiController.SaveProjectFiles.</summary>
        public List<string> RejectedFiles { get; set; }

        public CreateProjectResultDto()
        {
            RejectedFiles = new List<string>();
        }
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

    /// <summary>Số giờ logtime của một ngày trong tuần phục vụ vẽ biểu đồ Power Curve HUD.</summary>
    public class DailyLogTimeDto
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; }
        public decimal Hours { get; set; }
        public bool IsToday { get; set; }
    }

    /// <summary>Tổng hợp thời gian làm việc và logtime theo tuần/ngày cho Dashboard.</summary>
    public class WorkTimeDashboardDto
    {
        public decimal TotalHoursWeek { get; set; }
        public decimal TotalHoursMonth { get; set; }
        public decimal TodayHours { get; set; }
        public decimal TargetHoursPerDay { get; set; }
        public decimal MaxHoursPerDay { get; set; }
        public List<DailyLogTimeDto> DailyLogs { get; set; }

        public WorkTimeDashboardDto()
        {
            DailyLogs = new List<DailyLogTimeDto>();
            TargetHoursPerDay = 8.0m;
            MaxHoursPerDay = 12.0m;
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

        /// <summary>Thoi gian lam viec & Logtime 7 ngay trong tuan phuc vu bieu do Power Curve HUD.</summary>
        public WorkTimeDashboardDto WorkTime { get; set; }

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

    /// <summary>Mot don nghi phep — khop cac truong cua LeaveRequest, xem
    /// Controllers/Api/LeavesApiController.cs.</summary>
    public class LeaveRequestDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; }

        /// <summary>Gia tri tho cua LeaveKinds (PhepNam/KhongLuong/Om/Khac).</summary>
        public string Kind { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsHalfDay { get; set; }
        public string HalfDaySession { get; set; }
        public decimal Days { get; set; }
        public string Reason { get; set; }

        /// <summary>Gia tri tho cua LeaveStates (ChoDuyet/DaDuyet/TuChoi/DaHuy).</summary>
        public string State { get; set; }

        public string ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string ApproverNote { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Danh sach "Nghi phep cua toi" tren mobile — khop ViewBag cua LeavesController.My
    /// (Years/StatYear/ApprovedDaysInYear/PendingCount) cong danh sach don da loc.</summary>
    public class LeavesListDto
    {
        public int StatYear { get; set; }
        public decimal ApprovedDaysInYear { get; set; }
        public int PendingCount { get; set; }

        /// <summary>Cac nam co don, moi cho dropdown loc — giam dan.</summary>
        public List<int> Years { get; set; }

        public List<LeaveRequestDto> Items { get; set; }

        public LeavesListDto()
        {
            Years = new List<int>();
            Items = new List<LeaveRequestDto>();
        }
    }

    /// <summary>Danh sach "Duyet nghi phep" toan To tren mobile.</summary>
    public class LeaveApprovalsDataDto
    {
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public List<int> Years { get; set; }
        public List<AssigneeOptionDto> Members { get; set; }
        public List<LeaveRequestDto> Items { get; set; }

        public LeaveApprovalsDataDto()
        {
            Years = new List<int>();
            Members = new List<AssigneeOptionDto>();
            Items = new List<LeaveRequestDto>();
        }
    }

    /// <summary>Giao việc riêng — Danh sách việc riêng trả về cho Mobile.</summary>
    public class PrivateTasksDataDto
    {
        public int TotalCount { get; set; }
        public int OverdueCount { get; set; }
        public int InProgressCount { get; set; }
        public int DoneCount { get; set; }
        public List<PrivateTaskItemDto> Items { get; set; }
        public List<AssigneeOptionDto> Members { get; set; }

        public PrivateTasksDataDto()
        {
            Items = new List<PrivateTaskItemDto>();
            Members = new List<AssigneeOptionDto>();
        }
    }

    public class PrivateTaskItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string State { get; set; }
        public string Priority { get; set; }
        public int Progress { get; set; }
        public int AssigneeUserId { get; set; }
        public string AssigneeName { get; set; }
        public int AssignedByUserId { get; set; }
        public string AssignedByName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsOverdue { get; set; }
        public decimal BonusPercent { get; set; }
        public bool HasAttachment { get; set; }
        public string AttachmentName { get; set; }
        public long AttachmentSize { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PrivateTaskFormOptionsDto
    {
        public List<AssigneeOptionDto> Members { get; set; }
        public List<decimal> BonusOptions { get; set; }
        public List<string> Priorities { get; set; }

        public PrivateTaskFormOptionsDto()
        {
            Members = new List<AssigneeOptionDto>();
            BonusOptions = new List<decimal>();
            Priorities = new List<string>();
        }
    }

    // ==========================================
    // BẢNG ĐIỀU KHIỂN TỔ (TEAM DASHBOARD) DTOs
    // ==========================================

    public class TeamDashboardDto
    {
        public DateTime Today { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public bool IsCurrentMonth { get; set; }
        public int TotalMembers { get; set; }
        public int IdleCount { get; set; }
        public int OverdueTodayCount { get; set; }
        public List<TeamMemberRowDto> Members { get; set; }

        public TeamDashboardDto()
        {
            Members = new List<TeamMemberRowDto>();
        }
    }

    public class TeamMemberRowDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public List<TeamTodayTaskDto> TodayTasks { get; set; }
        public int TodayTaskCount { get; set; }
        public int OverdueTodayCount { get; set; }
        public KpiSummaryDto Kpi { get; set; }
        public decimal TotalPenalty { get; set; }
        public int TotalTasks { get; set; }
        public TeamProjectCountDto Implement { get; set; }
        public TeamProjectCountDto Support { get; set; }

        public TeamMemberRowDto()
        {
            TodayTasks = new List<TeamTodayTaskDto>();
            Implement = new TeamProjectCountDto();
            Support = new TeamProjectCountDto();
        }
    }

    public class TeamTodayTaskDto
    {
        public int TaskId { get; set; }
        public string Title { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string State { get; set; }
        public int Progress { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class TeamProjectCountDto
    {
        public int Projects { get; set; }
        public int Tasks { get; set; }
    }

    public class TeamMemberTaskItemDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string State { get; set; }
        public string Priority { get; set; }
        public int Progress { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsOverdue { get; set; }
        public decimal LoggedHours { get; set; }
    }

    public class TeamMemberTasksResultDto
    {
        public int UserId { get; set; }
        public string MemberName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string Kind { get; set; }
        public string KindLabel { get; set; }
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public List<TeamMemberTaskItemDto> Tasks { get; set; }

        public TeamMemberTasksResultDto()
        {
            Tasks = new List<TeamMemberTaskItemDto>();
        }
    }

    public class KpiMonthSummaryDto
    {
        public int Total { get; set; }
        public decimal AveragePoint { get; set; }
        public int PassCount { get; set; }
        public int PassPercent { get; set; }
        public int FailCount { get; set; }
        public int ShortHoursCount { get; set; }
        public string TopName { get; set; }
        public decimal TopPoint { get; set; }
        public int StandardDays { get; set; }
        public decimal ScaleMax { get; set; }
    }

    public class KpiMemberRowDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal SupportPoint { get; set; }
        public decimal SupportGrossPoint { get; set; }
        public decimal SupportHours { get; set; }
        public decimal SupportCapHours { get; set; }
        public decimal ExecutePoint { get; set; }
        public decimal ExecuteGrossPoint { get; set; }
        public decimal ExecuteHours { get; set; }
        public decimal ExecuteTargetHours { get; set; }
        public decimal AssignedPoint { get; set; }
        public decimal LatePenalty { get; set; }
        public decimal QualityPoint { get; set; }
        public int StandardDays { get; set; }
        public decimal LeaveDays { get; set; }
        public decimal RequiredHours { get; set; }
        public decimal WorkedHours { get; set; }
        public int AttendanceRate { get; set; }
        public decimal FinalPoint { get; set; }
        public string Rank { get; set; }
        public bool IsSaved { get; set; }
        public int RankIndex { get; set; }
    }

    public class KpiIndexDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public bool IsCurrentMonth { get; set; }
        public int SelectedUserId { get; set; }
        public KpiMonthSummaryDto Summary { get; set; }
        public List<KpiMemberRowDto> Rows { get; set; }
        public List<AssigneeOptionDto> Users { get; set; }
        public bool CanGenerate { get; set; }
        public bool CanConfig { get; set; }
        public int StandardDays { get; set; }
        public decimal ScaleMax { get; set; }

        public KpiIndexDto()
        {
            Rows = new List<KpiMemberRowDto>();
            Users = new List<AssigneeOptionDto>();
        }
    }

    public class KpiTaskItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Kind { get; set; }
        public string State { get; set; }
        public int Progress { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsOverdue { get; set; }
        public decimal LoggedHours { get; set; }
        public decimal EstimatedHours { get; set; }
        public bool IsLate { get; set; }
    }

    public class KpiDetailDto
    {
        public KpiMemberRowDto Row { get; set; }
        public List<KpiTaskItemDto> SupportTasks { get; set; }
        public List<KpiTaskItemDto> ExecuteTasks { get; set; }
        public List<KpiTaskItemDto> AssignedTasks { get; set; }
        public bool CanGenerate { get; set; }
        public bool CanConfig { get; set; }

        public KpiDetailDto()
        {
            SupportTasks = new List<KpiTaskItemDto>();
            ExecuteTasks = new List<KpiTaskItemDto>();
            AssignedTasks = new List<KpiTaskItemDto>();
        }
    }

    public class HolidayDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Kind { get; set; }
        public string KindName { get; set; }
        public int? Day { get; set; }
        public int? Month { get; set; }
        public DateTime? SpecificDate { get; set; }
        public int? Year { get; set; }
        public string Note { get; set; }
        public bool IsActive { get; set; }
        public string DisplayDate { get; set; }
    }

    public class HolidayListDto
    {
        public int Year { get; set; }
        public int TotalCount { get; set; }
        public List<HolidayDto> Holidays { get; set; }

        public HolidayListDto()
        {
            Holidays = new List<HolidayDto>();
        }
    }
}

