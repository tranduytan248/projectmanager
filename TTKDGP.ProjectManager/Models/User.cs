using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Mọi entity lưu trong JSON đều có khoá tự tăng.</summary>
    public interface IEntity
    {
        int Id { get; set; }
    }

    public static class Roles
    {
        /// <summary>Toàn quyền, bao gồm quản trị người dùng.</summary>
        public const string Admin = "Admin";

        /// <summary>Cập nhật dự án / thành viên / phân công, không quản trị người dùng.</summary>
        public const string Manager = "Manager";

        /// <summary>
        /// Chỉ báo cáo công việc của chính mình. Không vào được các màn quản trị; chỉ thấy màn
        /// tổng hợp và màn báo cáo cá nhân theo tuần.
        /// </summary>
        public const string Reporter = "Reporter";

        public static readonly string[] All = { Admin, Manager, Reporter };

        /// <summary>
        /// Nhóm quyền được cập nhật dữ liệu quản trị. Truyền vào
        /// <c>AppAuthorize(AllowedRoles = Roles.Management)</c> để chặn tài khoản báo cáo.
        /// </summary>
        public const string Management = Admin + "," + Manager;

        /// <summary>
        /// Một tài khoản có thể mang NHIỀU quyền, lưu chung một chuỗi ngăn bởi dấu phẩy
        /// (ví dụ "Manager,Reporter"). Tách thành danh sách để so sánh.
        /// Dữ liệu cũ chỉ có một quyền vẫn tách ra đúng một phần tử nên tương thích ngược.
        /// </summary>
        public static string[] Split(string roles)
        {
            if (string.IsNullOrWhiteSpace(roles)) return new string[0];

            return roles.Split(',')
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .ToArray();
        }

        /// <summary>
        /// Ghép danh sách mã nhóm thành chuỗi để lưu. Chấp nhận mọi mã nhóm (nhóm giờ cấu hình
        /// được), chỉ bỏ khoảng trắng và trùng lặp — việc kiểm mã nhóm có tồn tại để nơi gọi lo.
        /// </summary>
        public static string Join(IEnumerable<string> roles)
        {
            if (roles == null) return string.Empty;

            var valid = roles
                .Select(r => (r ?? string.Empty).Trim())
                .Where(r => r.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return string.Join(",", valid);
        }

        /// <summary>Tài khoản (có thể nhiều quyền) có mang quyền này không.</summary>
        public static bool Has(string roles, string role)
        {
            return Split(roles).Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Tên hiển thị của (các) nhóm quyền, tra theo tên nhóm trong CSDL. Nhóm nhiều thì
        /// nối bằng dấu phẩy. Không tìm thấy nhóm thì hiện luôn mã.
        /// </summary>
        public static string Display(string roles)
        {
            var codes = Split(roles);
            if (codes.Length == 0) return "Chưa phân nhóm";

            var names = codes.Select(code =>
            {
                var group = Data.Repository.RoleGroups.FirstOrDefault(
                    g => string.Equals(g.Code, code, StringComparison.OrdinalIgnoreCase));
                return group != null ? group.Name : code;
            });

            return string.Join(", ", names);
        }

        /// <summary>
        /// Có quyền nào cho vào các màn quản lý/quản trị không (bất kỳ chức năng nào ngoài
        /// Tổng hợp và Báo cáo của tôi). Dùng để điều hướng sau đăng nhập.
        /// </summary>
        public static bool CanManage(string roles)
        {
            var set = Permissions.ResolvePermissions(roles);
            if (set.Contains("*")) return true;
            return set.Any(c => !c.StartsWith("home.", StringComparison.OrdinalIgnoreCase)
                                && !c.StartsWith("myreports.", StringComparison.OrdinalIgnoreCase));
        }
    }

    public class User : IEntity
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
        /// Một hoặc nhiều quyền, ngăn bởi dấu phẩy (ví dụ "Manager,Reporter").
        /// Dùng <see cref="Roles.Has"/> / <see cref="Roles.Split"/> để so sánh, đừng so chuỗi thẳng.
        /// </summary>
        [Required]
        [Display(Name = "Phân quyền")]
        public string Role { get; set; }

        /// <summary>
        /// Nhân sự (bảng Members) mà tài khoản này đại diện. Bắt buộc với quyền Báo cáo công việc
        /// — dùng để biết tài khoản được báo cáo cho những phân công nào. 0 nghĩa là chưa gắn.
        /// </summary>
        [Display(Name = "Nhân sự tương ứng")]
        public int MemberId { get; set; }

        /// <summary>Định dạng: iterations.salt_base64.key_base64 (PBKDF2-SHA256).</summary>
        public string PasswordHash { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsAdmin
        {
            get { return Roles.Has(Role, Roles.Admin); }
        }
    }
}
