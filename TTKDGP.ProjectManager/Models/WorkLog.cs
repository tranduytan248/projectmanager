using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Một dòng nhật ký công việc theo tuần của một phân công:
    /// tuần này thành viên đó làm gì trong dự án, trạng thái ra sao.
    /// Nhiều dòng nối lại thành timeline tiến trình thực hiện.
    /// </summary>
    public class WorkLog : IEntity
    {
        public int Id { get; set; }

        /// <summary>Phân công (thành viên × dự án) mà dòng nhật ký này thuộc về.</summary>
        public int AssignmentId { get; set; }

        // Lưu kèm để dữ liệu vẫn tra cứu được nếu phân công bị xoá về sau.
        public int ProjectId { get; set; }
        public int MemberId { get; set; }

        [Display(Name = "Năm")]
        [Range(2000, 2100, ErrorMessage = "Năm không hợp lệ")]
        public int Year { get; set; }

        [Display(Name = "Tuần")]
        [Range(1, 53, ErrorMessage = "Tuần phải từ 1 đến 53")]
        public int Week { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung công việc trong tuần")]
        [Display(Name = "Nội dung thực hiện")]
        [StringLength(2000, ErrorMessage = "Nội dung tối đa 2000 ký tự")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        [Display(Name = "Người ghi")]
        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>Form khai báo nhật ký một tuần.</summary>
    public class WorkLogEditViewModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Không xác định được phân công")]
        public int AssignmentId { get; set; }

        [Display(Name = "Năm")]
        [Range(2000, 2100, ErrorMessage = "Năm không hợp lệ")]
        public int Year { get; set; }

        [Display(Name = "Tuần")]
        [Range(1, 53, ErrorMessage = "Tuần phải từ 1 đến 53")]
        public int Week { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung công việc trong tuần")]
        [Display(Name = "Nội dung thực hiện")]
        [StringLength(2000, ErrorMessage = "Nội dung tối đa 2000 ký tự")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        // Ngữ cảnh hiển thị, không lưu
        public string ProjectName { get; set; }
        public string MemberName { get; set; }
        public string Role { get; set; }

        public bool IsNew { get { return Id == 0; } }
    }

    /// <summary>Một mốc trên timeline.</summary>
    public class TimelineEntry
    {
        public WorkLog Log { get; set; }

        /// <summary>Nhãn tuần kèm khoảng ngày, ví dụ "Tuần 30/2026 (20/07 – 26/07/2026)".</summary>
        public string WeekLabel { get; set; }

        /// <summary>Mốc mới nhất — được tô đậm trên timeline.</summary>
        public bool IsLatest { get; set; }

        /// <summary>Đúng tuần hiện tại.</summary>
        public bool IsCurrentWeek { get; set; }
    }

    /// <summary>Các mốc trong cùng một năm.</summary>
    public class TimelineYear
    {
        public int Year { get; set; }
        public List<TimelineEntry> Entries { get; set; }

        public TimelineYear()
        {
            Entries = new List<TimelineEntry>();
        }
    }

    /// <summary>Màn hình timeline tiến trình của một thành viên trên một dự án.</summary>
    public class TimelineViewModel
    {
        public int AssignmentId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectStatus { get; set; }
        public string Customer { get; set; }

        public int MemberId { get; set; }
        public string MemberName { get; set; }
        public string Role { get; set; }

        /// <summary>Trạng thái hiện tại của phân công, lấy theo mốc mới nhất.</summary>
        public string CurrentStatus { get; set; }
        public bool IsActive { get; set; }

        public List<TimelineYear> Years { get; set; }

        public int TotalEntries { get; set; }

        /// <summary>Tuần đã có nhật ký chưa — để nút "Ghi nhật ký tuần này" đổi chữ cho đúng.</summary>
        public bool HasCurrentWeekLog { get; set; }

        public TimelineViewModel()
        {
            Years = new List<TimelineYear>();
        }
    }
}
