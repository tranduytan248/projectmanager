using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Data
{
    /// <summary>
    /// Tạo ba nhóm quyền mặc định khi bảng RoleGroups còn trống. Mã nhóm giữ đúng
    /// Admin/Manager/Reporter để dữ liệu tài khoản hiện có (User.Role) khớp ngay.
    /// </summary>
    public static class RoleGroupSeeder
    {
        public static void EnsureDefaults()
        {
            try
            {
                if (Repository.RoleGroups.All().Any()) return;

                Repository.RoleGroups.Insert(new RoleGroup
                {
                    Code = Roles.Admin,
                    Name = "Quản trị",
                    Description = "Toàn quyền hệ thống, gồm cả cấu hình nhóm quyền và người dùng.",
                    Permissions = "*",
                    IsActive = true,
                    SortOrder = 1
                });

                Repository.RoleGroups.Insert(new RoleGroup
                {
                    Code = Roles.Manager,
                    Name = "Quản lý",
                    Description = "Cập nhật dự án, thành viên, phân công, báo cáo nhân sự và danh mục.",
                    Permissions = string.Join(",", Permissions.ManagerDefaults()),
                    IsActive = true,
                    SortOrder = 2
                });

                Repository.RoleGroups.Insert(new RoleGroup
                {
                    Code = Roles.Reporter,
                    Name = "Báo cáo công việc",
                    Description = "Chỉ xem màn Tổng hợp và báo cáo phần việc của chính mình.",
                    Permissions = string.Join(",", Permissions.ReporterDefaults()),
                    IsActive = true,
                    SortOrder = 3
                });
            }
            catch (Exception)
            {
                // Không để lỗi hạ tầng (SQL chập chờn) làm sập lúc khởi động; lần chạy sau seed lại.
            }
        }

        /// <summary>
        /// Cấp các chức năng của những màn hình MỚI cho hai nhóm gốc Quản lý / Báo cáo.
        ///
        /// Cần bước này vì <see cref="EnsureDefaults"/> chỉ chạy khi bảng còn trống: bản đã vận
        /// hành luôn có sẵn ba nhóm, nên thêm màn hình mới mà không làm gì thì không ai vào được
        /// màn đó, kể cả Quản lý.
        ///
        /// Chỉ cấp khi nhóm CHƯA có bất kỳ chức năng nào của module đó. Nhờ vậy quản trị viên gỡ
        /// bớt vài chức năng trong màn Nhóm quyền thì lần khởi động sau không bị cấp lại — nếu cấp
        /// vô điều kiện thì mọi tuỳ chỉnh của họ sẽ bị ghi đè mỗi lần khởi động.
        /// </summary>
        public static void EnsureNewModulePermissions()
        {
            try
            {
                var newModules = new[]
                {
                    Models.Permissions.Team,
                    Models.Permissions.WorkProjects,
                    Models.Permissions.WorkTasks,
                    Models.Permissions.WorkReports,
                    Models.Permissions.Kpi,
                    Models.Permissions.Leaves,
                    Models.Permissions.Workload
                };

                GrantMissingModules(Roles.Manager, newModules, Permissions.ManagerDefaults());
                GrantMissingModules(Roles.Reporter, newModules, Permissions.ReporterDefaults());
            }
            catch (Exception)
            {
                // Như trên: lỗi hạ tầng thì bỏ qua, lần khởi động sau làm lại.
            }
        }

        private static void GrantMissingModules(
            string roleCode, IEnumerable<PermModule> modules, IEnumerable<string> defaults)
        {
            var group = Repository.RoleGroups.FirstOrDefault(
                g => string.Equals(g.Code, roleCode, StringComparison.OrdinalIgnoreCase));

            // Nhóm toàn quyền không cần cấp thêm gì.
            if (group == null || group.Permissions == "*") return;

            var current = new HashSet<string>(
                (group.Permissions ?? string.Empty).Split(',')
                    .Select(p => p.Trim()).Where(p => p.Length > 0),
                StringComparer.OrdinalIgnoreCase);

            var wanted = new HashSet<string>(defaults, StringComparer.OrdinalIgnoreCase);
            var added = false;

            foreach (var module in modules)
            {
                var prefix = module.Code + ".";

                // Nhóm đã biết tới module này rồi thì để nguyên, kể cả khi chỉ còn vài chức năng.
                if (current.Any(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) continue;

                foreach (var code in wanted.Where(w => w.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    if (current.Add(code)) added = true;
                }
            }

            if (!added) return;

            group.Permissions = string.Join(",", current.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
            Repository.RoleGroups.Update(group);
        }
    }
}
