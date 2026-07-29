using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Hosting;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Lưu và phục vụ file đính kèm của trao đổi công việc.
    ///
    /// File nằm trong App_Data/attachments với tên NGẪU NHIÊN (chỉ giữ lại đuôi); tên gốc lưu
    /// trong CSDL để hiển thị. App_Data không phục vụ thẳng qua web nên mọi lượt tải về đều phải
    /// đi qua action có kiểm quyền xem đầu việc — không đoán được đường dẫn file của người khác.
    /// </summary>
    public static class CommentAttachments
    {
        /// <summary>Trần dung lượng một file đính kèm. Đổi thì nhớ chỉnh maxRequestLength trong Web.config.</summary>
        public const int MaxBytes = 10 * 1024 * 1024;

        /// <summary>
        /// Các đuôi file được nhận: tài liệu, ảnh và file nén thông dụng. KHÔNG nhận file chạy
        /// được (exe/bat/js...) — máy người tải về là máy công vụ, không mở đường phát tán.
        /// </summary>
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".txt", ".csv", ".md", ".zip", ".rar", ".7z"
        };

        private static string Folder()
        {
            return HostingEnvironment.MapPath("~/App_Data/attachments");
        }

        /// <summary>
        /// Kiểm và lưu file đính kèm vào đĩa, ghi thông tin vào bản ghi trao đổi.
        /// Không chọn file nào cũng tính là hợp lệ. Trả false kèm lý do khi file bị từ chối.
        /// </summary>
        public static bool TrySave(HttpPostedFileBase file, WorkComment comment, out string error)
        {
            error = null;
            if (file == null || file.ContentLength <= 0) return true;

            if (file.ContentLength > MaxBytes)
            {
                error = "File đính kèm vượt quá 10 MB.";
                return false;
            }

            var name = Path.GetFileName(file.FileName ?? string.Empty);
            var ext = Path.GetExtension(name);

            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            {
                error = "Chỉ nhận tài liệu, ảnh hoặc file nén (pdf, docx, xlsx, pptx, ảnh, txt, csv, zip, rar, 7z...).";
                return false;
            }

            var folder = Folder();
            Directory.CreateDirectory(folder);

            var stored = Guid.NewGuid().ToString("N") + ext.ToLowerInvariant();
            file.SaveAs(Path.Combine(folder, stored));

            comment.AttachmentFile = stored;
            comment.AttachmentName = name;
            comment.AttachmentSize = file.ContentLength;
            return true;
        }

        /// <summary>Đường dẫn tuyệt đối của một file đã lưu; null nếu tên lạ hoặc file không còn.</summary>
        public static string FullPath(string storedName)
        {
            if (string.IsNullOrWhiteSpace(storedName)) return null;

            // Tên lưu do hệ thống sinh, nhưng vẫn chặn ký tự đường dẫn cho chắc.
            if (storedName.IndexOfAny(new[] { '/', '\\' }) >= 0 || storedName.Contains("..")) return null;

            var path = Path.Combine(Folder(), storedName);
            return File.Exists(path) ? path : null;
        }

        /// <summary>Xoá file trên đĩa; file không còn hoặc đang bị giữ thì bỏ qua êm.</summary>
        public static void Delete(string storedName)
        {
            try
            {
                var path = FullPath(storedName);
                if (path != null) File.Delete(path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>Nhãn dung lượng gọn để hiển thị: "356 KB", "2,4 MB".</summary>
        public static string SizeLabel(long bytes)
        {
            if (bytes >= 1024L * 1024L) return (bytes / 1024.0 / 1024.0).ToString("0.#") + " MB";
            if (bytes >= 1024L) return (bytes / 1024.0).ToString("0") + " KB";
            return bytes + " B";
        }
    }
}
