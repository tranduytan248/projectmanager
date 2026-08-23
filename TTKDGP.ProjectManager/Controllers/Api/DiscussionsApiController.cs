using System;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>
    /// API cho ứng dụng di động Flutter: tải lên file đính kèm (ảnh/video <=10MB) và lấy danh sách task gợi ý trong trao đổi dự án.
    /// </summary>
    [ApiAuthorize]
    public class DiscussionsApiController : BaseController
    {
        /// <summary>
        /// Tải lên file đính kèm (Ảnh, Video <=10MB, Tài liệu).
        /// </summary>
        [HttpPost]
        public ActionResult Upload(int projectId)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null)
            {
                return Json(new { success = false, error = "Dự án không tồn tại." });
            }

            var userId = CurrentUserId;
            var isMember = Can("wprojects.view") || Repository.WorkAssignments.All().Any(a => a.ProjectId == projectId && a.UserId == userId && a.IsActive);
            if (!isMember)
            {
                return Json(new { success = false, error = "Bạn không có quyền gửi tài liệu trong dự án này." });
            }

            if (Request.Files.Count == 0 || Request.Files[0] == null || Request.Files[0].ContentLength <= 0)
            {
                return Json(new { success = false, error = "Vui lòng chọn file hợp lệ." });
            }

            var file = Request.Files[0];
            if (file.ContentLength > CommentAttachments.MaxBytes)
            {
                return Json(new { success = false, error = "File/Video đính kèm vượt quá giới hạn tối đa 10 MB." });
            }

            string stored, name, error;
            long size;
            if (!CommentAttachments.TrySaveFile(file, out stored, out name, out size, out error))
            {
                return Json(new { success = false, error = error ?? "Không thể lưu file đính kèm." });
            }

            string fileType = "file";
            if (CommentAttachments.IsImage(name)) fileType = "image";
            else if (CommentAttachments.IsVideo(name)) fileType = "video";

            var fileUrl = "/Discussions/Attachment?id=" + stored + "&name=" + Uri.EscapeDataString(name);

            return Json(new
            {
                success = true,
                storedName = stored,
                originalName = name,
                fileSize = size,
                fileSizeLabel = CommentAttachments.SizeLabel(size),
                fileType = fileType,
                url = fileUrl
            });
        }

        /// <summary>
        /// Lấy danh sách công việc (Task) trong dự án để gợi ý khi gõ ký tự '/' trên mobile.
        /// </summary>
        [HttpGet]
        public ActionResult Tasks(int projectId)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null)
            {
                return Json(new { success = false, tasks = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            var userId = CurrentUserId;
            var isMember = Can("wprojects.view") || Repository.WorkAssignments.All().Any(a => a.ProjectId == projectId && a.UserId == userId && a.IsActive);
            if (!isMember)
            {
                return Json(new { success = false, tasks = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            var tasks = Repository.WorkTasks.All()
                .Where(t => t.ProjectId == projectId)
                .OrderByDescending(t => t.Id)
                .Select(t => new
                {
                    id = t.Id,
                    code = t.Code,
                    title = t.Title,
                    state = t.State,
                    assigneeName = t.AssigneeName,
                    priority = t.Priority
                })
                .ToList();

            return Json(new { success = true, tasks = tasks }, JsonRequestBehavior.AllowGet);
        }
    }
}
