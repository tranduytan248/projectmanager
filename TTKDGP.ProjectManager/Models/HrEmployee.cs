using System;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Thông tin nhân sự đồng bộ từ GoConnect về phần mềm nội bộ.
    ///
    /// Khoá nghiệp vụ là <see cref="Email"/>: mỗi lần đồng bộ, người đã có thì cập nhật lại
    /// thông tin, người chưa có thì thêm mới. Chọn email thay vì mã nhân viên vì email là thứ
    /// dùng để đối chiếu với tài khoản nội bộ; đã kiểm trên dữ liệu thật (780 người): không
    /// ai bỏ trống và không ai trùng.
    ///
    /// Đơn vị và chức danh không lưu tên ở đây mà tham chiếu sang HrWorkplaces/HrPositions
    /// qua <see cref="WorkplaceId"/> và <see cref="PositionId"/> — cùng một phòng ban xuất
    /// hiện ở hàng chục người, đổi tên một chỗ là xong.
    /// </summary>
    public class HrEmployee
    {
        /// <summary>Email công tác. Khoá nghiệp vụ để thêm/cập nhật.</summary>
        public string Email { get; set; }

        /// <summary>Mã nhân viên, ví dụ "VNPT005345".</summary>
        public string Code { get; set; }

        /// <summary>Họ tên đầy đủ.</summary>
        public string FullName { get; set; }

        public string PhoneNumber { get; set; }

        /// <summary>Giới tính (giữ nguyên chuỗi trả về từ GoConnect).</summary>
        public string Gender { get; set; }

        /// <summary>Định danh đơn vị sâu nhất của người này — phòng/tổ cụ thể.</summary>
        public string WorkplaceId { get; set; }

        /// <summary>Định danh chức danh.</summary>
        public string PositionId { get; set; }

        /// <summary>Định danh người dùng bên GoConnect (GUID), để đối chiếu về sau.</summary>
        public string UserId { get; set; }

        /// <summary>
        /// Định danh tài khoản bên GoConnect. API danh sách nhân sự chưa trả trường này
        /// (userIdentifiers luôn rỗng) nên hiện luôn để trống; giữ cột cho nguồn khác về sau.
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>Thời điểm bản ghi được cập nhật vào CSDL nội bộ.</summary>
        public DateTime UpdatedAt { get; set; }

        // ----- Chỉ để hiển thị: lấy từ bảng đơn vị/chức danh khi đọc, không lưu ở bảng nhân sự -----

        /// <summary>Tên đơn vị công tác, điền khi đọc bằng phép nối bảng.</summary>
        public string DepartmentName { get; set; }

        /// <summary>Tên chức danh, điền khi đọc bằng phép nối bảng.</summary>
        public string PositionName { get; set; }
    }

    /// <summary>
    /// Một đơn vị trong cây tổ chức của GoConnect. Cây có nhiều cấp, nối với nhau qua
    /// <see cref="WpParent"/>; <see cref="Level"/> đánh NGƯỢC với trực giác — 0 là cấp thấp
    /// nhất (phòng/tổ cụ thể), số càng lớn càng lên cao (4 = tập đoàn VNPT).
    /// </summary>
    public class HrWorkplace
    {
        public string WpId { get; set; }
        public string WpCode { get; set; }
        public string WpName { get; set; }

        /// <summary>Đơn vị cấp trên. Null/rỗng ở gốc cây.</summary>
        public string WpParent { get; set; }

        public int? WpOrder { get; set; }

        /// <summary>0 là cấp thấp nhất. Null nếu GoConnect không trả.</summary>
        public int? Level { get; set; }

        public DateTime UpdatedAt { get; set; }

        /// <summary>Số nhân sự thuộc đơn vị này — chỉ để hiển thị, tính khi đọc.</summary>
        public int EmployeeCount { get; set; }
    }

    /// <summary>Một chức danh / vị trí công việc trong GoConnect.</summary>
    public class HrPosition
    {
        public string PosId { get; set; }
        public string PosCode { get; set; }
        public string PosName { get; set; }
        public int? PosOrder { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>Số nhân sự đang giữ chức danh này — chỉ để hiển thị, tính khi đọc.</summary>
        public int EmployeeCount { get; set; }
    }
}
