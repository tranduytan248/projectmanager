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

                // Quyền LẺ thêm vào một module ĐÃ CÓ. Bước trên bỏ qua trường hợp này: nó chỉ cấp
                // khi nhóm chưa biết gì về module, mà nhóm Quản lý thì đã có sẵn cả bộ kpi.* —
                // nên màn "Cấu hình KPI" sẽ không ai vào được trên bản đang vận hành.
                GrantSingle(Roles.Manager, Models.Permissions.Kpi.Perm("config"));

                // KHÔNG cấp wteam.manage cho nhóm nào ở đây.
                //
                // Vai Quản lý Tổ nay đặt theo TỪNG TÀI KHOẢN qua ô tích trên màn Người dùng
                // (User.IsTeamManager), không phải theo nhóm. Cấp theo nhóm thì mọi PM mang nhóm
                // "Quản lý" đều thành Quản lý Tổ và biến mất khỏi Bảng điều khiển Tổ, bảng KPI,
                // thống kê khối lượng — trong khi họ vẫn làm việc trong dự án như mọi người.
                //
                // Bước cấp cũ đã bị gỡ khỏi đây; RevokeMisgrantedPermissions bên dưới lo thu hồi
                // phần đã lỡ cấp trên bản đang vận hành.
            }
            catch (Exception)
            {
                // Như trên: lỗi hạ tầng thì bỏ qua, lần khởi động sau làm lại.
            }
        }

        /// <summary>
        /// Cấp MỘT mã chức năng cho một nhóm nếu chưa có.
        ///
        /// Chỉ dùng cho quyền mới thêm vào module sẵn có. Khác với <see cref="GrantMissingModules"/>,
        /// hàm này cấp lại ở mỗi lần khởi động kể cả khi quản trị viên đã cố ý gỡ — nên chỉ đưa vào
        /// đây những quyền thực sự phải có, và gỡ khỏi danh sách gọi sau vài phiên bản.
        /// </summary>
        private static void GrantSingle(string roleCode, string permCode)
        {
            var group = Repository.RoleGroups.FirstOrDefault(
                g => string.Equals(g.Code, roleCode, StringComparison.OrdinalIgnoreCase));

            if (group == null || group.Permissions == "*") return;

            var current = new HashSet<string>(
                (group.Permissions ?? string.Empty).Split(',')
                    .Select(p => p.Trim()).Where(p => p.Length > 0),
                StringComparer.OrdinalIgnoreCase);

            if (!current.Add(permCode)) return;

            group.Permissions = string.Join(",", current.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
            Repository.RoleGroups.Update(group);
        }

        /// <summary>
        /// Thu hồi những chức năng lẽ ra không thuộc nhóm "Báo cáo công việc".
        ///
        /// <see cref="EnsureNewModulePermissions"/> chỉ biết CẤP THÊM, không bao giờ gỡ — nên khi
        /// một quyền đã cấp nhầm thì bản đang vận hành giữ nguyên mãi, sửa danh sách mặc định
        /// không có tác dụng. Danh sách dưới đây là các mã cần gỡ hẳn.
        ///
        /// Chỉ chạm nhóm gốc Reporter; nhóm do quản trị viên tự tạo không bị đụng tới.
        /// </summary>
        public static void RevokeMisgrantedPermissions()
        {
            try
            {
                // kpi.view bày điểm của TOÀN BỘ nhân sự — số liệu quản lý, không dành cho nhân sự
                // thường. kpi.pmapprove đi kèm nó nhưng chưa có màn nào dùng.
                //
                // wteam.view là "Bảng điều khiển Tổ": bày việc hôm nay, KPI tạm tính và số dự án
                // của TỪNG người trong tổ. Cũng là số liệu quản lý — chỉ Quản lý Tổ được xem.
                var revoke = new[]
                {
                    Models.Permissions.Kpi.Perm(Models.Permissions.View),
                    Models.Permissions.Kpi.Perm("pmapprove"),
                    Models.Permissions.Team.Perm(Models.Permissions.View),

                    // Quyền sửa MỌI dự án là quyền quản lý. PM sửa dự án của chính mình đã được
                    // BaseController.CanEditProject lo theo ngữ cảnh, không cần mã này.
                    //
                    // wteam.manage không liệt kê ở đây: MigrateTeamManagerFlag gỡ nó khỏi MỌI
                    // nhóm, không riêng nhóm Báo cáo.
                    Models.Permissions.WorkProjects.Perm(Models.Permissions.Edit)
                };

                RevokeFrom(Roles.Reporter, revoke);

                // Chuyển vai Quản lý Tổ từ nhóm quyền sang ô tích trên từng tài khoản.
                MigrateTeamManagerFlag();
            }
            catch (Exception)
            {
                // Như trên: lỗi hạ tầng thì bỏ qua, lần khởi động sau làm lại.
            }
        }

        /// <summary>
        /// Chuyển vai Quản lý Tổ từ quyền nhóm (<c>wteam.manage</c>) sang ô tích trên từng tài
        /// khoản (<see cref="User.IsTeamManager"/>), rồi gỡ quyền đó khỏi MỌI nhóm.
        ///
        /// Vì sao phải chuyển: cấp theo nhóm thì mọi PM mang nhóm "Quản lý" đều thành Quản lý Tổ
        /// và biến mất khỏi Bảng điều khiển Tổ, bảng KPI, thống kê khối lượng — trong khi họ vẫn
        /// làm việc trong dự án như mọi người. Vai này phải đặt theo từng người.
        ///
        /// Thứ tự bắt buộc: TÍCH Ô TRƯỚC rồi mới thu hồi quyền. Làm ngược lại thì giữa hai bước
        /// không còn ai là Quản lý Tổ, và nếu bước sau trượt (SQL chập chờn) thì cả tổ mất người
        /// quản lý cho tới lần khởi động sau.
        ///
        /// Chạy MỘT LẦN là xong: sau khi không nhóm nào còn wteam.manage thì vòng lặp không tìm
        /// thấy ai để tích nữa, nên các lần khởi động sau không ghi đè lựa chọn của quản trị viên.
        /// </summary>
        private static void MigrateTeamManagerFlag()
        {
            var code = Models.Permissions.Team.Perm("manage");

            var groupsHavingIt = Repository.RoleGroups.All()
                .Where(g => !string.IsNullOrWhiteSpace(g.Permissions)
                            && g.Permissions.Split(',')
                                .Select(p => p.Trim())
                                .Any(p => string.Equals(p, code, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (groupsHavingIt.Count == 0) return;

            // Bước 1 — tích ô cho những ai đang có vai qua các nhóm đó.
            foreach (var user in Repository.Users.All())
            {
                if (user.IsTeamManager) continue;

                var hasIt = groupsHavingIt.Any(g => Models.Roles.Has(user.Role, g.Code));
                if (!hasIt) continue;

                user.IsTeamManager = true;
                Repository.Users.Update(user);
            }

            // Bước 2 — gỡ quyền khỏi mọi nhóm. Nhóm toàn quyền ("*") không đụng tới: gỡ một mã lẻ
            // khỏi "*" nghĩa là phải liệt kê toàn bộ mã còn lại, biến nhóm quản trị thành danh
            // sách cứng không tự nhận chức năng thêm về sau.
            foreach (var group in groupsHavingIt)
            {
                if ((group.Permissions ?? string.Empty).Trim() == "*") continue;
                RevokeFrom(group.Code, new[] { code });
            }
        }

        private static void RevokeFrom(string roleCode, IEnumerable<string> codes)
        {
            var group = Repository.RoleGroups.FirstOrDefault(
                g => string.Equals(g.Code, roleCode, StringComparison.OrdinalIgnoreCase));

            if (group == null || group.Permissions == "*") return;

            var current = new HashSet<string>(
                (group.Permissions ?? string.Empty).Split(',')
                    .Select(p => p.Trim()).Where(p => p.Length > 0),
                StringComparer.OrdinalIgnoreCase);

            var removed = false;
            foreach (var code in codes)
            {
                if (current.Remove(code)) removed = true;
            }

            if (!removed) return;

            group.Permissions = string.Join(",", current.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
            Repository.RoleGroups.Update(group);
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
