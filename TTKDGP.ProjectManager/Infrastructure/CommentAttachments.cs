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
            ".mp4", ".mov", ".webm", ".m4v",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".txt", ".csv", ".md", ".zip", ".rar", ".7z"
        };

        public static bool IsImage(string fileNameOrExt)
        {
            if (string.IsNullOrWhiteSpace(fileNameOrExt)) return false;
            var ext = Path.GetExtension(fileNameOrExt);
            if (string.IsNullOrEmpty(ext)) ext = fileNameOrExt;
            return ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsVideo(string fileNameOrExt)
        {
            if (string.IsNullOrWhiteSpace(fileNameOrExt)) return false;
            var ext = Path.GetExtension(fileNameOrExt);
            if (string.IsNullOrEmpty(ext)) ext = fileNameOrExt;
            return ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".mov", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".webm", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".m4v", StringComparison.OrdinalIgnoreCase);
        }

        private static string RootFolder()
        {
            var appData = HostingEnvironment.MapPath("~/App_Data/attachments");
            if (string.IsNullOrEmpty(appData))
            {
                appData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "attachments");
            }
            return appData;
        }

        private static string Folder(int taskId = 0)
        {
            var root = RootFolder();
            if (taskId > 0)
            {
                return Path.Combine(root, taskId.ToString());
            }
            return root;
        }

        /// <summary>
        /// Kiểm và lưu một file vào kho đính kèm theo thư mục taskId. Không chọn file nào cũng tính là hợp lệ.
        /// </summary>
        public static bool TrySaveFile(HttpPostedFileBase file, int taskId,
            out string storedName, out string originalName, out long size, out string error)
        {
            storedName = null;
            originalName = null;
            size = 0;
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

            var folder = Folder(taskId);
            Directory.CreateDirectory(folder);

            var stored = Guid.NewGuid().ToString("N") + ext.ToLowerInvariant();
            file.SaveAs(Path.Combine(folder, stored));

            storedName = stored;
            originalName = name;
            size = file.ContentLength;
            return true;
        }

        public static bool TrySaveFile(HttpPostedFileBase file,
            out string storedName, out string originalName, out long size, out string error)
        {
            return TrySaveFile(file, 0, out storedName, out originalName, out size, out error);
        }

        /// <summary>
        /// Lưu danh sách nhiều file đính kèm của một lượt trao đổi.
        /// </summary>
        public static bool TrySaveFiles(IEnumerable<HttpPostedFileBase> files, int taskId,
            out List<CommentAttachmentItem> savedItems, out string error)
        {
            savedItems = new List<CommentAttachmentItem>();
            error = null;

            if (files == null) return true;

            foreach (var file in files)
            {
                if (file == null || file.ContentLength <= 0) continue;

                string stored, name, fileErr;
                long size;
                if (!TrySaveFile(file, taskId, out stored, out name, out size, out fileErr))
                {
                    // Rollback các file đã lưu trước đó nếu có lỗi
                    foreach (var item in savedItems)
                    {
                        Delete(item.StoredName, taskId);
                    }
                    savedItems.Clear();
                    error = fileErr;
                    return false;
                }

                if (stored != null)
                {
                    savedItems.Add(new CommentAttachmentItem
                    {
                        StoredName = stored,
                        OriginalName = name,
                        Size = size
                    });
                }
            }

            return true;
        }

        /// <summary>Lưu danh sách file đính kèm của một lượt trao đổi.</summary>
        public static bool TrySave(IEnumerable<HttpPostedFileBase> files, WorkComment comment, out string error)
        {
            List<CommentAttachmentItem> items;
            if (!TrySaveFiles(files, comment.TaskId, out items, out error)) return false;

            if (items != null && items.Count > 0)
            {
                comment.Attachments = items;
                // Giữ trường cũ cho tương thích ngược 100%
                comment.AttachmentFile = items[0].StoredName;
                comment.AttachmentName = items[0].OriginalName;
                comment.AttachmentSize = items[0].Size;
            }
            return true;
        }

        /// <summary>Lưu một file đính kèm của một lượt trao đổi (tương thích ngược).</summary>
        public static bool TrySave(HttpPostedFileBase file, WorkComment comment, out string error)
        {
            if (file == null || file.ContentLength <= 0)
            {
                error = null;
                return true;
            }
            return TrySave(new[] { file }, comment, out error);
        }

        /// <summary>Lưu file đính kèm khi giao một đầu việc. Thay file cũ thì file cũ bị xoá.</summary>
        public static bool TrySave(HttpPostedFileBase file, WorkTask task, out string error)
        {
            string stored, name;
            long size;
            if (!TrySaveFile(file, task.Id, out stored, out name, out size, out error)) return false;

            if (stored != null)
            {
                if (task.HasAttachment) Delete(task.AttachmentFile, task.Id);
                task.AttachmentFile = stored;
                task.AttachmentName = name;
                task.AttachmentSize = size;
            }
            return true;
        }

        /// <summary>Đường dẫn tuyệt đối của một file đã lưu; null nếu tên lạ hoặc file không còn.</summary>
        public static string FullPath(string storedName, int taskId = 0)
        {
            if (string.IsNullOrWhiteSpace(storedName)) return null;

            // Tên lưu do hệ thống sinh, nhưng vẫn chặn ký tự đường dẫn cho chắc.
            if (storedName.IndexOfAny(new[] { '/', '\\' }) >= 0 || storedName.Contains("..")) return null;

            // 1. Kiểm tra trong thư mục theo taskId nếu có
            if (taskId > 0)
            {
                var path = Path.Combine(Folder(taskId), storedName);
                if (File.Exists(path)) return path;
            }

            // 2. Kiểm tra trong thư mục gốc attachments (dữ liệu cũ)
            var rootPath = Path.Combine(Folder(0), storedName);
            if (File.Exists(rootPath)) return rootPath;

            // 3. Quét đệ quy tìm file trong các thư mục con của attachments (phòng trường hợp taskId không khớp)
            try
            {
                var root = RootFolder();
                if (Directory.Exists(root))
                {
                    var files = Directory.GetFiles(root, storedName, SearchOption.AllDirectories);
                    if (files.Length > 0 && File.Exists(files[0])) return files[0];
                }
            }
            catch { }

            return null;
        }

        /// <summary>Đường dẫn tuyệt đối của một file đã lưu (tương thích chữ ký cũ).</summary>
        public static string FullPath(string storedName)
        {
            return FullPath(storedName, 0);
        }

        /// <summary>Xoá file trên đĩa; file không còn hoặc đang bị giữ thì bỏ qua êm.</summary>
        public static void Delete(string storedName, int taskId = 0)
        {
            try
            {
                var path = FullPath(storedName, taskId);
                if (path != null) File.Delete(path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public static void Delete(string storedName)
        {
            Delete(storedName, 0);
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
