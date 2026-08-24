using System.Collections.Generic;

namespace TTKDGP.ProjectManager.Models
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

        public DiscussionHubViewModel()
        {
            Projects = new List<DiscussionProjectDto>();
            Members = new List<ProjectMemberViewModel>();
            CurrentUserName = "";
            CurrentUserFullName = "";
        }
    }

    public class ProjectMemberViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string RoleInProject { get; set; }

        public ProjectMemberViewModel()
        {
            FullName = "";
            Username = "";
            RoleInProject = "";
        }
    }
}
