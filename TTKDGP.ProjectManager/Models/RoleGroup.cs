using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Nhóm quyền cấu hình được. Mỗi nhóm nắm một tập mã chức năng (dạng "module.action",
    /// ví dụ "projects.edit"), lưu chung một chuỗi ngăn bởi dấu phẩy. Nhóm mang dấu "*"
    /// nghĩa là toàn quyền — tự bao gồm cả module thêm về sau.
    ///
    /// Tài khoản trỏ tới nhóm qua <see cref="User.Role"/> (chuỗi Code nhóm ngăn phẩy).
    /// </summary>
    public class RoleGroup : IEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Mã nhóm, dùng để tài khoản tham chiếu tới. Ba nhóm mặc định giữ mã cũ
        /// (Admin/Manager/Reporter) để dữ liệu tài khoản hiện có khớp ngay.
        /// </summary>
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

        /// <summary>Các mã chức năng được cấp, ngăn bởi dấu phẩy. "*" nghĩa là toàn quyền.</summary>
        public string Permissions { get; set; }

        [Display(Name = "Đang dùng")]
        public bool IsActive { get; set; }

        [Display(Name = "Thứ tự")]
        public int SortOrder { get; set; }
    }
}
