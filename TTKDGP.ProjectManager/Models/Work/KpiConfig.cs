using System;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Bộ tham số của công thức chấm KPI tháng — sửa được trên màn "Cấu hình KPI" thay vì phải
    /// vào Web.config trên máy chủ rồi khởi động lại ứng dụng.
    ///
    /// Bảng chỉ giữ ĐÚNG MỘT dòng (bản đang áp dụng). Sửa là ghi đè lên chính dòng đó chứ không
    /// thêm dòng mới: điểm KPI luôn được tính lại từ đầu mỗi lần bấm "Tính KPI", nên giữ nhiều
    /// phiên bản cấu hình cũng không dựng lại được điểm của tháng trước — chỉ thêm chỗ để nhầm.
    /// Muốn biết ai đổi lúc nào thì xem <see cref="UpdatedBy"/> và <see cref="UpdatedAt"/>.
    ///
    /// Giá trị mặc định của các thuộc tính ở đây KHÔNG phải mặc định của hệ thống — dòng mới
    /// dựng bằng <c>KpiConfigService.Defaults()</c>, nơi lấy số từ Web.config.
    /// </summary>
    public class KpiConfig : IEntity
    {
        public int Id { get; set; }

        // ---------- Trọng số các nhóm việc ----------

        /// <summary>Điểm tối đa nhóm công việc hỗ trợ.</summary>
        public decimal SupportMaxPoint { get; set; }

        /// <summary>
        /// Điểm nhóm công việc thực hiện khi dành đúng định mức giờ cho nó — xem
        /// <see cref="ExecuteHoursShare"/>. KHÔNG phải trần: làm nhiều giờ hơn thì điểm vượt lên.
        /// </summary>
        public decimal ExecuteMaxPoint { get; set; }

        /// <summary>
        /// Định mức giờ của nhóm thực hiện, tính theo PHẦN của giờ yêu cầu trong tháng
        /// (0,7 = 70%). Dành đúng bấy nhiêu giờ cho việc triển khai thì được
        /// <see cref="ExecuteMaxPoint"/> điểm; dành nhiều hơn thì điểm cao hơn.
        /// </summary>
        public decimal ExecuteHoursShare { get; set; }

        // ---------- Mức trừ khi báo cáo trễ ----------

        /// <summary>Số lần trễ được bỏ qua ở nhóm hỗ trợ (mặc định 1 — trễ 0–1 lần không trừ).</summary>
        public int SupportLateFreeCount { get; set; }

        /// <summary>Mức trừ nhóm hỗ trợ khi số lần trễ vượt mức miễn đúng một lần.</summary>
        public decimal SupportLate2Penalty { get; set; }

        /// <summary>Mức trừ nhóm hỗ trợ khi trễ nhiều hơn thế.</summary>
        public decimal SupportLateMorePenalty { get; set; }

        /// <summary>Mức trừ nhóm thực hiện cho MỖI lần báo cáo trễ.</summary>
        public decimal ExecuteLatePenalty { get; set; }

        // ---------- Ngày công ----------

        /// <summary>Số giờ của một ngày công.</summary>
        public decimal HoursPerDay { get; set; }

        /// <summary>Tính Thứ 7 vào ngày công chuẩn của tháng.</summary>
        public bool CountSaturday { get; set; }

        /// <summary>Tính Chủ nhật vào ngày công chuẩn của tháng.</summary>
        public bool CountSunday { get; set; }

        // ---------- Ngưỡng xếp loại ----------

        public decimal RankExcellent { get; set; }
        public decimal RankGood { get; set; }
        public decimal RankFair { get; set; }
        public decimal RankPass { get; set; }

        // ---------- Vết sửa ----------

        public DateTime UpdatedAt { get; set; }

        /// <summary>Họ tên người sửa gần nhất — giữ tên chứ không giữ Id để đọc thẳng trên màn hình.</summary>
        public string UpdatedBy { get; set; }
    }
}
