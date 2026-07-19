using System.Collections.Generic;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>Danh mục loại dự án — dùng cho cột "Loại dự án" của dự án.</summary>
    public class ProjectTypesController : CatalogControllerBase<ProjectType>
    {
        protected override JsonStore<ProjectType> Store { get { return Repository.ProjectTypes; } }

        protected override CatalogConfig Config
        {
            get
            {
                return new CatalogConfig
                {
                    Title = "Loại dự án",
                    ItemLabel = "loại dự án",
                    NameLabel = "Tên loại dự án",
                    UsageHeader = "Số dự án",
                    Description = "Danh mục dùng cho ô \"Loại dự án\" khi khai báo dự án.",
                    ControllerName = "ProjectTypes"
                };
            }
        }

        protected override Dictionary<string, int> Usage()
        {
            return Repository.CountUsage(Repository.Projects.All(), p => p.ProjectType);
        }

        protected override int RenameUsage(string oldName, string newName)
        {
            return Repository.RenameUsage(Repository.Projects,
                p => p.ProjectType, (p, v) => p.ProjectType = v, oldName, newName);
        }
    }

    /// <summary>Danh mục trạng thái dự án — dùng cho cột "Trạng thái hiện tại" của dự án.</summary>
    public class ProjectStatusesController : CatalogControllerBase<ProjectStatus>
    {
        protected override JsonStore<ProjectStatus> Store { get { return Repository.ProjectStatuses; } }

        protected override CatalogConfig Config
        {
            get
            {
                return new CatalogConfig
                {
                    Title = "Trạng thái dự án",
                    ItemLabel = "trạng thái dự án",
                    NameLabel = "Tên trạng thái",
                    UsageHeader = "Số dự án",
                    Description = "Danh mục dùng cho ô \"Trạng thái hiện tại\" của dự án. "
                                  + "Độ ưu tiên quyết định dự án nào nổi lên đầu ở màn hình tổng hợp.",
                    ControllerName = "ProjectStatuses",
                    ShowPriority = true,
                    ShowExcludeFlag = true,
                    ExcludeLabel = "Coi như đã dừng",
                    ExcludeHint = "Dự án ở trạng thái này coi như đã dừng, không bị nhắc báo cáo hàng tuần."
                };
            }
        }

        protected override Dictionary<string, int> Usage()
        {
            return Repository.CountUsage(Repository.Projects.All(), p => p.CurrentStatus);
        }

        protected override int RenameUsage(string oldName, string newName)
        {
            return Repository.RenameUsage(Repository.Projects,
                p => p.CurrentStatus, (p, v) => p.CurrentStatus = v, oldName, newName);
        }
    }

    /// <summary>Danh mục trạng thái tham gia — dùng cho trạng thái công việc trong phân công.</summary>
    public class WorkStatusesController : CatalogControllerBase<WorkStatus>
    {
        protected override JsonStore<WorkStatus> Store { get { return Repository.WorkStatuses; } }

        protected override CatalogConfig Config
        {
            get
            {
                return new CatalogConfig
                {
                    Title = "Trạng thái tham gia",
                    ItemLabel = "trạng thái tham gia",
                    NameLabel = "Tên trạng thái",
                    UsageHeader = "Số phân công",
                    Description = "Danh mục dùng cho trạng thái công việc của thành viên trong dự án. "
                                  + "Chỉ thành viên ở trạng thái cần báo cáo mới bị nhắc hàng tuần.",
                    ControllerName = "WorkStatuses",
                    ShowExcludeFlag = true,
                    ExcludeLabel = "Không cần báo cáo",
                    ExcludeHint = "Thành viên ở trạng thái này không phải ghi nhật ký tuần, "
                                  + "nên không bị nhắc. Thường chỉ để trống cho \"Đang thực hiện\"."
                };
            }
        }

        protected override Dictionary<string, int> Usage()
        {
            return Repository.CountUsage(Repository.Assignments.All(), a => a.WorkStatus);
        }

        protected override int RenameUsage(string oldName, string newName)
        {
            return Repository.RenameUsage(Repository.Assignments,
                a => a.WorkStatus, (a, v) => a.WorkStatus = v, oldName, newName);
        }
    }

    /// <summary>Danh mục vai trò của thành viên trong dự án.</summary>
    public class MemberRolesController : CatalogControllerBase<MemberRole>
    {
        protected override JsonStore<MemberRole> Store { get { return Repository.MemberRoles; } }

        protected override CatalogConfig Config
        {
            get
            {
                return new CatalogConfig
                {
                    Title = "Vai trò",
                    ItemLabel = "vai trò",
                    NameLabel = "Tên vai trò",
                    UsageHeader = "Số phân công",
                    Description = "Danh mục vai trò của thành viên khi tham gia dự án.",
                    ControllerName = "MemberRoles"
                };
            }
        }

        protected override Dictionary<string, int> Usage()
        {
            return Repository.CountUsage(Repository.Assignments.All(), a => a.Role);
        }

        protected override int RenameUsage(string oldName, string newName)
        {
            return Repository.RenameUsage(Repository.Assignments,
                a => a.Role, (a, v) => a.Role = v, oldName, newName);
        }
    }
}
