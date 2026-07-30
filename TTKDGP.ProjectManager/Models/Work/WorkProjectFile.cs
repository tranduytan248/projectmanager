using System;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Một file tài liệu đính kèm vào dự án (bộ quản lý công việc mới). File lưu trong
    /// App_Data/attachments với tên ngẫu nhiên như file trao đổi công việc; tên gốc giữ ở đây
    /// để hiển thị và đặt tên khi tải về. Mọi lượt tải đều qua action có kiểm quyền xem dự án.
    /// </summary>
    public class WorkProjectFile : IEntity
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        /// <summary>Tên file lưu trên đĩa (ngẫu nhiên, giữ đuôi).</summary>
        public string StoredName { get; set; }

        /// <summary>Tên gốc của file để hiển thị và đặt tên khi tải về.</summary>
        public string OriginalName { get; set; }

        public long Size { get; set; }

        /// <summary>Tài khoản người tải lên (Id bảng Users).</summary>
        public int UploadedByUserId { get; set; }

        /// <summary>Tên người tải lên lúc đó — giữ để còn đọc được nếu tài khoản bị xoá.</summary>
        public string UploadedByName { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
