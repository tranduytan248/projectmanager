using System;
using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Một lượt ghi giờ công vào một đầu việc — "logtime".
    ///
    /// Một đầu việc ghi được NHIỀU lượt, vào NHIỀU ngày khác nhau: làm ba tiếng hôm nay, hai
    /// tiếng ngày mai thì là hai dòng riêng. Vì vậy giờ công không nằm trên chính đầu việc mà
    /// tách ra bảng này.
    ///
    /// Khác hẳn cách tính giờ cũ (suy ra từ khoảng ngày của đầu việc, xem KpiService.HoursOf):
    /// đây là giờ NGƯỜI LÀM TỰ KHAI, có ngày cụ thể, nên cộng lại mới ra bức tranh thật.
    /// </summary>
    public class WorkTimeLog : IEntity
    {
        public int Id { get; set; }

        /// <summary>Đầu việc được ghi giờ.</summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Người bỏ công. LUÔN là người đang đăng nhập lúc ghi — không cho chọn người khác,
        /// vì giờ công là căn cứ chấm KPI của chính họ.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>Tên người ghi lúc ghi, giữ lại để bảng còn đọc được nếu tài khoản bị xoá.</summary>
        public string UserName { get; set; }

        /// <summary>
        /// NGÀY bỏ công (không có giờ phút). Là mốc để xét trần 12 giờ mỗi ngày, nên phải cắt
        /// về đúng ngày khi lưu.
        /// </summary>
        [Display(Name = "Ngày làm")]
        public DateTime WorkDate { get; set; }

        /// <summary>
        /// Số giờ của lượt này. Cho phép nửa tiếng (0,5) nên dùng decimal thay vì int —
        /// SqlStore ánh xạ thành DECIMAL(18,2).
        /// </summary>
        [Display(Name = "Số giờ")]
        public decimal Hours { get; set; }

        /// <summary>Mô tả ngắn phần việc đã làm trong lượt này. Không bắt buộc.</summary>
        [Display(Name = "Nội dung đã làm")]
        [StringLength(500, ErrorMessage = "Nội dung tối đa 500 ký tự")]
        public string Note { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Các mốc chặn của việc ghi giờ. Đặt một chỗ để màn hình và máy chủ dùng chung.</summary>
    public static class TimeLogRules
    {
        /// <summary>
        /// Trần giờ của MỘT NGƯỜI trong MỘT NGÀY, cộng trên mọi đầu việc.
        ///
        /// Một người có thể xong nhiều việc trong ngày, nhưng tổng giờ khai của cả ngày không
        /// vượt quá mốc này — quá 12 tiếng một ngày thì con số không còn đáng tin.
        /// </summary>
        public const decimal MaxHoursPerDay = 12m;

        /// <summary>Số giờ quy đổi cho một ngày công, dùng để tính trần giờ của một đầu việc.</summary>
        public const decimal HoursPerWorkDay = 8m;

        /// <summary>Giờ nhỏ nhất của một lượt ghi — chặn ghi 0 giờ hoặc số âm.</summary>
        public const decimal MinHoursPerEntry = 0.25m;

        /// <summary>
        /// Trần giờ của MỘT ĐẦU VIỆC: (Hạn hoàn thành − Ngày bắt đầu) × 8.
        ///
        /// Trả null khi việc thiếu ngày bắt đầu hoặc hạn — lúc đó không có căn cứ tính trần, và
        /// theo quy định thì việc như vậy KHÔNG cho ghi giờ.
        ///
        /// Tính CẢ ngày đầu và ngày cuối: việc bắt đầu và kết thúc trong cùng một ngày vẫn được
        /// một ngày công, nếu không thì trần bằng 0 và không ghi nổi giờ nào.
        /// </summary>
        public static decimal? TaskCap(DateTime? startDate, DateTime? dueDate)
        {
            if (!startDate.HasValue || !dueDate.HasValue) return null;

            var days = (dueDate.Value.Date - startDate.Value.Date).Days + 1;
            if (days <= 0) return null;

            return days * HoursPerWorkDay;
        }
    }
}
