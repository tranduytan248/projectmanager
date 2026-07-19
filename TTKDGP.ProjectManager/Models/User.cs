using System;
using System.ComponentModel.DataAnnotations;

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

        public static readonly string[] All = { Admin, Manager };
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

        [Required]
        [Display(Name = "Phân quyền")]
        public string Role { get; set; }

        /// <summary>Định dạng: iterations.salt_base64.key_base64 (PBKDF2-SHA256).</summary>
        public string PasswordHash { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsAdmin
        {
            get { return string.Equals(Role, Roles.Admin, StringComparison.OrdinalIgnoreCase); }
        }
    }
}
