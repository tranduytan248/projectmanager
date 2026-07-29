using System;
using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Trạng thái của một phiếu chấm KPI trong luồng duyệt hai cấp.</summary>
    public static class KpiStates
    {
        public const string Draft = "Nhap";
        public const string WaitingPm = "ChoPmDuyet";
        public const string PmApproved = "PmDaDuyet";
        public const string ManagerApproved = "ToTruongDaDuyet";
        public const string Sent = "DaGui";

        public static readonly string[] All = { Draft, WaitingPm, PmApproved, ManagerApproved, Sent };

        public static string Display(string state)
        {
            if (state == Draft) return "Nháp";
            if (state == WaitingPm) return "Chờ PM duyệt";
            if (state == PmApproved) return "PM đã duyệt";
            if (state == ManagerApproved) return "Quản lý Tổ đã duyệt";
            if (state == Sent) return "Đã gửi";
            return state;
        }

        /// <summary>Kỳ đã khoá thì không sinh lại và không sửa được nữa.</summary>
        public static bool IsLocked(string state)
        {
            return state == ManagerApproved || state == Sent;
        }
    }

    /// <summary>
    /// Phiếu chấm chất lượng công việc của một người dùng trong một tháng.
    /// Mỗi người mỗi kỳ đúng một phiếu, gộp điểm của mọi dự án.
    /// </summary>
    public class KpiSheet : IEntity
    {
        public int Id { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        /// <summary>Người được chấm (Id bảng Users).</summary>
        public int UserId { get; set; }

        /// <summary>Họ tên lúc chốt kỳ.</summary>
        public string UserFullName { get; set; }

        // ---------- Điểm hệ thống tự tính ----------

        /// <summary>Điểm thành phần checklist, 0-100. Null khi kỳ đó không có việc checklist nào.</summary>
        public decimal? ChecklistScore { get; set; }

        /// <summary>Điểm thành phần hỗ trợ, 0-100. Null khi kỳ đó không được phân việc hỗ trợ.</summary>
        public decimal? SupportScore { get; set; }

        /// <summary>Điểm nền sau khi áp trọng số 80/20 (hoặc lấy nguyên phần có việc).</summary>
        public decimal? BaseScore { get; set; }

        /// <summary>Cộng thêm từ việc ngoài dự án hoàn thành đúng hạn.</summary>
        public decimal ExtraBonus { get; set; }

        /// <summary>Cộng 2% cho PM làm đúng hạn cả ba đầu việc quản lý.</summary>
        public decimal PmBonus { get; set; }

        /// <summary>Tổng do hệ thống tính. Null nghĩa là kỳ này không có việc để chấm.</summary>
        public decimal? SystemTotal { get; set; }

        // ---------- Sau điều chỉnh ----------

        /// <summary>Điểm cuối cùng. Khởi tạo bằng SystemTotal; người duyệt sửa được.</summary>
        public decimal? FinalTotal { get; set; }

        /// <summary>Bắt buộc khi FinalTotal khác SystemTotal.</summary>
        [Display(Name = "Lý do điều chỉnh")]
        public string AdjustReason { get; set; }

        // ---------- Luồng duyệt ----------

        public string State { get; set; }

        [Display(Name = "Nhận xét của PM")]
        public string PmComment { get; set; }
        public int PmApprovedByUserId { get; set; }
        public DateTime? PmApprovedAt { get; set; }

        [Display(Name = "Nhận xét của Quản lý Tổ")]
        public string ManagerComment { get; set; }
        public int ManagerApprovedByUserId { get; set; }
        public DateTime? ManagerApprovedAt { get; set; }

        public DateTime? SentAt { get; set; }
        public string SentTo { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsLocked { get { return KpiStates.IsLocked(State); } }

        /// <summary>Kỳ này có việc để chấm không.</summary>
        public bool HasWork { get { return SystemTotal.HasValue; } }
    }

    /// <summary>
    /// Một dòng chi tiết trong phiếu chấm: một đầu việc đã đến hạn trong kỳ.
    ///
    /// CHỤP LẠI giá trị tại thời điểm chốt kỳ, không trỏ động sang bảng công việc — sửa đầu việc
    /// sau khi đã duyệt không được phép làm đổi bảng KPI đã chốt.
    /// </summary>
    public class KpiLine : IEntity
    {
        public int Id { get; set; }

        public int SheetId { get; set; }

        public int TaskId { get; set; }
        public string Kind { get; set; }

        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        /// <summary>Tên việc lúc chốt kỳ.</summary>
        public string Title { get; set; }

        /// <summary>Nội dung người duyệt sửa lại cho báo cáo. Rỗng nghĩa là giữ nguyên Title.</summary>
        [Display(Name = "Nội dung điều chỉnh")]
        public string AdjustedTitle { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        /// <summary>Số ngày thực hiện, dùng xếp bậc điểm cộng.</summary>
        public int DurationDays { get; set; }

        public bool IsOnTime { get; set; }

        /// <summary>Điểm dòng này đóng góp vào tổng.</summary>
        public decimal Score { get; set; }

        public string Note { get; set; }

        /// <summary>Nội dung hiển thị: ưu tiên bản đã điều chỉnh.</summary>
        public string DisplayTitle
        {
            get { return string.IsNullOrWhiteSpace(AdjustedTitle) ? Title : AdjustedTitle; }
        }
    }
}
