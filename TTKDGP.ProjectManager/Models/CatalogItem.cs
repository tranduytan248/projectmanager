using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Một mục danh mục dùng chung (loại dự án, trạng thái dự án, trạng thái tham gia, vai trò).
    /// Các bản ghi nghiệp vụ tham chiếu tới danh mục bằng TÊN chứ không bằng khoá, nên khi đổi tên
    /// một mục thì phải đồng bộ lại mọi bản ghi đang mang tên cũ.
    /// </summary>
    public abstract class CatalogItem : IEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên")]
        [Display(Name = "Tên")]
        [StringLength(150, ErrorMessage = "Tên tối đa 150 ký tự")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string Description { get; set; }

        /// <summary>
        /// Độ ưu tiên — số nhỏ là ưu tiên cao. Hiện chỉ dùng cho trạng thái dự án, để quyết định
        /// dự án nào nổi lên đầu ở màn hình tổng hợp. Các danh mục khác để 0.
        /// </summary>
        [Display(Name = "Độ ưu tiên")]
        [Range(0, 999, ErrorMessage = "Độ ưu tiên từ 0 đến 999")]
        public int Priority { get; set; }

        [Display(Name = "Đang sử dụng")]
        public bool IsActive { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        public int SortOrder { get; set; }
    }

    /// <summary>Loại dự án — dùng cho cột "Loại dự án" của dự án.</summary>
    public class ProjectType : CatalogItem { }

    /// <summary>Trạng thái dự án — dùng cho cột "Trạng thái hiện tại" của dự án.</summary>
    public class ProjectStatus : CatalogItem { }

    /// <summary>Trạng thái tham gia — dùng cho trạng thái công việc trong bản ghi phân công.</summary>
    public class WorkStatus : CatalogItem { }

    /// <summary>Vai trò của thành viên trong dự án.</summary>
    public class MemberRole : CatalogItem { }

    /// <summary>Mô tả cách hiển thị và nơi sử dụng của một danh mục.</summary>
    public class CatalogConfig
    {
        /// <summary>Tên màn hình, ví dụ "Trạng thái dự án".</summary>
        public string Title { get; set; }

        /// <summary>Danh từ dùng trong câu thông báo, ví dụ "trạng thái dự án".</summary>
        public string ItemLabel { get; set; }

        /// <summary>Nhãn ô nhập tên, ví dụ "Tên trạng thái".</summary>
        public string NameLabel { get; set; }

        /// <summary>Tiêu đề cột đếm số bản ghi đang dùng, ví dụ "Số dự án".</summary>
        public string UsageHeader { get; set; }

        /// <summary>Câu mô tả ngắn dưới tiêu đề màn hình.</summary>
        public string Description { get; set; }

        /// <summary>Tên controller của màn hình này, để dựng link.</summary>
        public string ControllerName { get; set; }

        /// <summary>Chỉ danh mục trạng thái dự án mới hiện và cho sửa cột Độ ưu tiên.</summary>
        public bool ShowPriority { get; set; }
    }

    /// <summary>Dữ liệu cho màn hình danh sách danh mục.</summary>
    public class CatalogListViewModel
    {
        public CatalogConfig Config { get; set; }
        public List<CatalogItem> Items { get; set; }

        /// <summary>Số bản ghi nghiệp vụ đang dùng mỗi mục, khoá theo tên mục.</summary>
        public Dictionary<string, int> Usage { get; set; }

        public CatalogListViewModel()
        {
            Items = new List<CatalogItem>();
            Usage = new Dictionary<string, int>();
        }
    }

    /// <summary>Dữ liệu cho màn hình thêm/sửa một mục danh mục.</summary>
    public class CatalogEditViewModel
    {
        public CatalogConfig Config { get; set; }

        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên")]
        [Display(Name = "Tên")]
        [StringLength(150, ErrorMessage = "Tên tối đa 150 ký tự")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string Description { get; set; }

        [Display(Name = "Độ ưu tiên")]
        [Range(0, 999, ErrorMessage = "Độ ưu tiên từ 0 đến 999")]
        public int Priority { get; set; }

        [Display(Name = "Đang sử dụng")]
        public bool IsActive { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        public int SortOrder { get; set; }

        public bool IsNew { get { return Id == 0; } }
    }
}
