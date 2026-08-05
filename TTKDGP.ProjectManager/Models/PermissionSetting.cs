using System;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Phần CẤU HÌNH ĐƯỢC của một mã chức năng: tên hiển thị và bật/tắt.
    ///
    /// Danh mục chức năng vẫn nằm cứng trong <see cref="Permissions"/> — mỗi mã gắn với một
    /// endpoint có thật trong mã nguồn, nên không thể thêm mã mới từ màn hình: thêm một dòng ở
    /// đây mà không có <c>[AppAuthorize]</c> nào dùng tới thì nó chẳng chặn hay mở được gì.
    /// Bảng này chỉ ghi ĐÈ phần hiển thị và trạng thái bật/tắt lên danh mục đó.
    ///
    /// Mã nào chưa có dòng ở đây thì hiểu là đang BẬT và dùng tên gốc — nhờ vậy chức năng mới
    /// thêm vào mã nguồn tự chạy ngay, không phải chờ ai đó vào màn hình bật lên.
    /// </summary>
    public class PermissionSetting : IEntity
    {
        public int Id { get; set; }

        /// <summary>Mã chức năng đầy đủ, dạng "module.action" — ví dụ "kpi.view".</summary>
        public string Code { get; set; }

        /// <summary>
        /// Tên hiển thị thay cho tên gốc trong mã nguồn. Để trống thì dùng tên gốc.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Chức năng có đang mở không. Tắt thì KHÔNG AI dùng được, kể cả tài khoản toàn quyền
        /// ("*") — dùng để đóng hẳn một tính năng chưa tới lúc dùng, thay vì phải gỡ quyền khỏi
        /// từng nhóm rồi nhớ cấp lại sau.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>Ghi chú của quản trị viên, ví dụ lý do tắt.</summary>
        public string Note { get; set; }

        public DateTime UpdatedAt { get; set; }

        /// <summary>Họ tên người sửa gần nhất — giữ tên để đọc thẳng trên màn hình.</summary>
        public string UpdatedBy { get; set; }
    }
}
