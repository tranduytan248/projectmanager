using System;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Một đơn vị (phòng/ban/trung tâm/tổ) trong cây tổ chức của HRM (Odoo, hrm.vnpt.vn).
    ///
    /// API danh bạ chỉ trả một cặp (id, "Viễn thông Khánh Hòa / .../ Tên đơn vị") — id của
    /// đơn vị SÂU NHẤT mà nhân sự đó thuộc về, không có id riêng cho "Viễn thông Khánh Hòa"
    /// (đơn vị gốc, cấp Viễn thông tỉnh). Quy ước: gốc được gán cứng DepartmentId = 0,
    /// DepartmentParentId = null; các đơn vị khác suy cây bằng cách so khớp đường dẫn tên
    /// (đơn vị cha của "A / B / C" là "A / B") — xem <see cref="Data.HrmDirectorySync"/>.
    /// </summary>
    public class DepartmentHrm
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }

        /// <summary>Đơn vị cấp trên. Null ở gốc, hoặc khi không có ai trực thuộc trực tiếp
        /// đơn vị cha đó trong dữ liệu nên chưa suy ra được id.</summary>
        public int? DepartmentParentId { get; set; }

        /// <summary>
        /// Mã đơn vị, suy ra bằng cách khớp TÊN với <c>HrWorkplaces</c> (đơn vị đồng bộ từ
        /// GoConnect, đã có sẵn mã chuẩn) — xem <see cref="Data.HrmDirectorySync"/>. Null khi
        /// không tìm được đơn vị cùng tên bên GoConnect.
        /// </summary>
        public string DepartmentCode { get; set; }

        /// <summary>
        /// Mã đơn vị CHA, lấy từ đơn vị cha thật của đơn vị đã khớp bên GoConnect (đi theo
        /// <c>HrWorkplace.WpParent</c>), KHÔNG phải suy từ <see cref="DepartmentParentId"/> ở
        /// trên. Nhờ vậy vẫn ra được mã cha cả với những đơn vị mà cây nội bộ CAS chưa suy được
        /// cha (xem cảnh báo "chưa suy được đơn vị cha" khi đồng bộ).
        /// </summary>
        public string DepartmentParentCode { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Một chức danh (hr.job) trong HRM. Tên gốc từ API dạng "MÃ - Tên" (vd "CD_VNPT_08 -
    /// Giám đốc "); tách mã ra JobCode khi tách được, JobName giữ phần tên còn lại.
    /// </summary>
    public class JobHrm
    {
        public int JobId { get; set; }
        public string JobCode { get; set; }
        public string JobName { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Một nhân sự lấy từ API danh bạ HRM (model vnpt.hr.danhba.view). Khoá nghiệp vụ là
    /// <see cref="EmployeeId"/> — id bản ghi gốc bên Odoo, ổn định hơn mã nhân viên (một số
    /// cộng tác viên có thể thiếu mã).
    /// </summary>
    public class EmployeeHrm
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public string MobilePhone { get; set; }
        public string WorkEmail { get; set; }

        /// <summary>Tham chiếu tới <see cref="DepartmentHrm.DepartmentId"/>.</summary>
        public int? DepartmentId { get; set; }

        /// <summary>Mã đơn vị — chép lại từ <see cref="DepartmentHrm.DepartmentCode"/> của đơn vị
        /// mình lúc đồng bộ, xem <see cref="Data.HrmDirectorySync"/>. Null khi đơn vị chưa khớp
        /// được mã bên GoConnect.</summary>
        public string DepartmentCode { get; set; }

        /// <summary>Tham chiếu tới <see cref="JobHrm.JobId"/>.</summary>
        public int? JobId { get; set; }

        /// <summary>Vị trí công việc — khái niệm khác với chức danh (JobId), Odoo trả riêng.</summary>
        public int? ViTriCongViecId { get; set; }
        public string ViTriCongViecName { get; set; }
        public string ViTriCongViecCode { get; set; }

        public DateTime? Birthday { get; set; }

        /// <summary>Giữ nguyên chuỗi trả về từ HRM ("nam"/"nu").</summary>
        public string GioiTinh { get; set; }

        public bool IsCongTacVien { get; set; }

        public DateTime UpdatedAt { get; set; }

        /// <summary>Tên đơn vị, chỉ có khi đọc qua <see cref="Data.EmployeeHrmStore.Page"/> (JOIN
        /// sang department_hrm). Rỗng khi đọc qua đường khác.</summary>
        public string DepartmentName { get; set; }

        /// <summary>Tên chức danh, chỉ có khi đọc qua <see cref="Data.EmployeeHrmStore.Page"/>
        /// (JOIN sang job_hrm). Rỗng khi đọc qua đường khác.</summary>
        public string JobName { get; set; }

        /// <summary>Mã chức danh, chỉ có khi đọc qua <see cref="Data.EmployeeHrmStore.Page"/>
        /// (JOIN sang job_hrm). Rỗng khi đọc qua đường khác.</summary>
        public string JobCode { get; set; }
    }
}
