using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Hosting;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Data
{
    /// <summary>
    /// Điểm truy cập duy nhất tới các kho dữ liệu SQL, kèm các phép nối dữ liệu dùng chung.
    /// </summary>
    public static class Repository
    {
        private static readonly Lazy<SqlStore<Member>> _members =
            new Lazy<SqlStore<Member>>(() => new SqlStore<Member>("Members"));

        private static readonly Lazy<SqlStore<Project>> _projects =
            new Lazy<SqlStore<Project>>(() => new SqlStore<Project>("Projects"));

        private static readonly Lazy<SqlStore<ProjectMember>> _assignments =
            new Lazy<SqlStore<ProjectMember>>(() => new SqlStore<ProjectMember>("ProjectMembers"));

        private static readonly Lazy<SqlStore<User>> _users =
            new Lazy<SqlStore<User>>(() => new SqlStore<User>("Users"));

        private static readonly Lazy<SqlStore<ProjectType>> _projectTypes =
            new Lazy<SqlStore<ProjectType>>(() => new SqlStore<ProjectType>("ProjectTypes"));

        private static readonly Lazy<SqlStore<ProjectStatus>> _projectStatuses =
            new Lazy<SqlStore<ProjectStatus>>(() => new SqlStore<ProjectStatus>("ProjectStatuses"));

        private static readonly Lazy<SqlStore<WorkStatus>> _workStatuses =
            new Lazy<SqlStore<WorkStatus>>(() => new SqlStore<WorkStatus>("WorkStatuses"));

        private static readonly Lazy<SqlStore<MemberRole>> _memberRoles =
            new Lazy<SqlStore<MemberRole>>(() => new SqlStore<MemberRole>("MemberRoles"));

        private static readonly Lazy<SqlStore<WorkLog>> _workLogs =
            new Lazy<SqlStore<WorkLog>>(() => new SqlStore<WorkLog>("WorkLogs"));

        private static readonly Lazy<SqlStore<ReminderLog>> _reminderLogs =
            new Lazy<SqlStore<ReminderLog>>(() => new SqlStore<ReminderLog>("ReminderLogs"));

        private static readonly Lazy<SqlStore<IntegrationSystem>> _integrationSystems =
            new Lazy<SqlStore<IntegrationSystem>>(() => new SqlStore<IntegrationSystem>("IntegrationSystems"));

        private static readonly Lazy<SqlStore<RoleGroup>> _roleGroups =
            new Lazy<SqlStore<RoleGroup>>(() => new SqlStore<RoleGroup>("RoleGroups"));

        // ---------- Bộ quản lý công việc & KPI ----------
        //
        // Bộ bảng RIÊNG, tiền tố Work/Kpi, không dùng chung dòng nào với Projects/Members/
        // ProjectMembers/WorkLogs cũ. Hai bộ chạy song song: bộ cũ giữ nguyên để đối chiếu, bộ mới
        // được thiết kế thẳng từ yêu cầu nghiệp vụ (vai trò, giai đoạn tham gia, ba loại đầu việc,
        // báo cáo tuần theo format, chấm KPI hai cấp).

        private static readonly Lazy<SqlStore<WorkProject>> _workProjects =
            new Lazy<SqlStore<WorkProject>>(() => new SqlStore<WorkProject>("WorkProjects"));

        private static readonly Lazy<SqlStore<WorkAssignment>> _workAssignments =
            new Lazy<SqlStore<WorkAssignment>>(() => new SqlStore<WorkAssignment>("WorkAssignments"));

        private static readonly Lazy<SqlStore<WorkTask>> _workTasks =
            new Lazy<SqlStore<WorkTask>>(() => new SqlStore<WorkTask>("WorkTasks"));

        private static readonly Lazy<SqlStore<WorkComment>> _workComments =
            new Lazy<SqlStore<WorkComment>>(() => new SqlStore<WorkComment>("WorkComments"));

        private static readonly Lazy<SqlStore<WorkTimeLog>> _workTimeLogs =
            new Lazy<SqlStore<WorkTimeLog>>(() => new SqlStore<WorkTimeLog>("WorkTimeLogs"));

        private static readonly Lazy<SqlStore<WorkProjectFile>> _workProjectFiles =
            new Lazy<SqlStore<WorkProjectFile>>(() => new SqlStore<WorkProjectFile>("WorkProjectFiles"));

        private static readonly Lazy<SqlStore<WorkWeekReport>> _workWeekReports =
            new Lazy<SqlStore<WorkWeekReport>>(() => new SqlStore<WorkWeekReport>("WorkWeekReports"));

        private static readonly Lazy<SqlStore<WorkPlanItem>> _workPlanItems =
            new Lazy<SqlStore<WorkPlanItem>>(() => new SqlStore<WorkPlanItem>("WorkPlanItems"));

        private static readonly Lazy<SqlStore<KpiSheet>> _kpiSheets =
            new Lazy<SqlStore<KpiSheet>>(() => new SqlStore<KpiSheet>("KpiSheets"));

        private static readonly Lazy<SqlStore<UserNotification>> _userNotifications =
            new Lazy<SqlStore<UserNotification>>(() => new SqlStore<UserNotification>("UserNotifications"));

        private static readonly Lazy<SqlStore<EmailTemplate>> _emailTemplates =
            new Lazy<SqlStore<EmailTemplate>>(() => new SqlStore<EmailTemplate>("EmailTemplates"));

        private static readonly Lazy<SqlStore<KpiLine>> _kpiLines =
            new Lazy<SqlStore<KpiLine>>(() => new SqlStore<KpiLine>("KpiLines"));

        private static readonly Lazy<SqlStore<KpiMonth>> _kpiMonths =
            new Lazy<SqlStore<KpiMonth>>(() => new SqlStore<KpiMonth>("KpiMonths"));

        private static readonly Lazy<SqlStore<LeaveRequest>> _leaveRequests =
            new Lazy<SqlStore<LeaveRequest>>(() => new SqlStore<LeaveRequest>("LeaveRequests"));

        private static readonly Lazy<SqlStore<KpiConfig>> _kpiConfigs =
            new Lazy<SqlStore<KpiConfig>>(() => new SqlStore<KpiConfig>("KpiConfigs"));

        private static readonly Lazy<SqlStore<PermissionSetting>> _permissionSettings =
            new Lazy<SqlStore<PermissionSetting>>(() => new SqlStore<PermissionSetting>("PermissionSettings"));

        /// <summary>Dự án trong bộ quản lý công việc.</summary>
        public static SqlStore<WorkProject> WorkProjects { get { return _workProjects.Value; } }

        /// <summary>Các giai đoạn tham gia dự án.</summary>
        public static SqlStore<WorkAssignment> WorkAssignments { get { return _workAssignments.Value; } }

        /// <summary>Đầu việc: checklist dự án, hỗ trợ theo tuần, việc ngoài dự án.</summary>
        public static SqlStore<WorkTask> WorkTasks { get { return _workTasks.Value; } }

        /// <summary>Trao đổi trong từng đầu việc.</summary>
        public static SqlStore<WorkComment> WorkComments { get { return _workComments.Value; } }

        /// <summary>Giờ công người thực hiện tự khai vào từng đầu việc (logtime).</summary>
        public static SqlStore<WorkTimeLog> WorkTimeLogs { get { return _workTimeLogs.Value; } }

        /// <summary>File tài liệu đính kèm vào từng dự án.</summary>
        public static SqlStore<WorkProjectFile> WorkProjectFiles { get { return _workProjectFiles.Value; } }

        /// <summary>Báo cáo tuần của PM cho từng dự án.</summary>
        public static SqlStore<WorkWeekReport> WorkWeekReports { get { return _workWeekReports.Value; } }

        /// <summary>Các mục checklist được chọn vào kế hoạch tuần tiếp theo.</summary>
        public static SqlStore<WorkPlanItem> WorkPlanItems { get { return _workPlanItems.Value; } }

        /// <summary>Phiếu chấm KPI theo tháng của từng nhân sự.</summary>
        public static SqlStore<KpiSheet> KpiSheets { get { return _kpiSheets.Value; } }

        /// <summary>Dòng chi tiết của phiếu chấm KPI.</summary>
        public static SqlStore<KpiLine> KpiLines { get { return _kpiLines.Value; } }

        /// <summary>Kết quả KPI tháng của từng người theo bộ chấm 30/70 + tỷ lệ ngày công.</summary>
        public static SqlStore<KpiMonth> KpiMonths { get { return _kpiMonths.Value; } }

        /// <summary>Đơn đăng ký nghỉ phép của cá nhân, chờ Quản lý Tổ duyệt.</summary>
        public static SqlStore<LeaveRequest> LeaveRequests { get { return _leaveRequests.Value; } }

        /// <summary>Bộ tham số công thức KPI đang áp dụng — luôn đúng một dòng, xem KpiConfigService.</summary>
        public static SqlStore<KpiConfig> KpiConfigs { get { return _kpiConfigs.Value; } }

        /// <summary>Tên hiển thị và trạng thái bật/tắt của từng mã chức năng.</summary>
        public static SqlStore<PermissionSetting> PermissionSettings { get { return _permissionSettings.Value; } }

        /// <summary>Thông báo cá nhân hiện ở chuông trên thanh đầu trang.</summary>
        public static SqlStore<UserNotification> UserNotifications { get { return _userNotifications.Value; } }

        /// <summary>Mẫu email của từng loại thông báo, sửa được trên màn quản trị.</summary>
        public static SqlStore<EmailTemplate> EmailTemplates { get { return _emailTemplates.Value; } }

        /// <summary>Các nhóm quyền cấu hình được (tập chức năng gán cho từng nhóm).</summary>
        public static SqlStore<RoleGroup> RoleGroups { get { return _roleGroups.Value; } }

        public static SqlStore<WorkLog> WorkLogs { get { return _workLogs.Value; } }
        public static SqlStore<ReminderLog> ReminderLogs { get { return _reminderLogs.Value; } }

        /// <summary>Các hệ thống bên ngoài được cấu hình để tích hợp (mã, tên, khoá bí mật).</summary>
        public static SqlStore<IntegrationSystem> IntegrationSystems { get { return _integrationSystems.Value; } }

        public static SqlStore<Member> Members { get { return _members.Value; } }
        public static SqlStore<Project> Projects { get { return _projects.Value; } }
        public static SqlStore<ProjectMember> Assignments { get { return _assignments.Value; } }
        public static SqlStore<User> Users { get { return _users.Value; } }

        public static SqlStore<ProjectType> ProjectTypes { get { return _projectTypes.Value; } }
        public static SqlStore<ProjectStatus> ProjectStatuses { get { return _projectStatuses.Value; } }
        public static SqlStore<WorkStatus> WorkStatuses { get { return _workStatuses.Value; } }
        public static SqlStore<MemberRole> MemberRoles { get { return _memberRoles.Value; } }

        // ---------- Danh mục dùng chung ----------

        /// <summary>Tên các mục đang sử dụng của một danh mục, theo thứ tự hiển thị — dùng đổ dropdown.</summary>
        public static List<string> ActiveNames<T>(SqlStore<T> store) where T : CatalogItem
        {
            return store.All()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name, StringComparer.CurrentCulture)
                .Select(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// Bổ sung giá trị hiện tại vào danh sách chọn nếu nó đã bị ẩn hoặc xoá khỏi danh mục,
        /// tránh việc mở bản ghi cũ ra sửa lại làm mất giá trị đang có.
        /// </summary>
        public static List<string> NamesForEdit<T>(SqlStore<T> store, string currentValue) where T : CatalogItem
        {
            var names = ActiveNames(store);
            if (!string.IsNullOrWhiteSpace(currentValue) &&
                !names.Contains(currentValue, StringComparer.CurrentCultureIgnoreCase))
            {
                names.Insert(0, currentValue);
            }
            return names;
        }

        /// <summary>Đếm số bản ghi đang dùng mỗi giá trị danh mục, khoá theo tên giá trị.</summary>
        public static Dictionary<string, int> CountUsage<TRecord>(
            IEnumerable<TRecord> records, Func<TRecord, string> selector)
        {
            return records
                .Select(selector)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .GroupBy(v => v.Trim(), StringComparer.CurrentCultureIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Đổi tên một giá trị danh mục trên toàn bộ bản ghi nghiệp vụ đang mang tên cũ.
        /// Trả về số bản ghi đã cập nhật.
        /// </summary>
        public static int RenameUsage<TRecord>(
            SqlStore<TRecord> store,
            Func<TRecord, string> read,
            Action<TRecord, string> write,
            string oldName,
            string newName) where TRecord : class, IEntity
        {
            if (string.IsNullOrWhiteSpace(oldName) ||
                string.Equals(oldName, newName, StringComparison.CurrentCulture))
            {
                return 0;
            }

            Func<TRecord, bool> matches = r =>
                string.Equals(read(r), oldName, StringComparison.CurrentCultureIgnoreCase);

            var affected = store.All().Count(matches);
            if (affected > 0)
            {
                store.UpdateWhere(matches, r => write(r, newName));
            }

            return affected;
        }

        private static string DataPath(string fileName)
        {
            return HostingEnvironment.MapPath("~/App_Data/" + fileName);
        }

        // ---------- Nhật ký công việc theo tuần ----------

        /// <summary>Nhật ký của một phân công, mới nhất trước.</summary>
        public static List<WorkLog> LogsOf(int assignmentId)
        {
            return WorkLogs.All()
                .Where(w => w.AssignmentId == assignmentId)
                .OrderByDescending(w => w.Year)
                .ThenByDescending(w => w.Week)
                .ToList();
        }

        /// <summary>Mốc mới nhất của một phân công, null nếu chưa ghi nhật ký lần nào.</summary>
        public static WorkLog LatestLogOf(int assignmentId)
        {
            return LogsOf(assignmentId).FirstOrDefault();
        }

        /// <summary>
        /// Cập nhật trạng thái và nội dung công việc của phân công theo mốc nhật ký mới nhất.
        /// Gọi sau mỗi lần thêm/sửa/xoá nhật ký để hai nơi luôn khớp nhau.
        /// Nếu không còn nhật ký nào thì giữ nguyên giá trị đang có.
        /// </summary>
        public static void SyncAssignmentFromLatestLog(int assignmentId)
        {
            var latest = LatestLogOf(assignmentId);
            if (latest == null) return;

            var assignment = Assignments.Find(assignmentId);
            if (assignment == null) return;

            assignment.WorkStatus = latest.Status;
            assignment.WorkContent = latest.Content;
            Assignments.Update(assignment);
        }

        /// <summary>Số mốc nhật ký của mỗi phân công, khoá theo Id phân công.</summary>
        public static Dictionary<int, int> LogCountByAssignment()
        {
            return WorkLogs.All()
                .GroupBy(w => w.AssignmentId)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>Các năm đã có nhật ký, để đổ dropdown chọn năm.</summary>
        public static List<int> YearsInLogs()
        {
            return WorkLogs.All().Select(w => w.Year).Distinct().OrderBy(y => y).ToList();
        }

        /// <summary>Độ ưu tiên của từng trạng thái dự án, khoá theo tên trạng thái.</summary>
        public static Dictionary<string, int> ProjectStatusPriorities()
        {
            return ProjectStatuses.All()
                .GroupBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Priority, StringComparer.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Độ ưu tiên của một trạng thái. Trạng thái lạ (không có trong danh mục) bị đẩy xuống cuối.
        /// </summary>
        public static int PriorityOf(Dictionary<string, int> priorities, string status)
        {
            int p;
            if (!string.IsNullOrWhiteSpace(status) && priorities.TryGetValue(status, out p)) return p;
            return int.MaxValue;
        }

        /// <summary>Tên thành viên theo Id. Trả về null nếu không tìm thấy.</summary>
        public static string MemberName(int memberId)
        {
            if (memberId <= 0) return null;
            var member = Members.Find(memberId);
            return member == null ? null : member.FullName;
        }

        /// <summary>
        /// Mốc nhật ký mới nhất của từng phân công, khoá theo Id phân công.
        /// Duyệt một lượt thay vì gọi LatestLogOf cho từng dòng.
        /// </summary>
        public static Dictionary<int, WorkLog> LatestLogByAssignment()
        {
            var result = new Dictionary<int, WorkLog>();

            foreach (var log in WorkLogs.All())
            {
                WorkLog current;
                if (!result.TryGetValue(log.AssignmentId, out current) ||
                    log.Year > current.Year ||
                    (log.Year == current.Year && log.Week > current.Week))
                {
                    result[log.AssignmentId] = log;
                }
            }

            return result;
        }

        /// <summary>
        /// Nối phân công với dự án và thành viên thành các dòng hiển thị.
        /// Nội dung và trạng thái công việc lấy theo tuần báo cáo mới nhất; phân công chưa ghi
        /// nhật ký lần nào thì dùng giá trị đang lưu trên chính phân công đó.
        /// Phân công trỏ tới dự án/thành viên đã bị xoá vẫn được giữ lại, dùng tên đã lưu kèm ghi chú.
        /// </summary>
        public static List<SummaryRow> BuildSummaryRows()
        {
            var projects = Projects.All().ToDictionary(p => p.Id);
            var members = Members.All().ToDictionary(m => m.Id);
            var priorities = ProjectStatusPriorities();
            var latestLogs = LatestLogByAssignment();

            return Assignments.All()
                .Select(a =>
                {
                    Project project = null;
                    if (a.ProjectId > 0) projects.TryGetValue(a.ProjectId, out project);

                    Member member = null;
                    if (a.MemberId > 0) members.TryGetValue(a.MemberId, out member);

                    WorkLog latest;
                    latestLogs.TryGetValue(a.Id, out latest);

                    return new SummaryRow
                    {
                        AssignmentId = a.Id,
                        ProjectId = a.ProjectId,
                        ProjectName = project != null
                            ? project.Name
                            : FallbackName(a.ProjectName, "dự án đã xoá"),
                        Customer = project != null ? project.Customer : null,
                        ProjectType = project != null ? project.ProjectType : null,
                        ProjectStatus = project != null ? project.CurrentStatus : null,
                        ProjectStatusPriority = project == null
                            ? int.MaxValue
                            : PriorityOf(priorities, project.CurrentStatus),
                        Pm = project == null ? null : MemberNameFrom(members, project.PmMemberId),
                        PmMemberId = project == null ? 0 : project.PmMemberId,

                        MemberId = a.MemberId,
                        MemberName = member != null
                            ? member.FullName
                            : FallbackName(a.MemberName, "thành viên đã xoá"),

                        Role = a.Role,
                        WorkStatus = latest != null ? latest.Status : a.WorkStatus,
                        WorkContent = latest != null ? latest.Content : a.WorkContent,
                        IsActive = a.IsActive,

                        LatestWeek = latest != null ? (int?)latest.Week : null,
                        LatestYear = latest != null ? (int?)latest.Year : null,
                        WeekLabel = latest == null
                            ? null
                            : string.Format("Tuần {0}/{1}", latest.Week, latest.Year),
                        WeekRange = latest == null
                            ? null
                            : string.Format("{0:dd/MM} – {1:dd/MM/yyyy}",
                                Infrastructure.WeekHelper.FirstDayOfWeek(latest.Year, latest.Week),
                                Infrastructure.WeekHelper.LastDayOfWeek(latest.Year, latest.Week))
                    };
                })
                .ToList();
        }

        private static string MemberNameFrom(Dictionary<int, Member> members, int memberId)
        {
            Member m;
            return memberId > 0 && members.TryGetValue(memberId, out m) ? m.FullName : null;
        }

        private static string FallbackName(string storedName, string label)
        {
            return string.IsNullOrWhiteSpace(storedName)
                ? "(" + label + ")"
                : storedName + " (" + label + ")";
        }

        /// <summary>
        /// Trạng thái tham gia để đổ bộ lọc: lấy từ danh mục, bổ sung các giá trị còn sót trong
        /// dữ liệu nhưng đã bị gỡ khỏi danh mục, để bộ lọc không bỏ lỡ bản ghi nào.
        /// </summary>
        public static List<string> FilterableWorkStatuses()
        {
            var fromCatalog = ActiveNames(WorkStatuses);

            var inData = Assignments.All()
                .Select(a => a.WorkStatus)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Where(s => !fromCatalog.Contains(s, StringComparer.CurrentCultureIgnoreCase))
                .OrderBy(s => s, StringComparer.CurrentCulture);

            return fromCatalog.Concat(inData).ToList();
        }

        /// <summary>Giữ tên dự án/thành viên trong phân công khớp với bản ghi gốc sau khi đổi tên.</summary>
        public static void SyncDenormalizedNames(int? projectId, int? memberId)
        {
            if (projectId.HasValue)
            {
                var project = Projects.Find(projectId.Value);
                if (project != null)
                {
                    Assignments.UpdateWhere(
                        a => a.ProjectId == project.Id,
                        a => a.ProjectName = project.Name);
                }
            }

            if (memberId.HasValue)
            {
                var member = Members.Find(memberId.Value);
                if (member != null)
                {
                    Assignments.UpdateWhere(
                        a => a.MemberId == member.Id,
                        a => a.MemberName = member.FullName);
                }
            }
        }
    }
}
