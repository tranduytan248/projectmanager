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
    }
}
