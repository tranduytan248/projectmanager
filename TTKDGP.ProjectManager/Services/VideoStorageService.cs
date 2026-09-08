using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Dịch vụ quản lý và lưu trữ video tải lên từ trình soạn thảo mô tả công việc / dự án.
    /// Video được lưu vào App_Data/task_videos/{taskFolder}/ được phân quyền an toàn,
    /// giới hạn dung lượng tối đa 5MB và hỗ trợ các định dạng video chuẩn web: MP4, WebM, MOV, Ogg.
    /// </summary>
    public static class VideoStorageService
    {
        public const long MaxVideoBytes = 5 * 1024 * 1024; // 5 MB

        public static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".webm", ".mov", ".ogg"
        };

        /// <summary>
        /// Lưu file video vào thư mục App_Data/task_videos/{taskId}/ của máy chủ.
        /// </summary>
        /// <param name="inputStream">Luồng dữ liệu video tải lên</param>
        /// <param name="originalFileName">Tên file gốc</param>
        /// <param name="taskId">Mã công việc / dự án (nếu có, dùng để phân cấp thư mục theo task)</param>
        /// <returns>Đường dẫn URL an toàn để chèn vào thẻ video</returns>
        public static async Task<string> SaveVideoAsync(Stream inputStream, string originalFileName, int? taskId = null)
        {
            if (inputStream == null || inputStream.Length == 0)
            {
                throw new ArgumentException("Dữ liệu tệp video rỗng.", nameof(inputStream));
            }

            if (inputStream.Length > MaxVideoBytes)
            {
                throw new InvalidOperationException("Dung lượng video vượt quá giới hạn cho phép (tối đa 5MB).");
            }

            var ext = Path.GetExtension(originalFileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            {
                throw new InvalidOperationException("Định dạng video không được hỗ trợ. Chỉ chấp nhận các định dạng: MP4, WebM, MOV, OGG.");
            }

            var taskFolder = (taskId.HasValue && taskId.Value > 0) ? taskId.Value.ToString() : "temp";
            var physicalDir = GetPhysicalDir(taskFolder);
            var fileId = Guid.NewGuid().ToString("N");
            var fileName = fileId + ext;
            var physicalPath = Path.Combine(physicalDir, fileName);

            using (var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                await inputStream.CopyToAsync(fileStream);
            }

            return string.Format("/Upload/Video/{0}/{1}", taskFolder, fileName);
        }

        /// <summary>
        /// Lấy thư mục vật lý lưu video trong App_Data/task_videos/{taskFolder}/
        /// </summary>
        public static string GetPhysicalDir(string taskFolder)
        {
            if (string.IsNullOrWhiteSpace(taskFolder) || taskFolder.IndexOfAny(new[] { '/', '\\' }) >= 0 || taskFolder.Contains(".."))
            {
                taskFolder = "temp";
            }

            var relativeDir = "~/App_Data/task_videos/" + taskFolder;
            var physicalDir = HostingEnvironment.MapPath(relativeDir);
            if (string.IsNullOrWhiteSpace(physicalDir))
            {
                var asmDir = Path.GetDirectoryName(typeof(VideoStorageService).Assembly.Location) ?? "";
                var rootDir = asmDir.EndsWith("\\bin", StringComparison.OrdinalIgnoreCase)
                    ? Directory.GetParent(asmDir).FullName
                    : asmDir;
                physicalDir = Path.Combine(rootDir, "App_Data", "task_videos", taskFolder);
            }

            if (!Directory.Exists(physicalDir))
            {
                Directory.CreateDirectory(physicalDir);
            }

            return physicalDir;
        }

        /// <summary>
        /// Lấy đường dẫn vật lý của một video đã lưu trong App_Data để phục vụ phát video an toàn.
        /// </summary>
        public static string GetVideoPhysicalPath(string taskFolder, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(new[] { '/', '\\' }) >= 0 || fileName.Contains(".."))
            {
                return null;
            }

            var dir = GetPhysicalDir(taskFolder);
            var path = Path.Combine(dir, fileName);
            if (File.Exists(path)) return path;

            // Thử tìm trong thư mục temp nếu tìm theo taskId chưa thấy
            if (taskFolder != "temp")
            {
                var tempDir = GetPhysicalDir("temp");
                var tempPath = Path.Combine(tempDir, fileName);
                if (File.Exists(tempPath)) return tempPath;
            }

            return null;
        }

        /// <summary>
        /// Xác định MIME type tương ứng theo phần mở rộng file video.
        /// </summary>
        public static string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            switch (ext)
            {
                case ".mp4": return "video/mp4";
                case ".webm": return "video/webm";
                case ".mov": return "video/quicktime";
                case ".ogg": return "video/ogg";
                default: return "video/mp4";
            }
        }
    }
}
