using System;
using System.Collections.Generic;
using System.Linq;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Một thao tác trong màn hình, ví dụ Xem / Thêm / Sửa / Xóa.</summary>
    public class PermAction
    {
        public PermAction(string code, string name) { Code = code; Name = name; }
        public string Code { get; private set; }
        public string Name { get; private set; }
    }

    /// <summary>
    /// Một màn hình (module) và các thao tác của nó. Mã chức năng đầy đủ có dạng
    /// "module.action" — ví dụ module "projects" + action "edit" => "projects.edit".
    /// </summary>
    public class PermModule
    {
        public PermModule(string code, string name, string category, IEnumerable<PermAction> actions)
        {
            Code = code;
            Name = name;
            Category = category;
            Actions = actions.ToList();
        }

        public string Code { get; private set; }
        public string Name { get; private set; }

        /// <summary>Nhóm hiển thị trên màn cấu hình (Chính / Danh mục / Nhân sự / Quản trị).</summary>
        public string Category { get; private set; }

        public List<PermAction> Actions { get; private set; }

        /// <summary>Mã chức năng đầy đủ cho một thao tác của màn này.</summary>
        public string Perm(string action) { return Code + "." + action; }
    }

    /// <summary>
    /// Một mục menu (hoặc một mục con trong nhóm gập). Chỉ hiện khi tài khoản có
    /// <see cref="Permission"/> (thường là quyền "xem" của màn tương ứng).
    /// </summary>
    public class MenuLink
    {
        public string Label { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Permission { get; set; }

        public MenuLink() { Action = "Index"; }
    }

    /// <summary>Một dòng trong menu: hoặc là liên kết đơn, hoặc là nhóm gập chứa nhiều liên kết con.</summary>
    public class MenuNode
    {
        /// <summary>Liên kết đơn (null nếu đây là nhóm gập).</summary>
        public MenuLink Link { get; set; }

        /// <summary>Nhãn nhóm gập (null nếu đây là liên kết đơn).</summary>
        public string GroupLabel { get; set; }

        /// <summary>Các liên kết con của nhóm gập.</summary>
        public List<MenuLink> Children { get; set; }

        public bool IsGroup { get { return Children != null; } }
    }

    /// <summary>Một khối menu có tiêu đề (null nghĩa là khối chính không tiêu đề).</summary>
    public class MenuSection
    {
        public string Title { get; set; }
        public List<MenuNode> Nodes { get; set; }
    }

    /// <summary>
    /// Danh mục chức năng CỐ ĐỊNH của hệ thống (gắn với các endpoint là mã nguồn), cùng cấu
    /// trúc menu. Việc gán chức năng cho từng <see cref="RoleGroup"/> mới là dữ liệu cấu hình.
    /// </summary>
    public static class Permissions
    {
        // Thao tác chuẩn
        public const string View = "view";
        public const string Create = "create";
        public const string Edit = "edit";
        public const string Delete = "delete";

        private static PermAction A(string code, string name) { return new PermAction(code, name); }

        // Bộ thao tác Xem/Thêm/Sửa/Xóa hay lặp lại.
        private static PermAction[] Crud()
        {
            return new[] { A(View, "Xem"), A(Create, "Thêm"), A(Edit, "Sửa"), A(Delete, "Xóa") };
        }

        // ---------- Danh mục module ----------

        public static readonly PermModule Home =
            new PermModule("home", "Tổng hợp", "Chính", new[] { A(View, "Xem") });

        public static readonly PermModule MyReports =
            new PermModule("myreports", "Báo cáo của tôi", "Chính",
                new[] { A(View, "Xem"), A("report", "Báo cáo") });

        public static readonly PermModule Projects =
            new PermModule("projects", "Dự án", "Chính", Crud());

        public static readonly PermModule Members =
            new PermModule("members", "Thành viên", "Chính", Crud());

        public static readonly PermModule Assignments =
            new PermModule("assignments", "Phân công", "Chính", Crud());

        public static readonly PermModule TeamReports =
            new PermModule("teamreports", "Báo cáo nhân sự", "Chính",
                new[] { A(View, "Xem"), A("save", "Lưu tổng hợp"), A("send", "Gửi Telegram") });

        public static readonly PermModule WorkLogs =
            new PermModule("worklogs", "Tiến trình công việc", "Chính",
                new[] { A(View, "Xem"), A(Edit, "Sửa") });

        public static readonly PermModule Catalog =
            new PermModule("catalog", "Danh mục", "Danh mục", Crud());

        public static readonly PermModule Hrm =
            new PermModule("hrm", "HRM", "Nhân sự", new[] { A(View, "Xem") });

        public static readonly PermModule Notifications =
            new PermModule("notifications", "Thông báo", "Quản trị",
                new[] { A(View, "Xem"), A("send", "Gửi / Kích hoạt") });

        public static readonly PermModule GoConnect =
            new PermModule("goconnect", "GoConnect", "Quản trị",
                new[] { A(View, "Xem"), A("run", "Chạy / Điều khiển") });

        public static readonly PermModule Integrations =
            new PermModule("integrations", "Hệ thống tích hợp", "Quản trị", Crud());

        public static readonly PermModule Users =
            new PermModule("users", "Người dùng", "Quản trị",
                new[] { A(View, "Xem"), A(Create, "Thêm"), A(Edit, "Sửa"), A(Delete, "Xóa"), A("provision", "Mở hàng loạt") });

        public static readonly PermModule Roles =
            new PermModule("roles", "Nhóm quyền", "Quản trị", Crud());

        /// <summary>Toàn bộ module, thứ tự dùng để hiển thị trên màn cấu hình.</summary>
        public static readonly List<PermModule> All = new List<PermModule>
        {
            Home, MyReports, Projects, Members, Assignments, TeamReports, WorkLogs,
            Catalog, Hrm, Notifications, GoConnect, Integrations, Users, Roles
        };

        /// <summary>Tất cả mã chức năng đầy đủ (module.action) — dùng để cấp toàn quyền.</summary>
        public static IEnumerable<string> AllCodes()
        {
            return All.SelectMany(m => m.Actions.Select(a => m.Perm(a.Code)));
        }

        /// <summary>Bộ chức năng mặc định của nhóm "Quản lý" — dùng cho seed và làm phương án dự phòng.</summary>
        public static IEnumerable<string> ManagerDefaults()
        {
            var codes = new List<string> { Home.Perm(View) };
            codes.AddRange(new[] { MyReports, Projects, Members, Assignments, TeamReports, Catalog, WorkLogs }
                .SelectMany(m => m.Actions.Select(a => m.Perm(a.Code))));
            return codes;
        }

        /// <summary>Bộ chức năng mặc định của nhóm "Báo cáo công việc".</summary>
        public static IEnumerable<string> ReporterDefaults()
        {
            return new[] { Home.Perm(View), MyReports.Perm(View), MyReports.Perm("report") };
        }

        /// <summary>
        /// Phương án dự phòng cho ba mã nhóm gốc (Admin/Manager/Reporter) khi CSDL chưa kịp
        /// tạo/seed bảng RoleGroups (SQL chập chờn lúc khởi động). Tránh việc mọi tài khoản
        /// bỗng mất sạch quyền, kể cả quản trị.
        /// </summary>
        private static void AddBuiltinFallback(HashSet<string> set, string code)
        {
            if (string.Equals(code, Models.Roles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                set.Add("*");
            }
            else if (string.Equals(code, Models.Roles.Manager, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var c in ManagerDefaults()) set.Add(c);
            }
            else if (string.Equals(code, Models.Roles.Reporter, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var c in ReporterDefaults()) set.Add(c);
            }
        }

        // ---------- Cấu trúc menu ----------

        private static MenuNode L(string label, string controller, string permission, string action = "Index")
        {
            return new MenuNode { Link = new MenuLink { Label = label, Controller = controller, Action = action, Permission = permission } };
        }

        private static MenuNode Group(string label, params MenuLink[] children)
        {
            return new MenuNode { GroupLabel = label, Children = children.ToList() };
        }

        private static MenuLink Child(string label, string controller, string permission, string action = "Index")
        {
            return new MenuLink { Label = label, Controller = controller, Action = action, Permission = permission };
        }

        /// <summary>Menu dọc, giữ đúng cấu trúc/thứ tự cũ; mỗi mục ẩn/hiện theo quyền "xem".</summary>
        public static readonly List<MenuSection> Menu = new List<MenuSection>
        {
            new MenuSection
            {
                Title = null,
                Nodes = new List<MenuNode>
                {
                    L("Việc cần xử lý", "WorkQueue", TeamReports.Perm(View)),
                    L("Tổng hợp", "Home", Home.Perm(View)),
                    L("Báo cáo của tôi", "MyReports", MyReports.Perm(View)),
                    L("Dự án", "Projects", Projects.Perm(View)),
                    L("Thành viên", "Members", Members.Perm(View)),
                    L("Phân công", "Assignments", Assignments.Perm(View)),
                    L("Báo cáo nhân sự", "TeamReports", TeamReports.Perm(View)),
                    Group("Danh mục",
                        Child("Loại dự án", "ProjectTypes", Catalog.Perm(View)),
                        Child("Trạng thái dự án", "ProjectStatuses", Catalog.Perm(View)),
                        Child("Trạng thái tham gia", "WorkStatuses", Catalog.Perm(View)),
                        Child("Vai trò", "MemberRoles", Catalog.Perm(View)))
                }
            },
            new MenuSection
            {
                Title = "Nhân sự",
                Nodes = new List<MenuNode>
                {
                    Group("HRM",
                        Child("Nhân sự", "Hrm", Hrm.Perm(View), "Employees"),
                        Child("Đơn vị", "Hrm", Hrm.Perm(View), "Workplaces"),
                        Child("Chức danh", "Hrm", Hrm.Perm(View), "Positions"))
                }
            },
            new MenuSection
            {
                Title = "Quản trị",
                Nodes = new List<MenuNode>
                {
                    L("Tình trạng hệ thống", "SystemStatus", Users.Perm(View)),
                    L("Thông báo", "Notifications", Notifications.Perm(View)),
                    L("GoConnect", "GoConnect", GoConnect.Perm(View)),
                    L("Hệ thống tích hợp", "IntegrationSystems", Integrations.Perm(View)),
                    L("Người dùng", "Users", Users.Perm(View)),
                    L("Nhóm quyền", "PermissionGroups", Roles.Perm(View))
                }
            }
        };

        // ---------- Kiểm tra quyền ----------

        /// <summary>
        /// Hợp tất cả mã chức năng mà một tài khoản (có thể mang nhiều nhóm) được cấp.
        /// Nhóm mang "*" nghĩa là toàn quyền — trả về tập chứa đúng "*".
        /// </summary>
        public static HashSet<string> ResolvePermissions(string userRole)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var code in Models.Roles.Split(userRole))
            {
                RoleGroup group = null;
                try
                {
                    group = Data.Repository.RoleGroups.FirstOrDefault(
                        g => g.IsActive && string.Equals(g.Code, code, StringComparison.OrdinalIgnoreCase));
                }
                catch (Exception)
                {
                    // SQL chập chờn: dùng phương án dự phòng cho mã nhóm gốc.
                }

                if (group == null)
                {
                    AddBuiltinFallback(set, code);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(group.Permissions)) continue;
                foreach (var p in group.Permissions.Split(','))
                {
                    var t = p.Trim();
                    if (t.Length > 0) set.Add(t);
                }
            }
            return set;
        }

        /// <summary>Tài khoản có mã chức năng này không. Mã rỗng = không yêu cầu (ai cũng qua).</summary>
        public static bool UserHas(string userRole, string permCode)
        {
            if (string.IsNullOrEmpty(permCode)) return true;
            var set = ResolvePermissions(userRole);
            return set.Contains("*") || set.Contains(permCode);
        }
    }
}
