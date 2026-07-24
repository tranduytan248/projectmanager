using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Nhân sự thuộc tổ NCPT (nguồn: sheet "4. Nhân sự NCPT").</summary>
    public class Member : IEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        [StringLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự")]
        public string FullName { get; set; }

        /// <summary>Nơi nhận mail nhắc báo cáo sáng thứ Sáu. Bỏ trống thì thành viên không nhận mail.</summary>
        [Display(Name = "Email")]
        [StringLength(120, ErrorMessage = "Email tối đa 120 ký tự")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Display(Name = "Đang làm việc")]
        public bool IsActive { get; set; }
    }
}
