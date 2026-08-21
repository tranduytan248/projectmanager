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
                new[] { A(View, "Xem"), A("save", "Lưu tổng hợp") });

        public static readonly PermModule WorkLogs =
            new PermModule("worklogs", "Tiến trình công việc", "Chính",
                new[] { A(View, "Xem"), A(Edit, "Sửa") });

        // ---------- Bộ quản lý công việc & KPI (bảng riêng, chạy song song bộ cũ) ----------

        /// <summary>
        /// Bảng điều khiển Tổ, và quyền "Quản lý Tổ" nói chung.
        ///
        /// <c>wteam.manage</c> là vai QUẢN LÝ TỔ: xem/sửa mọi dự án và mọi đầu việc của tổ, xem
        /// việc của người khác. Trước đây vai này được SUY RA từ quyền sửa dự án
        /// (<c>wprojects.edit</c>) — cấp cho ai quyền sửa dự án là vô tình cho họ thấy việc của
        /// cả tổ, mà nhìn màn Nhóm quyền không tài nào đoán ra. Nay nó là một ô tích riêng.
        /// </summary>
        public static readonly PermModule Team =
            new PermModule("wteam", "Bảng điều khiển Tổ", "Công việc",
                new[] { A(View, "Xem bảng điều khiển"), A("manage", "Quản lý Tổ — xem và sửa mọi dự án, mọi việc") });

        public static readonly PermModule WorkProjects =
            new PermModule("wprojects", "Dự án", "Công việc", Crud());

        /// <summary>
        /// Đầu việc. "Xem" là quyền nền ai cũng có (thấy việc của mình, checklist dự án mình tham
        /// gia, trao đổi). Thêm/Sửa/Xóa là quyền của màn "Giao việc riêng" — chỉ Quản lý Tổ.
        /// Việc PM sửa checklist dự án mình được chặn theo ngữ cảnh ở BaseController.CanEditProject.
        /// </summary>
        public static readonly PermModule WorkTasks =
            new PermModule("wtasks", "Công việc", "Công việc",
                new[] { A(View, "Xem"), A(Create, "Giao việc riêng"), A(Edit, "Sửa"),
                        A(Delete, "Xóa"), A("import", "Import checklist") });

        public static readonly PermModule WorkReports =
            new PermModule("wreports", "Báo cáo tuần dự án", "Công việc",
                new[] { A(View, "Xem"), A(Edit, "Lập báo cáo") });

        public static readonly PermModule Kpi =
            new PermModule("kpi", "Chấm KPI", "Công việc",
                new[] { A(View, "Xem"), A("generate", "Sinh bảng chấm"),
                        A("pmapprove", "PM duyệt"), A("approve", "Duyệt lần cuối"),
                        A("export", "Kết xuất"), A("send", "Gửi email"),
                        A("config", "Sửa công thức chấm") });

        /// <summary>
        /// Nghỉ phép. "Xem" là quyền nền AI CŨNG CÓ — đó là màn tự đăng ký và xem đơn của chính
        /// mình. "Duyệt" và "Xem tất cả" là quyền của Quản lý Tổ; thiếu tách đôi này thì cấp quyền
        /// đăng ký cho nhân sự cũng là cấp luôn quyền duyệt đơn của người khác.
        /// </summary>
        public static readonly PermModule Leaves =
            new PermModule("leaves", "Nghỉ phép", "Công việc",
                new[] { A(View, "Đăng ký / xem đơn của mình"), A("all", "Xem đơn của tất cả"),
                        A("approve", "Duyệt đơn") });

        /// <summary>
        /// Thống kê khối lượng công việc theo tháng trên màn checklist dự án. Tách riêng khỏi
        /// wtasks.view vì đây là số liệu quản lý — chỉ Quản lý Tổ được thấy.
        /// </summary>
        public static readonly PermModule Workload =
            new PermModule("workload", "Thống kê khối lượng", "Công việc", new[] { A(View, "Xem") });

        public static readonly PermModule Catalog =
            new PermModule("catalog", "Danh mục", "Danh mục", Crud());

        public static readonly PermModule Hrm =
            new PermModule("hrm", "HRM", "Nhân sự", new[] { A(View, "Xem") });

        /// <summary>
        /// Danh bạ lấy từ cổng CAS VNPT (id.vnpt.com.vn → hrm.vnpt.vn) qua lệnh "/signin" trên bot
        /// Telegram, xem <see cref="Infrastructure.AppSettings.Hrm"/>. HOÀN TOÀN KHÁC module
        /// <see cref="Hrm"/> ở trên (đó là dữ liệu đồng bộ từ GoConnect).
        ///
        /// KHÔNG đưa vào <see cref="ManagerDefaults"/>/<see cref="ReporterDefaults"/> — cũng như
        /// module Hrm, đây là thông tin cá nhân của toàn bộ nhân sự công ty nên chỉ Admin (nhóm
        /// "*") mới thấy theo mặc định.
        /// </summary>
        public static readonly PermModule HrmDirectory =
            new PermModule("hrmdirectory", "CAS VNPT (HRM)", "Nhân sự", new[] { A(View, "Xem") });

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

        /// <summary>
        /// Danh mục chức năng: đổi tên hiển thị và BẬT/TẮT từng chức năng của phần mềm.
        ///
        /// Tách khỏi "roles" vì mức ảnh hưởng khác hẳn: sửa nhóm quyền chỉ đổi quyền của một
        /// nhóm, còn tắt một chức năng là đóng nó với TẤT CẢ mọi người, kể cả tài khoản toàn
        /// quyền. Người được giao cấp quyền hàng ngày không nhất thiết được đụng tới cái sau.
        /// </summary>
        public static readonly PermModule Functions =
            new PermModule("functions", "Chức năng hệ thống", "Quản trị",
                new[] { A(View, "Xem"), A(Edit, "Đổi tên / Bật tắt") });

        /// <summary>Toàn bộ module, thứ tự dùng để hiển thị trên màn cấu hình.</summary>
        public static readonly List<PermModule> All = new List<PermModule>
        {
            Home, MyReports, Projects, Members, Assignments, TeamReports, WorkLogs, Catalog,
            Team, WorkProjects, WorkTasks, WorkReports, Kpi, Leaves, Workload,
            Hrm, HrmDirectory, Notifications, GoConnect, Integrations, Users, Roles, Functions
        };

        /// <summary>Tất cả mã chức năng đầy đủ (module.action) — dùng để cấp toàn quyền.</summary>
        public static IEnumerable<string> AllCodes()
        {
            return All.SelectMany(m => m.Actions.Select(a => m.Perm(a.Code)));
        }

        /// <summary>Bộ chức năng mặc định của nhóm "Quản lý" — dùng cho seed và làm phương án dự phòng.</summary>
        public static IEnumerable<string> ManagerDefaults()
        {
            // KHÔNG có Team.Perm("manage"): vai Quản lý Tổ đặt theo TỪNG TÀI KHOẢN qua ô tích
            // trên màn Người dùng, không theo nhóm. Cấp theo nhóm thì mọi PM mang nhóm "Quản lý"
            // đều thành Quản lý Tổ và biến mất khỏi các bảng theo dõi.
            var codes = new List<string> { Home.Perm(View), Team.Perm(View) };
            codes.AddRange(new[] { MyReports, Projects, Members, Assignments, TeamReports,
                                   Catalog, WorkLogs,
                                   WorkProjects, WorkTasks, WorkReports, Kpi, Leaves, Workload }
                .SelectMany(m => m.Actions.Select(a => m.Perm(a.Code))));
            return codes;
        }

        /// <summary>
        /// Bộ chức năng mặc định của nhóm "Báo cáo công việc" — dùng cho cả PM lẫn nhân sự thường.
        ///
        /// Nhóm này BUỘC phải có quyền sửa checklist và lập báo cáo tuần, vì "PM" không phải một
        /// nhóm quyền riêng: cùng một tài khoản là PM ở dự án này nhưng chỉ là nhân sự ở dự án
        /// khác. Việc chặn thật nằm ở BaseController.IsPmOf / CanEditProject, kiểm theo đúng dự án
        /// đang thao tác. Bỏ lớp kiểm đó thì mọi nhân sự sửa được checklist của mọi dự án.
        /// </summary>
        public static IEnumerable<string> ReporterDefaults()
        {
            return new[]
            {
                Home.Perm(View),
                MyReports.Perm(View), MyReports.Perm("report"),

                // Xem việc của mình, xem checklist dự án mình tham gia, trao đổi trong đầu việc.
                WorkTasks.Perm(View),

                // PM cần import checklist và lập báo cáo tuần. KHÔNG cấp wtasks.create/edit/delete
                // — đó là quyền của màn "Việc ngoài dự án", chỉ Quản lý Tổ mới giao loại việc này.
                WorkTasks.Perm("import"),
                WorkReports.Perm(View), WorkReports.Perm(Edit),

                // KHÔNG cấp kpi.view: màn "KPI theo tháng" bày điểm của TOÀN BỘ nhân sự, đó là số
                // liệu quản lý. Điểm của chính mình đã có trên màn Tổng quan.
                //
                // Tự đăng ký nghỉ phép và xem đơn của CHÍNH MÌNH. Không cấp leaves.all/approve —
                // xem đơn người khác và duyệt đơn là việc của Quản lý Tổ.
                Leaves.Perm(View)
            };
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
            // ===== BỘ MỚI: quản lý công việc & KPI =====
            // Khối đầu không tiêu đề: những màn AI CŨNG dùng, bất kể vai trò.
            new MenuSection
            {
                Title = null,
                Nodes = new List<MenuNode>
                {
                    L("Tổng quan", "Dashboard", WorkTasks.Perm(View)),
                    L("Công việc của tôi", "MyWork", WorkTasks.Perm(View), "Tasks"),
                    // Lich ca nhan — mo cho MOI tai khoan da dang nhap, khong xet quyen xem cong
                    // viec rieng (WorkTasks.Perm(View)) nhu 2 muc tren, vi day la du lieu cua
                    // chinh minh chu khong phai du lieu nghiep vu can duoc cap quyen.
                    L("Lịch công việc", "Calendar", ""),
                    L("Dự án của tôi", "MyWork", WorkTasks.Perm(View), "Projects"),
                    L("Nghỉ phép của tôi", "Leaves", Leaves.Perm(View), "My")
                }
            },

            // Khối này tự ẩn khi tài khoản không có quyền nào trong đó (xem _Layout), nên người
            // dùng thường chỉ nhìn thấy đúng khối đầu.
            new MenuSection
            {
                Title = "Quản lý Tổ",
                Nodes = new List<MenuNode>
                {
                    L("Bảng điều khiển Tổ", "TeamDashboard", Team.Perm(View)),
                    L("Lịch công tác Tổ", "TeamCalendar", Team.Perm(View)),
                    L("Dự án", "WorkProjects", WorkProjects.Perm(View)),
                    L("Giao việc riêng", "PrivateTasks", WorkTasks.Perm(Create)),
                    L("Duyệt nghỉ phép", "Leaves", Leaves.Perm("approve"), "Approve"),
                    L("KPI theo tháng", "Kpi", Kpi.Perm(View))
                }
            },

            // ===== BỘ CŨ: các màn vẫn chạy (mở được bằng đường dẫn) nhưng ẨN khỏi menu — bộ mới
            // đã thay thế. Muốn hiện lại thì thêm lại các mục vào đây.
            new MenuSection
            {
                Title = "Nhân sự",
                Nodes = new List<MenuNode>
                {
                    Group("HRM",
                        Child("Nhân sự", "Hrm", Hrm.Perm(View), "Employees"),
                        Child("Đơn vị", "Hrm", Hrm.Perm(View), "Workplaces"),
                        Child("Chức danh", "Hrm", Hrm.Perm(View), "Positions")),
                    Group("CAS VNPT (HRM)",
                        Child("Nhân sự", "HrmDirectory", HrmDirectory.Perm(View), "Employees"),
                        Child("Đơn vị", "HrmDirectory", HrmDirectory.Perm(View), "Departments"),
                        Child("Chức danh", "HrmDirectory", HrmDirectory.Perm(View), "Jobs"))
                }
            },
            new MenuSection
            {
                Title = "Quản trị",
                Nodes = new List<MenuNode>
                {
                    Group("Danh mục",
                        Child("Loại dự án", "ProjectTypes", Catalog.Perm(View)),
                        Child("Trạng thái dự án", "ProjectStatuses", Catalog.Perm(View)),
                        Child("Trạng thái tham gia", "WorkStatuses", Catalog.Perm(View)),
                        Child("Vai trò", "MemberRoles", Catalog.Perm(View))),
                    L("Cấu hình KPI", "KpiConfig", Kpi.Perm("config")),
                    L("Tình trạng hệ thống", "SystemStatus", Users.Perm(View)),
                    L("Thông báo", "Notifications", Notifications.Perm(View)),
                    L("Mẫu email", "EmailTemplates", Notifications.Perm(View)),
                    L("GoConnect", "GoConnect", GoConnect.Perm(View)),
                    L("Hệ thống tích hợp", "IntegrationSystems", Integrations.Perm(View)),
                    L("Người dùng", "Users", Users.Perm(View)),
                    L("Nhóm quyền", "PermissionGroups", Roles.Perm(View)),
                    L("Chức năng hệ thống", "Functions", Functions.Perm(View))
                }
            }
        };

        // ---------- Kiểm tra quyền ----------

        /// <summary>
        /// Hợp tất cả mã chức năng mà một tài khoản (có thể mang nhiều nhóm) được cấp.
        /// Nhóm mang "*" nghĩa là toàn quyền — trả về tập chứa đúng "*".
        ///
        /// Kết quả được nhớ trong phạm vi MỘT request: một lượt mở trang hỏi quyền rất nhiều lần
        /// (bộ lọc AppAuthorize, ViewBag.Can trong view, rồi từng hàm CanXem/CanSua trên controller),
        /// mà mỗi lượt hỏi đều quét lại bảng nhóm quyền. Gặp lúc SQL chập chờn thì mỗi lượt còn kéo
        /// theo bốn lần thử mở kết nối kèm khoảng nghỉ, cộng lại thành vài giây cho một trang.
        ///
        /// Nhớ theo request là đủ và cũng là mức an toàn nhất: đổi quyền có hiệu lực ngay ở lượt
        /// xem kế tiếp, không phải chờ hết hạn bộ đệm hay đăng nhập lại.
        ///
        /// KHÔNG bao giờ nhận tập quyền do trình duyệt gửi lên: chỉ cần sửa một dòng trong
        /// localStorage là tự cấp được "*" — toàn quyền. Quyền phải do máy chủ tự xác định.
        /// </summary>
        public static HashSet<string> ResolvePermissions(string userRole)
        {
            var context = System.Web.HttpContext.Current;
            var cacheKey = "TTKDGP:Perms:" + (userRole ?? string.Empty);

            if (context != null)
            {
                var cached = context.Items[cacheKey] as HashSet<string>;
                if (cached != null) return cached;
            }

            var set = Build(userRole);

            if (context != null) context.Items[cacheKey] = set;
            return set;
        }

        /// <summary>Dựng tập quyền từ dữ liệu. Tách riêng để phần nhớ tạm ở trên gọn một mạch.</summary>
        private static HashSet<string> Build(string userRole)
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
                catch (Exception ex)
                {
                    // SQL chập chờn: dùng phương án dự phòng cho mã nhóm gốc.
                    //
                    // Có ghi lại: nuốt lỗi hoàn toàn thì lúc máy chủ SQL không tới được, mỗi lượt
                    // hỏi quyền vẫn âm thầm chờ hết bốn lần thử mở kết nối rồi rơi về dự phòng —
                    // người dùng chỉ thấy trang chậm, còn nhật ký thì sạch trơn, không lần ra được.
                    System.Diagnostics.Debug.WriteLine(string.Format(
                        "Đọc nhóm quyền '{0}' trượt, dùng quyền dự phòng: {1}", code, ex.Message));
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

        /// <summary>
        /// Nguồn trạng thái BẬT/TẮT của từng mã chức năng. Lớp Models không được gọi ngược lên
        /// Services, nên tầng trên (Global.asax lúc khởi động) cắm hàm kiểm vào đây. Chưa cắm thì
        /// coi mọi chức năng đều bật — nhờ vậy UserHas không bao giờ hỏng dù chạy ngoài web.
        /// </summary>
        public static Func<string, bool> EnabledProvider { get; set; }

        /// <summary>
        /// Chức năng có đang mở không, hỏi qua <see cref="EnabledProvider"/>.
        /// Lỗi đọc cấu hình KHÔNG được phép khoá mọi người ra ngoài, nên trượt thì coi như mở.
        /// </summary>
        private static bool IsEnabled(string permCode)
        {
            var provider = EnabledProvider;
            if (provider == null) return true;

            try
            {
                return provider(permCode);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Tài khoản có mã chức năng này không. Mã rỗng = không yêu cầu (ai cũng qua).
        ///
        /// Chức năng bị TẮT trên màn "Chức năng" thì không ai qua được, KỂ CẢ tài khoản toàn
        /// quyền ("*"). Đó là điểm khác giữa "tắt" và "gỡ khỏi nhóm quyền": tắt là đóng hẳn tính
        /// năng khỏi phần mềm, còn gỡ khỏi nhóm chỉ là không cấp cho nhóm đó. Nếu "*" vẫn lọt qua
        /// thì quản trị viên tắt một màn rồi vẫn thấy nó, và sẽ tưởng nút tắt bị hỏng.
        /// </summary>
        public static bool UserHas(string userRole, string permCode)
        {
            if (string.IsNullOrEmpty(permCode)) return true;
            if (!IsEnabled(permCode)) return false;

            var set = ResolvePermissions(userRole);
            return set.Contains("*") || set.Contains(permCode);
        }
    }
}
