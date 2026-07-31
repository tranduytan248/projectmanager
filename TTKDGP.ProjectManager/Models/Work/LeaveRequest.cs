using System;
using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Loại nghỉ. Chỉ để thống kê và đối chiếu với bộ phận nhân sự — KHÔNG loại nào ảnh hưởng tới
    /// cách tính KPI, vì mọi ngày nghỉ đã duyệt đều làm giảm giờ yêu cầu như nhau.
    /// </summary>
    public static class LeaveKinds
    {
        public const string Annual = "PhepNam";
        public const string Unpaid = "KhongLuong";
        public const string Sick = "Om";
        public const string Other = "Khac";

        public static readonly string[] All = { Annual, Unpaid, Sick, Other };

        public static string Display(string kind)
        {
            if (kind == Annual) return "Phép năm";
            if (kind == Unpaid) return "Nghỉ không lương";
            if (kind == Sick) return "Nghỉ ốm";
            if (kind == Other) return "Khác";
            return kind;
        }
    }

    /// <summary>
    /// Trạng thái duyệt đơn.
    ///
    /// Cố định trong mã nguồn chứ không đưa ra danh mục: bộ tính KPI và bảng thống kê khối lượng
    /// đọc thẳng vào mã "DaDuyet" để biết ngày nghỉ nào được trừ vào giờ yêu cầu. Đưa ra danh mục
    /// sửa được thì chỉ cần đổi một chữ là toàn bộ phần tính giờ sai mà không báo lỗi.
    /// </summary>
    public static class LeaveStates
    {
        /// <summary>Đã gửi, đang chờ Quản lý Tổ duyệt.</summary>
        public const string Pending = "ChoDuyet";

        /// <summary>Đã duyệt — ĐÂY là trạng thái duy nhất được trừ vào giờ yêu cầu.</summary>
        public const string Approved = "DaDuyet";

        public const string Rejected = "TuChoi";

        /// <summary>Người đăng ký tự thu hồi khi chưa được duyệt.</summary>
        public const string Cancelled = "DaHuy";

        public static readonly string[] All = { Pending, Approved, Rejected, Cancelled };

        public static string Display(string state)
        {
            if (state == Pending) return "Chờ duyệt";
            if (state == Approved) return "Đã duyệt";
            if (state == Rejected) return "Từ chối";
            if (state == Cancelled) return "Đã huỷ";
            return state;
        }

        /// <summary>Lớp CSS của huy hiệu trạng thái, dùng chung cho màn đăng ký và màn duyệt.</summary>
        public static string BadgeClass(string state)
        {
            if (state == Approved) return "badge badge-active";
            if (state == Rejected) return "badge badge-danger";
            if (state == Cancelled) return "badge badge-inactive";
            return "badge";
        }

        /// <summary>Đơn còn sửa/thu hồi được không — chỉ khi chưa ai duyệt.</summary>
        public static bool IsEditable(string state)
        {
            return state == Pending;
        }
    }

    /// <summary>
    /// Một lần đăng ký nghỉ phép của cá nhân.
    ///
    /// Mỗi đơn là MỘT KHOẢNG NGÀY chứ không phải một ngày lẻ: nghỉ liền 3 ngày thì làm một đơn,
    /// đỡ phải duyệt ba lần. Số ngày tính được (<see cref="Days"/>) bỏ Thứ 7 và Chủ nhật cho khớp
    /// với cách tính ngày công chuẩn ở <see cref="Services.KpiService.StandardWorkingDays"/> —
    /// nghỉ vào cuối tuần thì vốn đã không phải ngày làm việc, trừ nữa là trừ hai lần.
    ///
    /// Nghỉ nửa ngày (một buổi) thì đặt <see cref="IsHalfDay"/> và khoảng ngày chỉ gồm đúng một
    /// ngày — lúc đó số ngày là 0,5.
    /// </summary>
    public class LeaveRequest : IEntity
    {
        public int Id { get; set; }

        /// <summary>Người xin nghỉ (Id bảng Users).</summary>
        public int UserId { get; set; }

        /// <summary>Họ tên lúc đăng ký — giữ để đơn cũ còn đọc được nếu tài khoản đổi tên/bị xoá.</summary>
        public string UserFullName { get; set; }

        [Display(Name = "Loại nghỉ")]
        public string Kind { get; set; }

        [Display(Name = "Từ ngày")]
        public DateTime FromDate { get; set; }

        /// <summary>
        /// Ngày cuối của đợt nghỉ, TÍNH CẢ ngày này. Nghỉ một ngày thì bằng <see cref="FromDate"/>.
        /// </summary>
        [Display(Name = "Đến ngày")]
        public DateTime ToDate { get; set; }

        /// <summary>
        /// Chỉ nghỉ một buổi. Chỉ có nghĩa khi đợt nghỉ gói trong đúng một ngày; nhiều ngày mà đánh
        /// dấu nửa ngày thì không rõ nửa ngày nào nên màn đăng ký chặn từ lúc nhập.
        /// </summary>
        [Display(Name = "Nghỉ nửa ngày (một buổi)")]
        public bool IsHalfDay { get; set; }

        /// <summary>Buổi nghỉ khi <see cref="IsHalfDay"/> — chỉ để hiển thị, không ảnh hưởng số ngày.</summary>
        [Display(Name = "Buổi")]
        public string HalfDaySession { get; set; }

        [Display(Name = "Lý do")]
        [StringLength(500, ErrorMessage = "Lý do tối đa 500 ký tự")]
        public string Reason { get; set; }

        /// <summary>Trạng thái duyệt, xem <see cref="LeaveStates"/>.</summary>
        public string State { get; set; }

        // ---------- Vết duyệt ----------

        public int ApprovedByUserId { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }

        /// <summary>Ý kiến của người duyệt — bắt buộc khi từ chối để người xin nghỉ biết lý do.</summary>
        [StringLength(500, ErrorMessage = "Ý kiến tối đa 500 ký tự")]
        public string ApproverNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ---------- Giá trị suy ra ----------

        /// <summary>
        /// Số ngày công của đợt nghỉ: đếm ngày làm việc trong khoảng (bỏ Thứ 7, Chủ nhật).
        /// Nghỉ nửa ngày thì 0,5.
        /// </summary>
        public decimal Days
        {
            get { return DaysWithin(FromDate, ToDate); }
        }

        /// <summary>
        /// Số ngày công của đợt nghỉ nằm TRONG một khoảng cho trước — dùng khi cộng phép theo
        /// tháng, vì một đơn có thể vắt qua hai tháng (nghỉ 28/02 đến 03/03).
        /// </summary>
        public decimal DaysInRange(DateTime from, DateTime to)
        {
            var start = FromDate.Date > from.Date ? FromDate.Date : from.Date;
            var end = ToDate.Date < to.Date ? ToDate.Date : to.Date;
            return DaysWithin(start, end);
        }

        private decimal DaysWithin(DateTime from, DateTime to)
        {
            if (from.Date > to.Date) return 0;

            var count = 0;
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) count++;
            }

            if (count == 0) return 0;

            // Nửa ngày chỉ áp cho đợt nghỉ gói trong một ngày, nên không cần chia theo từng ngày.
            return IsHalfDay ? 0.5m : count;
        }

        public bool IsApproved { get { return State == LeaveStates.Approved; } }

        /// <summary>Đợt nghỉ có chạm vào một khoảng ngày không — dùng để lọc theo tháng.</summary>
        public bool OverlapsRange(DateTime from, DateTime to)
        {
            return FromDate.Date <= to.Date && ToDate.Date >= from.Date;
        }
    }
}
