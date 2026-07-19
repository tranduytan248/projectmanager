using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Dự án (nguồn: sheet "1. Dự án của Tổ NCPT").</summary>
    public class Project : IEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dự án")]
        [Display(Name = "Tên dự án")]
        [StringLength(250, ErrorMessage = "Tên dự án tối đa 250 ký tự")]
        public string Name { get; set; }

        [Display(Name = "PM phụ trách")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn PM phụ trách")]
        public int PmMemberId { get; set; }

        [Display(Name = "Khách hàng")]
        public string Customer { get; set; }

        [Display(Name = "Loại dự án")]
        public string ProjectType { get; set; }

        [Display(Name = "Trạng thái hiện tại")]
        public string CurrentStatus { get; set; }

        [Display(Name = "Trạng thái hợp đồng")]
        public string ContractStatus { get; set; }

        [Display(Name = "Doanh thu dự kiến")]
        public string ExpectedRevenue { get; set; }

        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        /// <summary>
        /// Nhân sự tham gia dự án, lưu theo Id thành viên.
        /// Đây là danh sách khai báo ở màn hình Dự án; bảng Phân công mới là nơi ghi chi tiết
        /// vai trò và công việc của từng người.
        /// </summary>
        [Display(Name = "Nhân sự tham gia")]
        public List<int> ParticipantIds { get; set; }

        public Project()
        {
            ParticipantIds = new List<int>();
        }

        [Display(Name = "Redmine / Google Sheet")]
        public string Redmine { get; set; }

        [Display(Name = "Github")]
        public string Github { get; set; }

        [Display(Name = "SVN")]
        public string Svn { get; set; }

        [Display(Name = "Báo cáo CV chung")]
        public string GeneralReport { get; set; }
    }
}
