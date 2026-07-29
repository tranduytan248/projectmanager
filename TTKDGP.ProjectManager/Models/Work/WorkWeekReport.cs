using System;
using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Báo cáo tuần của PM cho một dự án, theo format chung ba phần: công việc đang thực hiện,
    /// khó khăn, tiến độ tuần tiếp theo.
    ///
    /// Khoá nghiệp vụ là (ProjectId, Year, Week). SqlStore không tạo được ràng buộc duy nhất nên
    /// phải tự kiểm trùng trước khi ghi.
    /// </summary>
    public class WorkWeekReport : IEntity
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        public int Year { get; set; }
        public int Week { get; set; }

        [Display(Name = "Công việc đang thực hiện")]
        [StringLength(4000, ErrorMessage = "Nội dung tối đa 4000 ký tự")]
        public string CurrentWork { get; set; }

        [Display(Name = "Khó khăn, vướng mắc")]
        [StringLength(4000, ErrorMessage = "Nội dung tối đa 4000 ký tự")]
        public string Difficulties { get; set; }

        [Display(Name = "Ghi chú cho tuần tiếp theo")]
        [StringLength(4000, ErrorMessage = "Nội dung tối đa 4000 ký tự")]
        public string NextWeekNote { get; set; }

        /// <summary>Thời điểm nộp. NULL nghĩa là còn là bản nháp.</summary>
        public DateTime? SubmittedAt { get; set; }

        public int SubmittedByUserId { get; set; }

        /// <summary>
        /// Nộp đúng hạn hay không. Đóng dấu MỘT LẦN ở lần nộp đầu tiên rồi chốt: nếu tính lại mỗi
        /// lần sửa thì PM nộp trễ chỉ cần mở ra sửa là tự xoá được vết trễ hạn.
        /// </summary>
        public bool IsOnTime { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        public bool IsSubmitted { get { return SubmittedAt.HasValue; } }
    }

    /// <summary>
    /// Một mục checklist được PM chọn vào kế hoạch tuần tiếp theo, kèm hạn cam kết.
    /// Hạn ở đây được ghi ngược lại vào chính đầu việc, để chỉ có MỘT nguồn sự thật về hạn.
    /// </summary>
    public class WorkPlanItem : IEntity
    {
        public int Id { get; set; }

        public int WeekReportId { get; set; }
        public int TaskId { get; set; }

        /// <summary>Tên mục lúc chọn — giữ để báo cáo cũ vẫn đọc được nếu mục bị xoá.</summary>
        public string TaskTitle { get; set; }

        [Display(Name = "Hạn cam kết")]
        public DateTime CommittedDueDate { get; set; }

        public string Note { get; set; }
    }
}
