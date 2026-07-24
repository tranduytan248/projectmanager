using System;
using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Một hệ thống bên ngoài được phép tích hợp với phần mềm. Mỗi hệ thống có một khoá bí mật
    /// (<see cref="PrivateKey"/>) sinh ngẫu nhiên: khi trao đổi dữ liệu, hai bên cộng khoá này
    /// vào chuỗi dữ liệu rồi băm ra checksum để kiểm tính toàn vẹn và xác thực nguồn gửi.
    /// Khoá không nhập tay mà do hệ thống tạo; khi cần thì tạo lại chứ không sửa trực tiếp.
    /// </summary>
    public class IntegrationSystem : IEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã hệ thống")]
        [Display(Name = "Mã hệ thống")]
        [StringLength(50, ErrorMessage = "Mã tối đa 50 ký tự")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên hệ thống")]
        [Display(Name = "Tên hệ thống")]
        [StringLength(200, ErrorMessage = "Tên tối đa 200 ký tự")]
        public string Name { get; set; }

        /// <summary>
        /// Khoá bí mật sinh ngẫu nhiên (chuỗi hex). Dùng để cộng vào chuỗi dữ liệu rồi băm ra
        /// checksum khi tích hợp. Không hiển thị cho người ngoài; chỉ Admin xem được để cấp cho
        /// hệ thống đối tác.
        /// </summary>
        [Display(Name = "Khoá bí mật")]
        public string PrivateKey { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
