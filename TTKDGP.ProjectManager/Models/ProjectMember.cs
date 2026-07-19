using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Phân công: một thành viên tham gia một dự án, với công việc và trạng thái tham gia.
    /// Nguồn: sheet "2. Nhân sự tham gia".
    /// </summary>
    public class ProjectMember : IEntity
    {
        public int Id { get; set; }

        [Display(Name = "Dự án")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn dự án")]
        public int ProjectId { get; set; }

        /// <summary>Tên dự án tại thời điểm import — dự phòng khi ProjectId chưa khớp.</summary>
        public string ProjectName { get; set; }

        [Display(Name = "Thành viên")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thành viên")]
        public int MemberId { get; set; }

        public string MemberName { get; set; }

        [Display(Name = "Vai trò")]
        public string Role { get; set; }

        [Display(Name = "Trạng thái công việc")]
        public string WorkStatus { get; set; }

        [Display(Name = "Nội dung công việc")]
        public string WorkContent { get; set; }

        [Display(Name = "Đang tham gia")]
        public bool IsActive { get; set; }
    }

    // Vai trò, trạng thái tham gia, trạng thái dự án và loại dự án đều là danh mục quản lý được
    // (xem CatalogItem.cs). Đọc qua Repository.ActiveNames(...) để chỉ có một nguồn dữ liệu duy nhất,
    // không để danh sách cứng trong mã nguồn.
}
