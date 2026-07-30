using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Giai đoạn của dự án. Dự án triển khai theo checklist, triển khai xong thì chuyển sang giai
    /// đoạn hỗ trợ; từ đó việc hỗ trợ phân theo tuần và phải báo cáo hằng tuần.
    ///
    /// Cả hai loại đầu việc vẫn dùng được ở mọi giai đoạn — giai đoạn chỉ nói việc nào đang là
    /// trọng tâm, không khoá màn hình nào cả.
    /// </summary>
    public static class ProjectPhases
    {
        public const string Implement = "TrienKhai";
        public const string Support = "HoTro";

        public static readonly string[] All = { Implement, Support };

        public static string Display(string phase)
        {
            if (phase == Implement) return "Triển khai";
            if (phase == Support) return "Hỗ trợ";
            return phase;
        }
    }

    /// <summary>
    /// Trạng thái dự án. MỖI GIAI ĐOẠN CÓ BỘ TRẠNG THÁI RIÊNG — trạng thái của giai đoạn triển
    /// khai không dùng cho giai đoạn hỗ trợ và ngược lại, trừ "Huỷ" là chung.
    ///
    /// Để cố định trong mã nguồn chứ không đưa ra danh mục, vì logic nghiệp vụ đọc thẳng vào các
    /// mã này (dự án còn mở không, có phải đang hỗ trợ không) — danh mục sửa được thì chỉ cần đổi
    /// một chữ là toàn bộ phần chấm chất lượng tính sai mà không báo lỗi.
    /// </summary>
    public static class ProjectStates
    {
        // ----- Giai đoạn Triển khai -----
        public const string Preparing = "ChuanBi";
        public const string Implementing = "DangThucHien";

        /// <summary>Đã làm xong phần lớn, đang chạy thử trước khi nghiệm thu.</summary>
        public const string Testing = "DangThuNghiem";

        public const string ImplementPaused = "TamDungTrienKhai";
        public const string Accepted = "DaNghiemThu";

        // ----- Giai đoạn Hỗ trợ -----
        public const string Supporting = "DangHoTro";
        public const string SupportPaused = "TamDungHoTro";
        public const string SupportEnded = "KetThucHoTro";

        /// <summary>Dùng chung cho cả hai giai đoạn.</summary>
        public const string Cancelled = "Huy";

        private static readonly string[] ImplementStates =
            { Preparing, Implementing, Testing, ImplementPaused, Accepted, Cancelled };

        private static readonly string[] SupportStates =
            { Supporting, SupportPaused, SupportEnded, Cancelled };

        public static readonly string[] All =
        {
            Preparing, Implementing, Testing, ImplementPaused, Accepted,
            Supporting, SupportPaused, SupportEnded, Cancelled
        };

        /// <summary>Các trạng thái hợp lệ của một giai đoạn.</summary>
        public static string[] ForPhase(string phase)
        {
            return phase == ProjectPhases.Support ? SupportStates : ImplementStates;
        }

        /// <summary>Trạng thái mặc định khi mới vào một giai đoạn.</summary>
        public static string DefaultFor(string phase)
        {
            return phase == ProjectPhases.Support ? Supporting : Preparing;
        }

        /// <summary>Trạng thái này có thuộc giai đoạn đó không — dùng để chặn dữ liệu lệch khi lưu.</summary>
        public static bool BelongsTo(string state, string phase)
        {
            return ForPhase(phase).Contains(state);
        }

        public static string Display(string state)
        {
            if (state == Preparing) return "Chuẩn bị";
            if (state == Implementing) return "Đang thực hiện";
            if (state == Testing) return "Đang thử nghiệm";
            if (state == ImplementPaused) return "Tạm dừng triển khai";
            if (state == Accepted) return "Đã nghiệm thu";
            if (state == Supporting) return "Đang hỗ trợ";
            if (state == SupportPaused) return "Tạm dừng hỗ trợ";
            if (state == SupportEnded) return "Kết thúc hỗ trợ";
            if (state == Cancelled) return "Huỷ";
            return state;
        }

        /// <summary>Dự án còn phải theo dõi tiến độ và chấm chất lượng không.</summary>
        public static bool IsOpen(string state)
        {
            return state != Cancelled && state != SupportEnded;
        }
    }

    /// <summary>
    /// Dự án trong bộ quản lý công việc.
    ///
    /// Khác bảng Projects cũ ở ba điểm quan trọng: có phân loại công việc, có mốc bắt đầu/kết thúc
    /// (để bộ chấm KPI biết tháng đó dự án còn hiệu lực không), và trạng thái là danh sách cố định
    /// thay vì danh mục tự do — vì logic nghiệp vụ phải dựa vào nó.
    /// </summary>
    public class WorkProject : IEntity
    {
        public int Id { get; set; }

        [Display(Name = "Mã dự án")]
        [StringLength(50, ErrorMessage = "Mã tối đa 50 ký tự")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dự án")]
        [Display(Name = "Tên dự án")]
        [StringLength(250, ErrorMessage = "Tên dự án tối đa 250 ký tự")]
        public string Name { get; set; }

        [Display(Name = "Khách hàng")]
        [StringLength(250)]
        public string Customer { get; set; }

        /// <summary>
        /// Loại dự án, lấy từ danh mục <see cref="Models.ProjectType"/> (màn Danh mục → Loại dự án)
        /// — ví dụ "Dự án khách hàng bên ngoài", "Dự án nội bộ". Lưu tên chứ không lưu Id, theo
        /// đúng nếp của các danh mục sẵn có, để đọc dữ liệu không cần nối bảng.
        ///
        /// Đây chỉ là phân loại, KHÔNG quyết định dự án có checklist hay có việc hỗ trợ — mọi dự
        /// án đều có cả hai.
        /// </summary>
        [Display(Name = "Loại dự án")]
        public string ProjectType { get; set; }

        /// <summary>Giai đoạn hiện tại, xem <see cref="ProjectPhases"/>.</summary>
        [Display(Name = "Giai đoạn")]
        public string Phase { get; set; }

        /// <summary>
        /// Trạng thái trong giai đoạn hiện tại, xem <see cref="ProjectStates"/>. Phải thuộc đúng
        /// bộ trạng thái của <see cref="Phase"/>.
        /// </summary>
        [Display(Name = "Trạng thái")]
        public string State { get; set; }

        /// <summary>
        /// PM ĐANG hiệu lực (Id bảng Users). Đây chỉ là bản sao cho tiện tra quyền và hiển thị
        /// danh sách — nguồn sự thật là dòng <see cref="WorkAssignment"/> có IsPm và chưa kết thúc.
        /// Mỗi lần thêm/kết thúc một dòng PM đều phải tính lại hai cột này. 0 nghĩa là hiện không có PM.
        /// </summary>
        [Display(Name = "PM phụ trách")]
        public int PmUserId { get; set; }

        public string PmName { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Ngày kết thúc dự kiến")]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Mô tả dạng HTML từ trình soạn thảo WYSIWYG. [AllowHtml] để MVC không chặn khi gửi form;
        /// nội dung LUÔN được lọc qua HtmlSanitizer trước khi lưu và trước khi hiển thị.
        /// </summary>
        [Display(Name = "Mô tả")]
        [AllowHtml]
        public string Description { get; set; }

        // ---------- Thông tin triển khai: nguồn mã, FTP, cơ sở dữ liệu ----------

        [Display(Name = "Link Source Github")]
        [StringLength(500)]
        public string GithubLink { get; set; }

        [Display(Name = "Link Source SVN")]
        [StringLength(500)]
        public string SvnLink { get; set; }

        [Display(Name = "FTP — Tài khoản")]
        [StringLength(250)]
        public string FtpAccount { get; set; }

        /// <summary>Hiển thị luôn che dạng ***; chỉ lấy được nội dung qua nút "Sao chép".</summary>
        [Display(Name = "FTP — Mật khẩu")]
        [StringLength(250)]
        public string FtpPassword { get; set; }

        [Display(Name = "Sử dụng database")]
        [StringLength(100)]
        public string DbType { get; set; }

        [Display(Name = "Database — Server")]
        [StringLength(250)]
        public string DbServer { get; set; }

        [Display(Name = "Database — Username")]
        [StringLength(250)]
        public string DbUsername { get; set; }

        /// <summary>Hiển thị luôn che dạng ***; chỉ lấy được nội dung qua nút "Sao chép".</summary>
        [Display(Name = "Database — Password")]
        [StringLength(250)]
        public string DbPassword { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsOpen { get { return ProjectStates.IsOpen(State); } }

        public bool IsSupportPhase { get { return Phase == ProjectPhases.Support; } }

        /// <summary>Dự án có hiệu lực trong khoảng ngày này không — dùng khi chấm KPI theo tháng.</summary>
        public bool OverlapsRange(DateTime from, DateTime to)
        {
            if (StartDate.HasValue && StartDate.Value.Date > to.Date) return false;
            if (EndDate.HasValue && EndDate.Value.Date < from.Date) return false;
            return true;
        }
    }

    /// <summary>Giai đoạn mà một người tham gia dự án.</summary>
    public static class AssignmentPhases
    {
        /// <summary>Tham gia lúc triển khai dự án theo checklist.</summary>
        public const string Implement = "TrienKhai";

        /// <summary>Chỉ tham gia hỗ trợ — kể cả sau khi dự án đã triển khai xong.</summary>
        public const string Support = "HoTro";

        /// <summary>Tham gia cả hai giai đoạn.</summary>
        public const string Both = "CaHai";

        public static readonly string[] All = { Both, Implement, Support };

        public static string Display(string phase)
        {
            if (phase == Implement) return "Triển khai";
            if (phase == Support) return "Hỗ trợ";
            if (phase == Both) return "Triển khai + Hỗ trợ";
            return phase;
        }
    }

    /// <summary>
    /// Một giai đoạn tham gia dự án của một người dùng.
    ///
    /// Cố ý KHÔNG mang "đang làm gì" như bảng phân công cũ — nội dung công việc nay thuộc về
    /// <see cref="WorkTask"/>. Bảng này chỉ trả lời đúng một câu: từ ngày nào đến ngày nào, ai ở
    /// trong dự án nào, với vai trò gì, và có phải PM trong khoảng đó không.
    ///
    /// Đây là NGUỒN SỰ THẬT về nhân sự dự án, kể cả về PM. PM đổi giữa chừng thì kết thúc dòng
    /// của người cũ và thêm dòng cho người mới — nhờ vậy tra được "tháng đó ai làm PM".
    /// <see cref="WorkProject.PmUserId"/> chỉ là bản sao của PM ĐANG hiệu lực, để tra quyền cho nhanh.
    ///
    /// Rút ra rồi quay lại cũng THÊM dòng mới, không sửa dòng cũ. Người tiếp tục hỗ trợ sau khi dự
    /// án triển khai xong thì để một dòng có Phase = HoTro, ngày rời bỏ trống.
    /// </summary>
    public class WorkAssignment : IEntity
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        /// <summary>Người tham gia (Id bảng Users).</summary>
        public int UserId { get; set; }

        /// <summary>Họ tên lúc phân công — bản sao để danh sách đọc được không cần nối bảng.</summary>
        public string UserFullName { get; set; }

        [Display(Name = "Vai trò trong dự án")]
        [StringLength(150)]
        public string Role { get; set; }

        /// <summary>
        /// Người này làm PM trong khoảng thời gian của dòng này. Mỗi dự án chỉ có MỘT dòng PM đang
        /// hiệu lực tại một thời điểm; đổi PM thì kết thúc dòng cũ rồi thêm dòng mới.
        /// </summary>
        [Display(Name = "Là PM giai đoạn này")]
        public bool IsPm { get; set; }

        /// <summary>Giai đoạn tham gia, xem <see cref="AssignmentPhases"/>.</summary>
        [Display(Name = "Giai đoạn tham gia")]
        public string Phase { get; set; }

        [Display(Name = "Tham gia từ")]
        public DateTime JoinedAt { get; set; }

        /// <summary>Ngày rời dự án. NULL nghĩa là vẫn đang tham gia — đây là nguồn sự thật duy nhất.</summary>
        [Display(Name = "Rời dự án ngày")]
        public DateTime? LeftAt { get; set; }

        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get { return !LeftAt.HasValue; } }

        /// <summary>Giai đoạn này có hiệu lực tại một ngày cụ thể không.</summary>
        public bool CoversDate(DateTime date)
        {
            if (date.Date < JoinedAt.Date) return false;
            return !LeftAt.HasValue || date.Date <= LeftAt.Value.Date;
        }

        /// <summary>Giai đoạn này có chồng lấn với một khoảng ngày không (một tuần, một tháng).</summary>
        public bool OverlapsRange(DateTime from, DateTime to)
        {
            if (JoinedAt.Date > to.Date) return false;
            return !LeftAt.HasValue || LeftAt.Value.Date >= from.Date;
        }
    }
}
