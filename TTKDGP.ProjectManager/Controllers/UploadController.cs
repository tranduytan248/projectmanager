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
    /// Bộ điều khiển tiếp nhận tải lên hình ảnh từ trình soạn thảo mô tả (copy-paste hoặc kéo thả).
    /// Ảnh được tự động chuyển lên Firebase Storage để tối ưu bộ nhớ máy chủ và cơ sở dữ liệu.
    /// </summary>
    public class UploadController : BaseController
    {
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp"
        };

        [HttpPost]
        public async Task<ActionResult> Image(HttpPostedFileBase file)
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
                var url = await ImageStorageService.SaveAndOptimizeImageAsync(file.InputStream, file.FileName, file.ContentType);
                return Json(new { success = true, url = url });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải ảnh lên hệ thống: " + ex.Message });
            }
        }
    }
}
