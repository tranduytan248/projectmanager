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
            var canViewAll = Can("wprojects.view");

            // Lấy danh sách dự án người dùng tham gia hoặc toàn bộ nếu có quyền
            var userProjectIds = Repository.WorkAssignments.All()
                .Where(a => a.UserId == userId && a.IsActive)
                .Select(a => a.ProjectId)
                .Distinct()
                .ToList();

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
                    Name = p.Name,
                    Code = p.Code,
                    Customer = p.Customer,
                    IsActive = p.IsOpen,
                    MemberCount = Repository.WorkAssignments.All().Count(a => a.ProjectId == p.Id && a.IsActive)
                })
                .ToList();

            var selectedId = projectId.HasValue && projectId.Value > 0
                ? projectId.Value
                : (projects.Count > 0 ? projects[0].Id : 0);

            var selectedProject = selectedId > 0 ? Repository.WorkProjects.Find(selectedId) : null;

            var members = new List<ProjectMemberViewModel>();
            if (selectedProject != null)
            {
                var assignments = Repository.WorkAssignments.All()
                    .Where(a => a.ProjectId == selectedProject.Id && a.IsActive)
                    .ToList();

                var memberUserIds = assignments.Select(a => a.UserId).Distinct().ToList();
                var users = Repository.Users.All().Where(u => memberUserIds.Contains(u.Id)).ToList();

                members = (from a in assignments
                           join u in users on a.UserId equals u.Id
                           select new ProjectMemberViewModel
                           {
                               UserId = u.Id,
                               FullName = u.FullName,
                               Username = u.UserName,
                               RoleInProject = a.Role
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
                CurrentUserName = CurrentUser != null ? CurrentUser.Name : "",
                CurrentUserFullName = CurrentUser != null ? CurrentUser.FullName : ""
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
            var canViewAll = Can("wprojects.view");

            var userProjectIds = Repository.WorkAssignments.All()
                .Where(a => a.UserId == userId && a.IsActive)
                .Select(a => a.ProjectId)
                .Distinct()
                .ToList();

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
                    Name = p.Name,
                    Code = p.Code,
                    Customer = p.Customer,
                    IsActive = p.IsOpen,
                    MemberCount = Repository.WorkAssignments.All().Count(a => a.ProjectId == p.Id && a.IsActive)
                })
                .ToList();

            return PartialView("_Panel", projects);
        }
    }
}
