using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Bộ điều khiển tiếp nhận tải lên và phục vụ hình ảnh từ trình soạn thảo mô tả (copy-paste hoặc kéo thả).
    /// Ảnh được lưu vào App_Data/task_images/{taskId}/ được phân quyền an toàn, tự động nén kích thước
    /// để tối ưu hóa bộ nhớ máy chủ và cơ sở dữ liệu.
    /// </summary>
    public class UploadController : BaseController
    {
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp"
        };

        [HttpPost]
        public async Task<ActionResult> Image(HttpPostedFileBase file, int? taskId)
        {
            if (CurrentUser == null)
            {
                return Json(new { success = false, message = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại." });
            }

            if (file == null || file.ContentLength <= 0)
            {
                return Json(new { success = false, message = "Không tìm thấy dữ liệu tệp hình ảnh." });
            }

            // Giới hạn kích thước ảnh tối đa 10MB để chống spam tài nguyên
            if (file.ContentLength > 10 * 1024 * 1024)
            {
                return Json(new { success = false, message = "Kích thước ảnh không được vượt quá 10MB." });
            }

            var ext = Path.GetExtension(file.FileName);
            if (!string.IsNullOrEmpty(ext))
            {
                ext = ext.ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                {
                    return Json(new { success = false, message = "Định dạng tệp không được hỗ trợ. Chỉ chấp nhận các định dạng ảnh: PNG, JPG, JPEG, GIF, WEBP, BMP." });
                }
            }

            try
            {
                var url = await ImageStorageService.SaveAndOptimizeImageAsync(file.InputStream, file.FileName, file.ContentType, taskId);
                return Json(new { success = true, url = url });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải ảnh lên hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Phục vụ ảnh an toàn từ App_Data/task_images/{taskFolder}/{fileName}.
        /// Bắt buộc người dùng đã đăng nhập hệ thống để xem ảnh nội bộ.
        /// </summary>
        [HttpGet]
        public ActionResult ViewImage(string taskFolder, string fileName)
        {
            if (CurrentUser == null)
            {
                return new HttpStatusCodeResult(401, "Chưa đăng nhập.");
            }

            var physicalPath = ImageStorageService.GetImagePhysicalPath(taskFolder, fileName);
            if (physicalPath == null || !System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound();
            }

            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            var mime = "image/jpeg";
            if (ext == ".png") mime = "image/png";
            else if (ext == ".gif") mime = "image/gif";
            else if (ext == ".webp") mime = "image/webp";

            Response.Cache.SetCacheability(HttpCacheability.Private);
            Response.Cache.SetMaxAge(TimeSpan.FromDays(7));
            return File(physicalPath, mime);
        }

        /// <summary>
        /// Tiếp nhận tải lên video từ trình soạn thảo mô tả (giới hạn tối đa 5MB).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Video(HttpPostedFileBase file, int? taskId)
        {
            if (CurrentUser == null)
            {
                return Json(new { success = false, message = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại." });
            }

            if (file == null || file.ContentLength <= 0)
            {
                return Json(new { success = false, message = "Không tìm thấy dữ liệu tệp video." });
            }

            if (file.ContentLength > VideoStorageService.MaxVideoBytes)
            {
                return Json(new { success = false, message = "Kích thước video không được vượt quá 5MB." });
            }

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) || !VideoStorageService.AllowedExtensions.Contains(ext))
            {
                return Json(new { success = false, message = "Định dạng video không được hỗ trợ. Chỉ chấp nhận các định dạng: MP4, WebM, MOV, OGG." });
            }

            try
            {
                var url = await VideoStorageService.SaveVideoAsync(file.InputStream, file.FileName, taskId);
                return Json(new { success = true, url = url });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải video lên hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Phục vụ video an toàn từ App_Data/task_videos/{taskFolder}/{fileName}.
        /// Bắt buộc người dùng đã đăng nhập hệ thống để xem video nội bộ.
        /// </summary>
        [HttpGet]
        public ActionResult ViewVideo(string taskFolder, string fileName)
        {
            if (CurrentUser == null)
            {
                return new HttpStatusCodeResult(401, "Chưa đăng nhập.");
            }

            var physicalPath = VideoStorageService.GetVideoPhysicalPath(taskFolder, fileName);
            if (physicalPath == null || !System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound();
            }

            var mime = VideoStorageService.GetMimeType(fileName);
            Response.Cache.SetCacheability(HttpCacheability.Private);
            Response.Cache.SetMaxAge(TimeSpan.FromDays(7));
            return File(physicalPath, mime);
        }
    }
}
