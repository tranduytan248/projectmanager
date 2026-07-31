using System;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Xếp loại theo KPI cuối cùng (đã làm tròn về số nguyên).</summary>
    public static class KpiRanks
    {
        public const string Excellent = "Xuất sắc";
        public const string Good = "Tốt";
        public const string Fair = "Khá";
        public const string Pass = "Đạt";
        public const string Fail = "Chưa đạt";

        public static string Of(decimal finalPoint)
        {
            // Ngưỡng xếp theo điểm nguyên: 99,5 đã được làm tròn thành 100 trước khi vào đây.
            if (finalPoint >= 100) return Excellent;
            if (finalPoint >= 95) return Good;
            if (finalPoint >= 90) return Fair;
            if (finalPoint >= 80) return Pass;
            return Fail;
        }

        /// <summary>Lớp CSS của huy hiệu xếp loại, dùng chung cho danh sách và chi tiết.</summary>
        public static string BadgeClass(string rank)
        {
            if (rank == Excellent || rank == Good) return "badge badge-active";
            if (rank == Fail) return "badge badge-danger";
            return "badge";
        }
    }

    /// <summary>
    /// Kết quả KPI THÁNG của một người dùng — bộ chấm theo đặc tả mới: hỗ trợ tối đa 30%,
    /// thực hiện tối đa 70%, việc riêng cộng theo % từng việc, nhân tỷ lệ ngày công khi thiếu giờ.
    ///
    /// Mỗi người mỗi tháng đúng một dòng. Điểm là ảnh chụp tại lần tính gần nhất — bấm "Tính KPI"
    /// sẽ tính lại từ dữ liệu công việc hiện có; hai ô nhập tay (ngày nghỉ phép, giờ thực hiện)
    /// được GIỮ NGUYÊN qua các lần tính lại.
    /// </summary>
    public class KpiMonth : IEntity
    {
        public int Id { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        /// <summary>Người được chấm (Id bảng Users).</summary>
        public int UserId { get; set; }

        /// <summary>Họ tên lúc tính — giữ để bảng cũ vẫn đọc được nếu tài khoản đổi tên/bị xoá.</summary>
        public string UserFullName { get; set; }

        // ---------- I. Công việc hỗ trợ (tối đa 30%) ----------

        public int SupportTotal { get; set; }
        public int SupportDone { get; set; }

        /// <summary>Số việc hỗ trợ hoàn thành trễ hạn trong tháng.</summary>
        public int SupportLateCount { get; set; }

        public decimal SupportPoint { get; set; }

        // ---------- II. Công việc thực hiện (tối đa 70%) ----------

        public int ExecuteTotal { get; set; }
        public int ExecuteDone { get; set; }
        public int ExecuteLateCount { get; set; }
        public decimal ExecutePoint { get; set; }

        // ---------- III. Công việc được giao (việc riêng, cộng theo % từng việc) ----------

        public int AssignedTotal { get; set; }
        public int AssignedDone { get; set; }
        public decimal AssignedPoint { get; set; }

        /// <summary>KPI chất lượng = hỗ trợ + thực hiện + được giao.</summary>
        public decimal QualityPoint { get; set; }

        // ---------- Ngày công ----------

        /// <summary>Số ngày làm việc chuẩn của tháng (bỏ Thứ 7, Chủ nhật).</summary>
        public int StandardDays { get; set; }

        /// <summary>Số ngày nghỉ có phép — nhập tay, giữ nguyên khi tính lại.</summary>
        public decimal LeaveDays { get; set; }

        /// <summary>Tổng giờ yêu cầu sau khi trừ nghỉ phép = (ngày chuẩn − nghỉ phép) × 8.</summary>
        public decimal RequiredHours { get; set; }

        /// <summary>
        /// Tổng giờ thực hiện — hệ thống tự tính từ thời gian (ngày bắt đầu → hạn hoàn thành,
        /// bỏ Thứ 7/Chủ nhật) của các việc đã hoàn thành hoặc đang làm trong tháng.
        /// </summary>
        public decimal? WorkingHours { get; set; }

        /// <summary>Tỷ lệ ngày công (%), chặn trên 100.</summary>
        public decimal AttendanceRate { get; set; }

        /// <summary>KPI cuối cùng, ĐÃ làm tròn về số nguyên (&gt;=0,5 làm tròn lên).</summary>
        public decimal FinalPoint { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ---------- Giá trị suy ra ----------

        public string Rank { get { return KpiRanks.Of(FinalPoint); } }

        /// <summary>Giờ thực hiện đã tính, đọc gọn cho view (dòng cũ chưa tính lại thì 0).</summary>
        public decimal WorkedHours { get { return WorkingHours ?? 0; } }
    }
}
