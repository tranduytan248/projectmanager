using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TTKDGP.ProjectManager.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; }
    }

    /// <summary>Phần thông tin cá nhân người dùng tự sửa được.</summary>
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        [StringLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự")]
        public string FullName { get; set; }
    }

    /// <summary>
    /// Trang "Thông tin cá nhân": gồm form sửa thông tin và form đổi mật khẩu.
    /// Hai form post về hai action khác nhau, mỗi form bind theo tiền tố riêng.
    /// </summary>
    public class ProfilePageViewModel
    {
        public ProfileViewModel Profile { get; set; }
        public ChangePasswordViewModel ChangePassword { get; set; }

        // Các thông tin chỉ để xem, người dùng không tự sửa được
        public string UserName { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsAdmin
        {
            get { return Models.Roles.Has(Role, Models.Roles.Admin); }
        }

        /// <summary>Tên các quyền của tài khoản, nhiều quyền thì nối bằng dấu phẩy.</summary>
        public string RoleDisplay
        {
            get { return Models.Roles.Display(Role); }
        }

        public ProfilePageViewModel()
        {
            Profile = new ProfileViewModel();
            ChangePassword = new ChangePasswordViewModel();
        }
    }

    /// <summary>
    /// Một mắt trên thanh breadcrumb đầu trang. View nào cần thì gán ViewBag.Crumbs =
    /// List&lt;Crumb&gt;; _Layout tự vẽ. Mắt cuối (trang hiện tại) không cần Url.
    /// </summary>
    public class Crumb
    {
        public string Label { get; set; }
        public string Url { get; set; }
    }

    /// <summary>Form tạo/sửa người dùng. Khi sửa, để trống mật khẩu nghĩa là giữ nguyên.</summary>
    public class UserEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        [StringLength(60, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 60 ký tự")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        [StringLength(120, ErrorMessage = "Email tối đa 120 ký tự")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        /// <summary>
        /// Các quyền được chọn (chọn được nhiều). Lưu xuống <see cref="User.Role"/> dưới dạng
        /// chuỗi ngăn bởi dấu phẩy.
        /// </summary>
        [Display(Name = "Phân quyền")]
        public string[] SelectedRoles { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; }

        public bool IsNew { get { return Id == 0; } }
    }

    /// <summary>Trạng thái một luồng tự động, hiện trên dashboard Quản trị.</summary>
    public class AutomationStatus
    {
        public string Name { get; set; }
        /// <summary>ok / warn / off — quyết định màu chấm.</summary>
        public string State { get; set; }
        public string Detail { get; set; }
    }

    /// <summary>Số tài khoản của một nhóm quyền.</summary>
    public class GroupUserCount
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public bool Highlight { get; set; }
    }

    /// <summary>Dashboard "Tình trạng hệ thống" của Quản trị.</summary>
    public class AdminSystemViewModel
    {
        public List<AutomationStatus> Flows { get; set; }
        public List<GroupUserCount> GroupCounts { get; set; }

        public int TotalUsers { get; set; }
        public int ActiveIntegrations { get; set; }

        public AdminSystemViewModel()
        {
            Flows = new List<AutomationStatus>();
            GroupCounts = new List<GroupUserCount>();
        }
    }

    /// <summary>Một dự án đang chạy nhưng lâu không có báo cáo — hiện trên dashboard Manager.</summary>
    public class DashboardSilentProject
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Customer { get; set; }
        public string PmName { get; set; }
        public string Status { get; set; }

        /// <summary>Nhãn tuần báo cáo mới nhất, ví dụ "Tuần 27/2026"; null nếu chưa có.</summary>
        public string LastReportLabel { get; set; }
        public int WeeksSilent { get; set; }
    }

    /// <summary>Dashboard "Việc cần xử lý" của Quản lý — số liệu tuần đang diễn ra.</summary>
    public class ManagerDashboardViewModel
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public string WeekLabel { get; set; }
        public DateTime WeekFrom { get; set; }
        public DateTime WeekTo { get; set; }

        /// <summary>Nhân sự chưa ghi báo cáo tuần này (tái dùng kiểu của bộ nhắc).</summary>
        public List<MemberReminder> MissingMembers { get; set; }

        public int ReportedProjects { get; set; }
        public int MissingProjects { get; set; }
        public int TotalOpenProjects { get; set; }

        public List<DashboardSilentProject> SilentProjects { get; set; }

        // Ô số liệu
        public int OpenProjectCount { get; set; }
        public int ActiveMemberCount { get; set; }
        public int AssignmentCount { get; set; }

        public bool TelegramReady { get; set; }
        public bool EmailReady { get; set; }

        public int MissingMemberCount { get { return MissingMembers == null ? 0 : MissingMembers.Count; } }
        public int ProjectReportPercent
        {
            get { return TotalOpenProjects <= 0 ? 0 : (int)System.Math.Round(ReportedProjects * 100.0 / TotalOpenProjects); }
        }

        public ManagerDashboardViewModel()
        {
            MissingMembers = new List<MemberReminder>();
            SilentProjects = new List<DashboardSilentProject>();
        }
    }

    /// <summary>Form tạo / sửa một nhóm quyền kèm ma trận chức năng.</summary>
    public class RoleGroupEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã nhóm")]
        [Display(Name = "Mã nhóm")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Mã nhóm từ 2 đến 50 ký tự")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhóm")]
        [Display(Name = "Tên nhóm")]
        [StringLength(100, ErrorMessage = "Tên nhóm tối đa 100 ký tự")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        [StringLength(300, ErrorMessage = "Mô tả tối đa 300 ký tự")]
        public string Description { get; set; }

        [Display(Name = "Đang dùng")]
        public bool IsActive { get; set; }

        /// <summary>Toàn quyền: cấp mọi chức năng, tự bao gồm cả chức năng thêm về sau ("*").</summary>
        [Display(Name = "Toàn quyền")]
        public bool GrantAll { get; set; }

        /// <summary>Các mã chức năng được tick trên ma trận (bỏ qua nếu GrantAll).</summary>
        public string[] SelectedPermissions { get; set; }

        public bool IsNew { get { return Id == 0; } }

        /// <summary>Số tài khoản đang dùng nhóm này (để cảnh báo khi sửa/xoá).</summary>
        public int UserCount { get; set; }

        public RoleGroupEditViewModel()
        {
            IsActive = true;
            SelectedPermissions = new string[0];
        }
    }

    /// <summary>Một nhân sự HRM trong màn mở tài khoản.</summary>
    public class UserProvisionRow
    {
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        /// <summary>Tên đăng nhập HRM, suy từ email (bỏ phần đuôi miền).</summary>
        public string UserName { get; set; }

        /// <summary>Tài khoản đang có của người này; 0 nghĩa là sắp mở mới.</summary>
        public int UserId { get; set; }
    }

    /// <summary>Một nhân sự bị bỏ qua, kèm lý do để quản trị biết đường xử lý tay.</summary>
    public class UserProvisionSkip
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Xem trước rồi mở hàng loạt tài khoản cho nhân sự HRM của đơn vị. Xem trước trước khi ghi để
    /// quản trị nhìn thấy đúng những tài khoản nào sắp sinh ra, tránh tạo nhầm bản trùng.
    /// Chỉ MỞ tài khoản — không gắn tài khoản với hồ sơ nhân sự hay thành viên nào.
    /// </summary>
    public class UserProvisionViewModel
    {
        /// <summary>Tên đơn vị HRM đang đối chiếu, kèm mã nếu có.</summary>
        public string UnitName { get; set; }

        /// <summary>Lỗi cấu hình/dữ liệu đơn vị HRM. Có giá trị thì không hiện bảng đối chiếu.</summary>
        public string UnitError { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu khởi tạo")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [Display(Name = "Mật khẩu khởi tạo")]
        public string Password { get; set; }

        /// <summary>
        /// Đặt lại mật khẩu cho cả những tài khoản ĐÃ CÓ về mật khẩu khởi tạo. Là một lựa chọn
        /// riêng chứ không làm mặc định — đây là thao tác đụng vào mật khẩu người khác.
        /// </summary>
        [Display(Name = "Đặt lại mật khẩu tài khoản đã có")]
        public bool ResetExisting { get; set; }

        /// <summary>Nhân sự chưa có tài khoản — sẽ mở mới.</summary>
        public List<UserProvisionRow> ToCreate { get; set; }

        /// <summary>Nhân sự đã có tài khoản — phạm vi áp dụng của việc đặt lại mật khẩu.</summary>
        public List<UserProvisionRow> Existing { get; set; }

        public List<UserProvisionSkip> Skipped { get; set; }

        /// <summary>
        /// Tài khoản đang có nhưng không khớp nhân sự HRM nào (ví dụ tài khoản quản trị). Nêu ra
        /// cho biết, chức năng này không đụng tới.
        /// </summary>
        public List<User> UnlinkedUsers { get; set; }

        /// <summary>Có việc gì để làm không — không thì khỏi hiện nút.</summary>
        public bool HasWork
        {
            get { return ToCreate.Count > 0 || Existing.Count > 0; }
        }

        public UserProvisionViewModel()
        {
            ToCreate = new List<UserProvisionRow>();
            Existing = new List<UserProvisionRow>();
            Skipped = new List<UserProvisionSkip>();
            UnlinkedUsers = new List<User>();
        }
    }

    /// <summary>Một dòng trên màn hình tổng hợp ngoài FrontEnd.</summary>
    public class SummaryRow
    {
        /// <summary>Id của bản ghi phân công, dùng để dựng link sửa/xoá ở màn hình quản trị.</summary>
        public int AssignmentId { get; set; }

        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Customer { get; set; }
        public string ProjectType { get; set; }
        public string ProjectStatus { get; set; }

        /// <summary>Độ ưu tiên của trạng thái dự án — số nhỏ nổi lên đầu.</summary>
        public int ProjectStatusPriority { get; set; }

        public string Pm { get; set; }

        /// <summary>Id nhân sự làm PM của dự án; 0 nếu chưa đặt. Dùng để lọc theo PM.</summary>
        public int PmMemberId { get; set; }

        public int MemberId { get; set; }
        public string MemberName { get; set; }
        public string Role { get; set; }
        public string WorkStatus { get; set; }
        public string WorkContent { get; set; }
        public bool IsActive { get; set; }

        // ----- Tuần báo cáo mới nhất -----

        /// <summary>Tuần của mốc nhật ký mới nhất; null nếu phân công chưa ghi nhật ký lần nào.</summary>
        public int? LatestWeek { get; set; }

        public int? LatestYear { get; set; }

        /// <summary>Nhãn ngắn, ví dụ "Tuần 29/2026".</summary>
        public string WeekLabel { get; set; }

        /// <summary>Khoảng ngày của tuần đó, dùng làm tooltip.</summary>
        public string WeekRange { get; set; }

        public bool HasLog { get { return LatestWeek.HasValue; } }
    }

    /// <summary>Dữ liệu cho màn hình tổng hợp: các dòng đã lọc + số liệu thống kê + nguồn cho bộ lọc.</summary>
    public class SummaryViewModel
    {
        public List<SummaryRow> Rows { get; set; }

        // ----- Tuần hiện tại -----

        public int CurrentYear { get; set; }
        public int CurrentWeek { get; set; }
        public DateTime CurrentWeekFrom { get; set; }
        public DateTime CurrentWeekTo { get; set; }

        // Bộ lọc hiện tại
        public string Keyword { get; set; }
        public int? MemberId { get; set; }
        public int? ProjectId { get; set; }

        /// <summary>Lọc theo PM của dự án (Id nhân sự làm PM).</summary>
        public int? PmId { get; set; }

        public string WorkStatus { get; set; }
        public bool ActiveOnly { get; set; }

        // Nguồn cho dropdown
        public List<Member> Members { get; set; }
        public List<Project> Projects { get; set; }

        /// <summary>Danh sách nhân sự đang làm PM của ít nhất một dự án — đổ vào dropdown PM.</summary>
        public List<Member> Pms { get; set; }

        public List<string> WorkStatuses { get; set; }

        // Thống kê (tính trên tập đã lọc)
        public int TotalAssignments { get; set; }
        public int DistinctMembers { get; set; }
        public int DistinctProjects { get; set; }
        public int ActiveAssignments { get; set; }

        /// <summary>Số lượng phân công theo từng trạng thái, để vẽ thanh phân bố.</summary>
        public List<StatusCount> StatusBreakdown { get; set; }

        /// <summary>Khối lượng của mỗi thành viên: tham gia bao nhiêu dự án.</summary>
        public List<MemberLoad> MemberLoads { get; set; }

        // ----- Công việc trong tháng hiện tại (bộ quản lý công việc mới) -----

        /// <summary>Tháng/năm của bảng tổng hợp công việc — luôn là tháng hiện tại.</summary>
        public int TaskMonth { get; set; }
        public int TaskMonthYear { get; set; }

        /// <summary>Công việc của từng nhân sự trong tháng, đếm theo trạng thái.</summary>
        public List<MemberTaskSummary> MonthTasks { get; set; }

        /// <summary>Dòng tổng cộng của bảng công việc trong tháng.</summary>
        public MemberTaskSummary MonthTaskTotal { get; set; }

        /// <summary>Tổng số công việc trong tháng hiện tại.</summary>
        public int MonthTaskCount { get; set; }

        /// <summary>Số dự án có ít nhất một công việc trong tháng hiện tại.</summary>
        public int MonthTaskProjectCount { get; set; }

        public SummaryViewModel()
        {
            Rows = new List<SummaryRow>();
            Members = new List<Member>();
            Projects = new List<Project>();
            Pms = new List<Member>();
            WorkStatuses = new List<string>();
            StatusBreakdown = new List<StatusCount>();
            MemberLoads = new List<MemberLoad>();
            MonthTasks = new List<MemberTaskSummary>();
            ActiveOnly = true;
        }
    }

    /// <summary>
    /// Tổng hợp công việc của một người trong một tháng, đếm theo trạng thái
    /// (dữ liệu từ bộ quản lý công việc mới — bảng WorkTasks).
    /// </summary>
    public class MemberTaskSummary
    {
        public int UserId { get; set; }
        public string UserName { get; set; }

        /// <summary>Số việc theo mã trạng thái, xem <see cref="TaskStates"/>.</summary>
        public Dictionary<string, int> ByState { get; set; }

        /// <summary>Việc đã quá hạn mà chưa đóng — nhắc riêng vì cần chú ý nhất.</summary>
        public int Overdue { get; set; }

        public int Total { get; set; }

        public MemberTaskSummary()
        {
            ByState = new Dictionary<string, int>();
        }

        public int Count(string state)
        {
            int n;
            return ByState.TryGetValue(state, out n) ? n : 0;
        }
    }

    public class StatusCount
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class MemberLoad
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; }
        public int ProjectCount { get; set; }
        public int ActiveProjectCount { get; set; }
    }

    /// <summary>Chi tiết một dự án ở FrontEnd công khai.</summary>
    public class ProjectDetailViewModel
    {
        public Project Project { get; set; }
        public List<SummaryRow> Members { get; set; }

        public ProjectDetailViewModel()
        {
            Members = new List<SummaryRow>();
        }
    }

    /// <summary>Màn hình quản trị phân công: dòng phân công kèm tên dự án/thành viên đã nối.</summary>
    public class AssignmentListViewModel
    {
        public List<SummaryRow> Rows { get; set; }
        public List<Member> Members { get; set; }
        public List<Project> Projects { get; set; }
        public int? MemberId { get; set; }
        public int? ProjectId { get; set; }
        public string Keyword { get; set; }

        public AssignmentListViewModel()
        {
            Rows = new List<SummaryRow>();
            Members = new List<Member>();
            Projects = new List<Project>();
        }
    }
}
