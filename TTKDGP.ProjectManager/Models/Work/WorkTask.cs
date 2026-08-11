using System;
using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Ba loại đầu việc, quyết định cách tính điểm chất lượng.</summary>
    public static class TaskKinds
    {
        /// <summary>
        /// Việc triển khai theo checklist dự án — nguồn của 80% điểm chất lượng.
        /// Giá trị lưu vẫn là "Checklist" cho khớp dữ liệu đã có; nhãn hiển thị là "Triển khai".
        /// </summary>
        public const string Checklist = "Checklist";

        /// <summary>Việc hỗ trợ kỹ thuật, phân theo tuần — nguồn của 20%.</summary>
        public const string Support = "HoTro";

        /// <summary>Việc riêng Quản lý Tổ giao thẳng cho cá nhân, ngoài dự án — có điểm cộng KPI 0,2–1,5%.</summary>
        public const string Standalone = "NgoaiDuAn";

        public static readonly string[] All = { Checklist, Support, Standalone };

        /// <summary>Hai loại chọn được khi thêm việc trong một dự án.</summary>
        public static readonly string[] InProject = { Checklist, Support };

        public static string Display(string kind)
        {
            if (kind == Checklist) return "Triển khai";
            if (kind == Support) return "Hỗ trợ";
            if (kind == Standalone) return "Việc riêng";
            return kind;
        }
    }

    /// <summary>
    /// Độ ưu tiên của đầu việc. Nhận cả chữ lẫn số khi import vì file người dùng điền mỗi nơi
    /// một kiểu ("Cao", "cao", "1", "Thấp"...).
    /// </summary>
    public static class TaskPriorities
    {
        public const string High = "Cao";
        public const string Normal = "TrungBinh";
        public const string Low = "Thap";

        public static readonly string[] All = { High, Normal, Low };

        public static string Display(string priority)
        {
            if (priority == High) return "Cao";
            if (priority == Normal) return "Trung bình";
            if (priority == Low) return "Thấp";
            return "Trung bình";
        }

        /// <summary>Chuẩn hoá giá trị người dùng nhập. Không nhận ra thì coi là Trung bình.</summary>
        public static string Parse(string raw)
        {
            var s = (raw ?? string.Empty).Trim().ToLowerInvariant();
            if (s.Length == 0) return Normal;

            if (s == "1" || s.StartsWith("cao") || s.StartsWith("high") || s.StartsWith("gap")) return High;
            if (s == "3" || s.StartsWith("thap") || s.StartsWith("thấp") || s.StartsWith("low")) return Low;
            return Normal;
        }
    }

    public static class TaskStates
    {
        public const string NotStarted = "ChuaBatDau";
        public const string InProgress = "DangLam";
        public const string Paused = "TamDung";
        public const string Done = "HoanThanh";
        public const string Cancelled = "Huy";

        public static readonly string[] All = { NotStarted, InProgress, Paused, Done, Cancelled };

        public static string Display(string state)
        {
            if (state == NotStarted) return "Chưa bắt đầu";
            if (state == InProgress) return "Đang làm";
            if (state == Paused) return "Tạm dừng";
            if (state == Done) return "Hoàn thành";
            if (state == Cancelled) return "Huỷ";
            return state;
        }

        /// <summary>Việc đã đóng thì không còn phải theo dõi hạn nữa.</summary>
        public static bool IsClosed(string state)
        {
            return state == Done || state == Cancelled;
        }

        /// <summary>
        /// Việc KHÔNG còn chạy đồng hồ hạn: đã đóng, hoặc đang tạm dừng.
        ///
        /// Tạm dừng nghĩa là việc đang vướng ở đâu đó chờ gỡ — người thực hiện không làm được
        /// gì thêm, nên đếm tiếp số ngày quá hạn là bắt họ chịu quãng thời gian nằm ngoài tầm
        /// tay. Ngày tạm dừng vẫn giữ nguyên để khi chạy lại còn biết hạn cũ là bao giờ.
        /// </summary>
        public static bool IsClockStopped(string state)
        {
            return IsClosed(state) || state == Paused;
        }
    }

    /// <summary>
    /// Một đầu việc có thể giao và có thể chấm điểm.
    ///
    /// Gộp cả ba loại ở <see cref="TaskKinds"/> vào một bảng thay vì tách ba, vì màn "Việc của tôi"
    /// và bộ chấm KPI đều phải duyệt cả ba cùng lúc — tách ra thì mọi truy vấn đều phải ghép ba nguồn.
    /// </summary>
    public class WorkTask : IEntity
    {
        public int Id { get; set; }

        /// <summary>Loại việc, xem <see cref="TaskKinds"/>. Quyết định cách chấm điểm.</summary>
        [Display(Name = "Loại công việc")]
        public string Kind { get; set; }

        /// <summary>Dự án chứa đầu việc. 0 với việc ngoài dự án.</summary>
        [Display(Name = "Dự án")]
        public int ProjectId { get; set; }

        public string ProjectName { get; set; }

        /// <summary>Mã việc do người dùng đặt (ví dụ "CL-01"). Chỉ cần duy nhất trong một dự án.</summary>
        [Display(Name = "Mã việc")]
        [StringLength(50, ErrorMessage = "Mã việc tối đa 50 ký tự")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên công việc")]
        [Display(Name = "Tên công việc")]
        [StringLength(250, ErrorMessage = "Tên công việc tối đa 250 ký tự")]
        public string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        /// <summary>Mục cha để gom nhóm checklist nhiều cấp. 0 là mục gốc.</summary>
        public int ParentId { get; set; }

        public int SortOrder { get; set; }

        /// <summary>Người dùng thực hiện (bảng Users). 0 nghĩa là chưa giao.</summary>
        [Display(Name = "Người thực hiện")]
        public int AssigneeUserId { get; set; }

        public string AssigneeName { get; set; }

        public int AssignedByUserId { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime? StartDate { get; set; }

        /// <summary>Hạn hoàn thành. Bắt buộc với việc checklist và việc ngoài dự án.</summary>
        [Display(Name = "Hạn hoàn thành")]
        public DateTime? DueDate { get; set; }

        /// <summary>Mốc hoàn thành — căn cứ DUY NHẤT để chấm đúng/trễ hạn.</summary>
        public DateTime? CompletedAt { get; set; }

        [Display(Name = "Trạng thái")]
        public string State { get; set; }

        [Display(Name = "Tiến độ (%)")]
        [Range(0, 100, ErrorMessage = "Tiến độ từ 0 đến 100")]
        public int Progress { get; set; }

        /// <summary>Độ ưu tiên, xem <see cref="TaskPriorities"/>.</summary>
        [Display(Name = "Độ ưu tiên")]
        public string Priority { get; set; }

        /// <summary>
        /// Năm của tuần mà đầu việc thuộc về. 0 nghĩa là không gắn với tuần nào.
        /// Việc hỗ trợ BẮT BUỘC có; việc triển khai thì tuỳ, dùng để xếp việc theo tuần kế hoạch.
        /// </summary>
        [Display(Name = "Thuộc năm")]
        public int Year { get; set; }

        /// <summary>Tuần ISO mà đầu việc thuộc về. 0 nghĩa là không gắn với tuần nào.</summary>
        [Display(Name = "Thuộc tuần")]
        public int Week { get; set; }

        /// <summary>
        /// Điểm cộng KPI (%) khi hoàn thành đúng hạn — Quản lý Tổ chọn lúc giao việc riêng,
        /// từ 0,2 đến 1,5. Việc trong dự án luôn bằng 0.
        /// </summary>
        [Display(Name = "Điểm cộng KPI (%)")]
        public decimal BonusPercent { get; set; }

        // ---------- File đính kèm khi giao việc (một file) ----------

        /// <summary>Tên file lưu trên đĩa (ngẫu nhiên, giữ đuôi). NULL nghĩa là không đính kèm.</summary>
        public string AttachmentFile { get; set; }

        /// <summary>Tên gốc của file để hiển thị và đặt tên khi tải về.</summary>
        public string AttachmentName { get; set; }

        public long AttachmentSize { get; set; }

        public bool HasAttachment
        {
            get { return !string.IsNullOrEmpty(AttachmentFile); }
        }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        // ---------- Giá trị suy ra ----------

        /// <summary>
        /// Hoàn thành đúng hạn chưa. So theo NGÀY, bỏ qua giờ phút — xong lúc 23:50 ngày cuối vẫn
        /// tính đúng hạn.
        /// </summary>
        public bool IsOnTime
        {
            get
            {
                if (!CompletedAt.HasValue || !DueDate.HasValue) return false;
                return CompletedAt.Value.Date <= DueDate.Value.Date;
            }
        }

        /// <summary>
        /// Đã quá hạn mà chưa xong. Việc tạm dừng KHÔNG tính quá hạn — đồng hồ dừng theo
        /// <see cref="TaskStates.IsClockStopped"/>.
        /// </summary>
        public bool IsOverdue
        {
            get
            {
                if (TaskStates.IsClockStopped(State) || !DueDate.HasValue) return false;
                return DateTime.Today > DueDate.Value.Date;
            }
        }

        /// <summary>
        /// Sắp đến hạn: còn tối đa 1 ngày (hết hạn hôm nay hoặc ngày mai) mà việc chưa đóng.
        /// Dùng để tô màu cảnh báo cam trên thẻ Kanban và dòng lưới; quá hạn thì tô đỏ riêng.
        /// </summary>
        public bool IsDueSoon
        {
            get
            {
                if (TaskStates.IsClockStopped(State) || !DueDate.HasValue) return false;

                var left = (DueDate.Value.Date - DateTime.Today).TotalDays;
                return left >= 0 && left <= 1;
            }
        }

        /// <summary>Số ngày còn lại tới hạn; âm là đã quá hạn. Null khi không có hạn.</summary>
        public int? DaysLeft
        {
            get
            {
                if (!DueDate.HasValue) return null;
                return (int)(DueDate.Value.Date - DateTime.Today).TotalDays;
            }
        }

        /// <summary>Số ngày trễ hạn; 0 nếu đúng hạn hoặc chưa xong.</summary>
        public int LateDays
        {
            get
            {
                if (!CompletedAt.HasValue || !DueDate.HasValue) return 0;
                var days = (int)(CompletedAt.Value.Date - DueDate.Value.Date).TotalDays;
                return days > 0 ? days : 0;
            }
        }

        /// <summary>
        /// Số ngày thực hiện, tính cả ngày đầu và ngày cuối — dùng xếp bậc điểm cộng của việc
        /// ngoài dự án. Thiếu ngày bắt đầu thì coi như giao đúng ngày tạo.
        /// </summary>
        public int DurationDays
        {
            get
            {
                if (!DueDate.HasValue) return 0;

                var start = StartDate.HasValue ? StartDate.Value.Date : CreatedAt.Date;
                var days = (int)(DueDate.Value.Date - start).TotalDays + 1;
                return days < 1 ? 1 : days;
            }
        }

    }

    /// <summary>
    /// Một lượt trao đổi trong đầu việc — nơi người thực hiện và PM nói chuyện ngay tại chỗ giao việc.
    ///
    /// Nội dung là văn bản người dùng nhập rồi hiển thị lại cho người khác. Razor tự HTML-encode
    /// khi render bằng @, nhưng TUYỆT ĐỐI không được dùng Html.Raw với trường này.
    /// </summary>
    public class WorkComment : IEntity
    {
        public int Id { get; set; }

        public int TaskId { get; set; }

        /// <summary>Tài khoản người viết (bảng Users).</summary>
        public int UserId { get; set; }

        /// <summary>Tên người viết lúc viết — giữ để mạch hội thoại còn đọc được nếu tài khoản bị xoá.</summary>
        public string AuthorName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung trao đổi")]
        [StringLength(4000, ErrorMessage = "Nội dung tối đa 4000 ký tự")]
        public string Content { get; set; }

        // ---------- File đính kèm (mỗi lượt trao đổi tối đa một file) ----------

        /// <summary>Tên file lưu trên đĩa (ngẫu nhiên, giữ đuôi). NULL nghĩa là không đính kèm.</summary>
        public string AttachmentFile { get; set; }

        /// <summary>Tên gốc của file để hiển thị và đặt tên khi tải về.</summary>
        public string AttachmentName { get; set; }

        public long AttachmentSize { get; set; }

        public bool HasAttachment
        {
            get { return !string.IsNullOrEmpty(AttachmentFile); }
        }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Đã thu hồi. Không xoá cứng để các trả lời bên dưới không mất ngữ cảnh.</summary>
        public bool IsDeleted { get; set; }
    }
}
