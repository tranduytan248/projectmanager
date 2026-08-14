using System;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Các loại thông báo cá nhân. Loại quyết định bấm vào thì đi tới màn nào.</summary>
    public static class NotificationTypes
    {
        /// <summary>Được thêm vào dự án — bấm vào mở "Dự án của tôi".</summary>
        public const string ProjectAdded = "VaoDuAn";

        /// <summary>Được rút khỏi dự án — bấm vào mở "Dự án của tôi".</summary>
        public const string ProjectRemoved = "RoiDuAn";

        /// <summary>Được nhắc tên (@) trong trao đổi — bấm vào mở checklist của dự án.</summary>
        public const string Mentioned = "DuocNhac";

        /// <summary>Công việc sắp đến hạn hoặc đã quá hạn — bấm vào mở checklist của dự án.</summary>
        public const string DueSoon = "SapDenHan";

        /// <summary>Được Quản lý Tổ giao việc riêng — bấm vào mở chi tiết công việc.</summary>
        public const string TaskAssigned = "GiaoViecRieng";

        /// <summary>
        /// Được giao một đầu việc THUỘC DỰ ÁN — bấm vào mở chi tiết công việc.
        ///
        /// Tách khỏi <see cref="TaskAssigned"/> vì hai loại nói hai chuyện khác nhau: việc riêng có
        /// điểm cộng ấn định lúc giao, còn việc dự án chấm theo bộ 30/70 và cần nêu tên dự án.
        /// Dùng chung một mẫu thì người nhận việc dự án đọc thấy "được giao việc riêng".
        /// </summary>
        public const string ProjectTaskAssigned = "GiaoViecDuAn";

        /// <summary>
        /// Có lượt trao đổi mới trong một công việc — báo cho người tạo việc và người đang được
        /// giao việc (nếu khác người vừa viết). Bấm vào mở trao đổi của công việc.
        /// </summary>
        public const string CommentAdded = "TraoDoiMoi";

        /// <summary>
        /// Một việc con trong todolist vừa được tick xong hoặc bỏ tick — báo cho người tạo việc
        /// và người đang được giao việc (nếu khác người vừa bấm). Bấm vào mở chi tiết công việc.
        /// </summary>
        public const string TodoToggled = "ViecConThayDoi";

        /// <summary>
        /// Một việc con MỚI vừa được thêm vào todolist — báo cho người tạo việc và người đang
        /// được giao việc (nếu khác người vừa thêm). Bấm vào mở chi tiết công việc.
        /// </summary>
        public const string TodoAdded = "ViecConMoi";
    }

    /// <summary>
    /// Một thông báo cá nhân, hiện ở chuông trên thanh đầu trang. Nội dung là câu hoàn chỉnh đã
    /// dựng sẵn lúc phát sinh sự kiện — người đọc không phải tra thêm gì; ProjectId/TaskId chỉ
    /// để dựng liên kết đích khi bấm vào.
    /// </summary>
    public class UserNotification : IEntity
    {
        public int Id { get; set; }

        /// <summary>Người nhận (bảng Users).</summary>
        public int UserId { get; set; }

        /// <summary>Loại thông báo, xem <see cref="NotificationTypes"/>.</summary>
        public string Type { get; set; }

        public string Message { get; set; }

        /// <summary>Dự án liên quan; 0 nếu không gắn dự án nào.</summary>
        public int ProjectId { get; set; }

        /// <summary>Đầu việc liên quan; 0 nếu không gắn đầu việc nào.</summary>
        public int TaskId { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
