using System;
using System.Collections.Generic;
using System.Linq;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Khối "Giờ công" trong hộp chi tiết công việc: danh sách các lượt đã ghi, các mốc trần
    /// và phần còn lại. Dựng một lần ở controller rồi trả cho partial vẽ — màn hình không tự
    /// tính lại con số nào, để những gì người dùng thấy đúng bằng cái máy chủ sẽ chặn.
    /// </summary>
    public class TaskTimeLogViewModel
    {
        public int TaskId { get; set; }

        /// <summary>Các lượt đã ghi, mới nhất lên trước.</summary>
        public List<WorkTimeLog> Logs { get; set; }

        /// <summary>Người đang xem có được ghi giờ vào việc này không.</summary>
        public bool CanLog { get; set; }

        /// <summary>Lý do không ghi được, để hiện thay cho ô nhập.</summary>
        public string BlockedReason { get; set; }

        /// <summary>Trần của đầu việc: (Hạn − Ngày bắt đầu) × 8. Null khi việc thiếu ngày.</summary>
        public decimal? TaskCap { get; set; }

        /// <summary>Tổng giờ đã ghi vào việc này, của mọi người.</summary>
        public decimal TaskTotal { get; set; }

        /// <summary>Giờ còn ghi được vào việc này. Null khi việc không có trần.</summary>
        public decimal? TaskRemaining { get; set; }

        /// <summary>Ngày mặc định điền sẵn ở ô nhập — hôm nay.</summary>
        public DateTime DefaultDate { get; set; }

        /// <summary>Giờ người đang xem đã khai trong ngày mặc định, trên mọi đầu việc.</summary>
        public decimal TodayTotal { get; set; }

        /// <summary>Giờ còn ghi được trong ngày mặc định trước khi chạm trần 12 giờ.</summary>
        public decimal TodayRemaining { get; set; }

        /// <summary>Trần mỗi ngày, đưa xuống để màn hình khỏi viết cứng con số.</summary>
        public decimal MaxPerDay { get; set; }

        public int CurrentUserId { get; set; }

        public TaskTimeLogViewModel()
        {
            Logs = new List<WorkTimeLog>();
        }

        public bool HasLogs { get { return Logs.Count > 0; } }

        /// <summary>Phần trăm giờ đã dùng so với trần của việc, để vẽ thanh tiến trình.</summary>
        public int UsedPercent
        {
            get
            {
                if (!TaskCap.HasValue || TaskCap.Value <= 0) return 0;

                var percent = (int)Math.Round(TaskTotal * 100 / TaskCap.Value);
                return percent > 100 ? 100 : percent;
            }
        }

        /// <summary>Đã ghi vượt trần của việc — chỉ xảy ra với dữ liệu cũ, cần báo đỏ.</summary>
        public bool IsOverCap
        {
            get { return TaskCap.HasValue && TaskTotal > TaskCap.Value; }
        }

        /// <summary>Giờ của riêng người đang xem trong việc này.</summary>
        public decimal MyTotal
        {
            get { return Logs.Where(l => l.UserId == CurrentUserId).Sum(l => l.Hours); }
        }
    }
}
