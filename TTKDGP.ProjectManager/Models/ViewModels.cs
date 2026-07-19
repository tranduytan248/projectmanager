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
            get { return string.Equals(Role, Models.Roles.Admin, StringComparison.OrdinalIgnoreCase); }
        }

        public string RoleDisplay
        {
            get { return IsAdmin ? "Quản trị" : "Quản lý"; }
        }

        public ProfilePageViewModel()
        {
            Profile = new ProfileViewModel();
            ChangePassword = new ChangePasswordViewModel();
        }
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

        [Required(ErrorMessage = "Vui lòng chọn phân quyền")]
        [Display(Name = "Phân quyền")]
        public string Role { get; set; }

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

        /// <summary>Chi tiết tham gia gom nhóm theo thành viên — cách hiển thị chính ở FrontEnd.</summary>
        public List<MemberParticipation> ByMember { get; set; }

        // Bộ lọc hiện tại
        public string Keyword { get; set; }
        public int? MemberId { get; set; }
        public int? ProjectId { get; set; }
        public string WorkStatus { get; set; }
        public bool ActiveOnly { get; set; }

        // Nguồn cho dropdown
        public List<Member> Members { get; set; }
        public List<Project> Projects { get; set; }
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

        public SummaryViewModel()
        {
            Rows = new List<SummaryRow>();
            ByMember = new List<MemberParticipation>();
            Members = new List<Member>();
            Projects = new List<Project>();
            WorkStatuses = new List<string>();
            StatusBreakdown = new List<StatusCount>();
            MemberLoads = new List<MemberLoad>();
            ActiveOnly = true;
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

    /// <summary>Khối "Chi tiết tham gia" của một thành viên: các dự án người đó đang làm.</summary>
    public class MemberParticipation
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; }

        /// <summary>Các dự án của thành viên này, đã sắp theo độ ưu tiên trạng thái dự án.</summary>
        public List<SummaryRow> Rows { get; set; }

        /// <summary>Các dòng hiện ngay — dự án ở mức ưu tiên cao nhất mà thành viên này đang có.</summary>
        public List<SummaryRow> VisibleRows { get; set; }

        /// <summary>Các dòng còn lại, gập sau nút "Xem thêm".</summary>
        public List<SummaryRow> HiddenRows { get; set; }

        public int ProjectCount { get { return Rows.Select(r => r.ProjectId).Distinct().Count(); } }
        public int ActiveCount { get { return Rows.Count(r => r.IsActive); } }

        /// <summary>Các vai trò người này đảm nhận, để hiện gọn ở tiêu đề khối.</summary>
        public List<string> Roles
        {
            get
            {
                return Rows
                    .Select(r => r.Role)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(r => r, StringComparer.CurrentCulture)
                    .ToList();
            }
        }

        public MemberParticipation()
        {
            Rows = new List<SummaryRow>();
            VisibleRows = new List<SummaryRow>();
            HiddenRows = new List<SummaryRow>();
        }
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
