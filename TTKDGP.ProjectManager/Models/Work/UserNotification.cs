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
