using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Trung tâm trao đổi & thảo luận dự án thời gian thực qua Firebase Realtime Database trên nền tảng Web.
    /// </summary>
    [AppAuthorize]
    public class DiscussionsController : BaseController
    {
        public class DiscussionProjectDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string Customer { get; set; }
            public int MemberCount { get; set; }
            public bool IsActive { get; set; }
        }

        public class DiscussionHubViewModel
        {
            public List<DiscussionProjectDto> Projects { get; set; }
            public int SelectedProjectId { get; set; }
            public WorkProject SelectedProject { get; set; }
            public List<ProjectMemberViewModel> Members { get; set; }
            public int CurrentUserId { get; set; }
            public string CurrentUserName { get; set; }
            public string CurrentUserFullName { get; set; }
        }

        public class ProjectMemberViewModel
        {
            public int UserId { get; set; }
            public string FullName { get; set; }
            public string Username { get; set; }
            public string RoleInProject { get; set; }
        }

        /// <summary>
        /// Trang chính: Trao đổi Dự án (Khung chat thời gian thực của dự án được chọn).
        /// </summary>
        [HttpGet]
        public ActionResult Index(int? projectId)
        {
            var userId = CurrentUserId;
            var canViewAll = IsTeamManager || Can("wprojects.view");

            // Lấy danh sách dự án người dùng tham gia hoặc làm PM
            var userProjectIds = Repository.WorkAssignments.All()
                .Where(a => a.UserId == userId && a.IsActive)
                .Select(a => a.ProjectId)
                .Distinct()
                .ToList();

            var pmProjectIds = Repository.WorkProjects.All()
                .Where(p => p.PmUserId == userId)
                .Select(p => p.Id)
                .ToList();

            userProjectIds = userProjectIds.Union(pmProjectIds).Distinct().ToList();

            IEnumerable<WorkProject> query = Repository.WorkProjects.All();
            if (!canViewAll)
            {
                query = query.Where(p => userProjectIds.Contains(p.Id));
            }

            var projects = query
                .OrderBy(p => p.Name)
                .Select(p => new DiscussionProjectDto
                {
                    Id = p.Id,
                    Name = p.Name ?? "",
                    Code = p.Code ?? "",
                    Customer = p.Customer ?? "",
                    IsActive = p.IsOpen,
                    MemberCount = Repository.WorkAssignments.All().Count(a => a.ProjectId == p.Id && a.IsActive)
                })
                .ToList();

            var selectedId = projectId.HasValue && projectId.Value > 0
                ? projectId.Value
                : (projects.Count > 0 ? projects[0].Id : 0);

            if (!canViewAll && selectedId > 0 && !userProjectIds.Contains(selectedId))
            {
                selectedId = projects.Count > 0 ? projects[0].Id : 0;
            }

            var selectedProject = selectedId > 0 ? Repository.WorkProjects.Find(selectedId) : null;

            var members = new List<ProjectMemberViewModel>();
            if (selectedProject != null)
            {
                var assignments = Repository.WorkAssignments.All()
                    .Where(a => a.ProjectId == selectedProject.Id && a.IsActive)
                    .ToList();

                var memberUserIds = assignments.Select(a => a.UserId).Where(id => id > 0).Distinct().ToList();
                if (selectedProject.PmUserId > 0 && !memberUserIds.Contains(selectedProject.PmUserId))
                {
                    memberUserIds.Add(selectedProject.PmUserId);
                }

                var users = Repository.Users.All().Where(u => memberUserIds.Contains(u.Id)).ToList();

                members = users.Select(u =>
                {
                    var assign = assignments.FirstOrDefault(a => a.UserId == u.Id);
                    var roleInProj = assign != null ? (assign.Role ?? "") : (u.Id == selectedProject.PmUserId ? "PM" : "Thành viên");
                    return new ProjectMemberViewModel
                    {
                        UserId = u.Id,
                        FullName = u.FullName ?? u.UserName ?? "",
                        Username = u.UserName ?? "",
                        RoleInProject = roleInProj
                    };
                })
                .OrderBy(m => m.FullName)
                .ToList();
            }

            var vm = new DiscussionHubViewModel
            {
                Projects = projects,
                SelectedProjectId = selectedId,
                SelectedProject = selectedProject,
                Members = members,
                CurrentUserId = CurrentUserId,
                CurrentUserName = CurrentUser != null ? (CurrentUser.Name ?? "") : "",
                CurrentUserFullName = CurrentUser != null ? (CurrentUser.FullName ?? "") : ""
            };

            return View(vm);
        }

        /// <summary>
        /// Dropdown Panel danh sách các dự án để trao đổi (Nạp qua AJAX khi bấm icon Chat ở thanh Topbar).
        /// </summary>
        [HttpGet]
        public ActionResult Panel()
        {
            var userId = CurrentUserId;
            var canViewAll = IsTeamManager || Can("wprojects.view");

            var userProjectIds = Repository.WorkAssignments.All()
                .Where(a => a.UserId == userId && a.IsActive)
                .Select(a => a.ProjectId)
                .Distinct()
                .ToList();

            var pmProjectIds = Repository.WorkProjects.All()
                .Where(p => p.PmUserId == userId)
                .Select(p => p.Id)
                .ToList();

            userProjectIds = userProjectIds.Union(pmProjectIds).Distinct().ToList();

            IEnumerable<WorkProject> query = Repository.WorkProjects.All();
            if (!canViewAll)
            {
                query = query.Where(p => userProjectIds.Contains(p.Id));
            }

            var projects = query
                .OrderBy(p => p.Name)
                .Select(p => new DiscussionProjectDto
                {
                    Id = p.Id,
                    Name = p.Name ?? "",
                    Code = p.Code ?? "",
                    Customer = p.Customer ?? "",
                    IsActive = p.IsOpen,
                    MemberCount = Repository.WorkAssignments.All().Count(a => a.ProjectId == p.Id && a.IsActive)
                })
                .ToList();

            return PartialView("_Panel", projects);
        }

        /// <summary>
        /// Tải lên file / ảnh / video đính kèm cho phòng trao đổi dự án (Tối đa 10 MB).
        /// </summary>
        [HttpPost]
        public ActionResult UploadAttachment(int projectId, System.Web.HttpPostedFileBase file)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null)
            {
                return Json(new { success = false, error = "Dự án không tồn tại." });
            }

            var userId = CurrentUserId;
            var isMember = IsTeamManager || Can("wprojects.view") || project.PmUserId == userId ||
                           Repository.WorkAssignments.All().Any(a => a.ProjectId == projectId && a.UserId == userId && a.IsActive);
            if (!isMember)
            {
                return Json(new { success = false, error = "Bạn không có quyền gửi tài liệu trong dự án này." });
            }

            if (file == null || file.ContentLength <= 0)
            {
                return Json(new { success = false, error = "Vui lòng chọn file hợp lệ." });
            }

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

            return Json(new
            {
                success = true,
                storedName = stored,
                originalName = name,
                fileSize = size,
                fileSizeLabel = CommentAttachments.SizeLabel(size),
                fileType = fileType,
                url = Url.Action("Attachment", "Discussions", new { id = stored, name = name })
            });
        }

        /// <summary>
        /// Tải về hoặc xem inline file đính kèm của cuộc trao đổi.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Attachment(string id, string name)
        {
            var path = CommentAttachments.FullPath(id);
            if (path == null) return HttpNotFound();

            var fileName = string.IsNullOrWhiteSpace(name) ? System.IO.Path.GetFileName(path) : name;
            var mime = System.Web.MimeMapping.GetMimeMapping(fileName);

            // Cho phép stream inline ảnh và video trong thẻ img/video của browser
            if (CommentAttachments.IsImage(fileName) || CommentAttachments.IsVideo(fileName))
            {
                return File(path, mime);
            }

            return File(path, "application/octet-stream", fileName);
        }

        /// <summary>
        /// Lấy danh sách công việc (Task) trong dự án để gợi ý khi gõ ký tự '/'.
        /// </summary>
        [HttpGet]
        public ActionResult GetProjectTasks(int projectId)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null)
            {
                return Json(new { success = false, tasks = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            var userId = CurrentUserId;
            var isMember = IsTeamManager || Can("wprojects.view") || project.PmUserId == userId ||
                           Repository.WorkAssignments.All().Any(a => a.ProjectId == projectId && a.UserId == userId && a.IsActive);
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
                    priority = t.Priority,
                    url = Url.Action("Detail", "MyWork", new { id = t.Id })
                })
                .ToList();

            return Json(new { success = true, tasks = tasks }, JsonRequestBehavior.AllowGet);
        }
    }
}
