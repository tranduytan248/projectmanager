using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>Số liệu gọn của một nhóm công việc, để vẽ thanh tiến độ.</summary>
    public class TaskStat
    {
        public int Total { get; set; }
        public int Done { get; set; }
        public int Overdue { get; set; }

        public int Percent
        {
            get { return Total == 0 ? 0 : (int)Math.Round(Done * 100.0 / Total); }
        }
    }

    /// <summary>
    /// Các phép đọc/ghi dùng chung của bộ quản lý công việc.
    ///
    /// Gom về một chỗ vì màn "Công việc của tôi", màn checklist và bộ chấm KPI đều cần cùng một logic —
    /// viết lại ba lần thì sớm muộn cũng lệch nhau, và điểm chất lượng tính ra sẽ khác nhau tuỳ
    /// người dùng mở màn nào.
    /// </summary>
    public static class WorkService
    {
        /// <summary>Số dòng mỗi trang cho mọi bảng danh sách của bộ quản lý công việc.</summary>
        public const int PageSize = 10;

        /// <summary>
        /// Gắn việc bỏ chỉ mục vào chính kho đầu việc: mỗi lần thêm/sửa/xoá ở bất kỳ đâu, chỉ mục
        /// trong bộ đệm đều bị bỏ để lượt xem kế tiếp dựng lại từ dữ liệu mới. Đăng ký tại đây thay
        /// vì gọi tay ở từng controller — controller viết thêm sau này khỏi phải nhớ gọi.
        /// </summary>
        static WorkService()
        {
            Repository.WorkTasks.Changed += ClearTaskIndex;
        }

        // ---------- Người dùng ----------

        /// <summary>
        /// Người dùng đang hoạt động — nguồn của mọi ô chọn "người thực hiện"/"PM". Công việc và
        /// dự án gán THẲNG theo tài khoản đăng nhập (bảng Users), không qua danh sách nhân sự riêng.
        /// </summary>
        public static List<User> ActiveUsers()
        {
            return Repository.Users.All()
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName, StringComparer.CurrentCulture)
                .ToList();
        }

        public static string UserFullName(int userId)
        {
            if (userId <= 0) return null;
            var user = Repository.Users.Find(userId);
            return user == null ? null : user.FullName;
        }

        /// <summary>
        /// Người liên quan tới một đầu việc: người đang tham gia dự án (phân công còn mở), PM và
        /// người đang được giao chính việc đó. Đây là danh sách gợi ý khi gõ @ trong trao đổi và
        /// cũng là phạm vi được nhận thông báo nhắc tên — hai nơi phải cùng một nguồn.
        /// </summary>
        public static List<User> TaskParticipants(WorkTask task)
        {
            var ids = new List<int>();

            if (task.ProjectId > 0)
            {
                ids.AddRange(Repository.WorkAssignments.All()
                    .Where(a => a.ProjectId == task.ProjectId && a.IsActive && a.UserId > 0)
                    .Select(a => a.UserId));

                var project = Repository.WorkProjects.Find(task.ProjectId);
                if (project != null && project.PmUserId > 0) ids.Add(project.PmUserId);
            }

            if (task.AssigneeUserId > 0) ids.Add(task.AssigneeUserId);

            var users = Repository.Users.All().ToDictionary(u => u.Id);

            return ids.Distinct()
                .Where(users.ContainsKey)
                .Select(id => users[id])
                .OrderBy(u => u.FullName, StringComparer.CurrentCulture)
                .ToList();
        }

        // ---------- Sinh mã dự án ----------

        /// <summary>Độ dài tối đa của mã dự án sinh tự động.</summary>
        public const int ProjectCodeLength = 10;

        /// <summary>Số ký tự ngẫu nhiên ở cuối mã, phần còn lại dành cho chữ cái đầu của tên.</summary>
        private const int CodeSuffixLength = 4;

        /// <summary>
        /// Bỏ dấu tiếng Việt, trả về chuỗi chỉ còn ký tự ASCII.
        ///
        /// Tách chuỗi ra dạng phân rã rồi bỏ các dấu thanh/dấu mũ. Riêng đ/Đ phải xử lý tay vì đây
        /// là chữ cái riêng trong bảng chữ cái, không phải d cộng dấu, nên phép phân rã không đụng tới.
        /// </summary>
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var replaced = text.Replace('đ', 'd').Replace('Đ', 'D');
            var normalized = replaced.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Sinh mã dự án từ tên: chữ cái đầu của từng từ (bỏ dấu, viết hoa) cộng thêm bốn ký tự
        /// ngẫu nhiên. Ví dụ "Hệ thống quản lý Tuyển sinh 10" → "HTQLTS" + "3F9A" = "HTQLTS3F9A".
        ///
        /// Phần ngẫu nhiên là bắt buộc: hai dự án khác nhau rất hay trùng chữ cái đầu
        /// ("Website Xã Vạn Ninh" và "Website Xã Vĩnh Nguyên" đều ra WXVN).
        /// </summary>
        public static string GenerateProjectCode(string projectName, ISet<string> taken = null)
        {
            var prefix = BuildCodePrefix(projectName);

            // Thử vài lần cho tới khi ra mã chưa dùng. Xác suất trùng cực thấp nên vòng lặp này
            // gần như luôn dừng ở lần đầu; giới hạn số lần để không bao giờ treo.
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var code = prefix + RandomSuffix();
                if (taken == null || !taken.Contains(code)) return code;
            }

            // Cực hiếm: rơi hẳn về mã ngẫu nhiên toàn phần.
            return ("PRJ" + Guid.NewGuid().ToString("N").ToUpperInvariant()).Substring(0, ProjectCodeLength);
        }

        private static string BuildCodePrefix(string projectName)
        {
            var maxPrefix = ProjectCodeLength - CodeSuffixLength;
            var plain = RemoveDiacritics(projectName ?? string.Empty).ToUpperInvariant();

            var words = plain.Split(new[] { ' ', '-', '_', '.', '/', '(', ')', ',' },
                StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder();
            foreach (var w in words)
            {
                // Chỉ lấy từ bắt đầu bằng chữ cái; số và ký hiệu bỏ qua cho mã dễ đọc.
                if (w.Length == 0 || !char.IsLetter(w[0])) continue;
                sb.Append(w[0]);
                if (sb.Length >= maxPrefix) break;
            }

            // Tên chỉ có một từ ngắn (hoặc toàn số) thì lấy luôn mấy chữ cái đầu của tên.
            if (sb.Length < 2)
            {
                var letters = new string(plain.Where(char.IsLetter).ToArray());
                sb.Clear();
                sb.Append(letters.Length > maxPrefix ? letters.Substring(0, maxPrefix) : letters);
            }

            return sb.Length == 0 ? "PRJ" : sb.ToString();
        }

        /// <summary>Bốn ký tự hex ngẫu nhiên. Dùng Guid để khỏi lo hai luồng cùng lấy một giá trị.</summary>
        private static string RandomSuffix()
        {
            return Guid.NewGuid().ToString("N").Substring(0, CodeSuffixLength).ToUpperInvariant();
        }

        /// <summary>Tập mã dự án đang dùng, để kiểm trùng khi sinh mã mới.</summary>
        public static HashSet<string> ExistingProjectCodes(int excludeProjectId = 0)
        {
            return new HashSet<string>(
                Repository.WorkProjects.All()
                    .Where(p => p.Id != excludeProjectId && !string.IsNullOrWhiteSpace(p.Code))
                    .Select(p => p.Code.Trim()),
                StringComparer.OrdinalIgnoreCase);
        }

        // ---------- Đầu việc ----------

        /// <summary>
        /// Nạp toàn bộ đầu việc một lần. SqlStore không lọc được ở tầng SQL nên mọi truy vấn đều
        /// là All(); gom về đây để chỗ gọi không vô tình gọi lại nhiều lần trong một vòng lặp.
        /// </summary>
        public static List<WorkTask> AllTasks()
        {
            return Repository.WorkTasks.All();
        }

        public static List<WorkTask> TasksOfUser(int userId, IEnumerable<WorkTask> source = null)
        {
            if (userId <= 0) return new List<WorkTask>();

            // Không truyền sẵn nguồn thì đi qua chỉ mục theo người thực hiện, khỏi quét cả bảng.
            if (source == null) return Sort(TasksByAssignee(userId));

            return Sort(source.Where(t => t.AssigneeUserId == userId));
        }

        /// <summary>Nhóm cache của chỉ mục đầu việc theo người thực hiện.</summary>
        private const string TaskIndexGroup = "wtasks-by-assignee";

        /// <summary>
        /// Đầu việc của một người, lấy qua chỉ mục gom sẵn theo người thực hiện.
        ///
        /// Cả bảng đã nằm trong bộ nhớ nhưng mỗi lượt xem vẫn phải duyệt hết để lọc ra việc của
        /// một người — số việc lớn dần theo năm tháng thì đây là phép quét tuyến tính lặp lại ở mọi
        /// màn hình cá nhân. Chỉ mục được dựng một lần rồi dùng lại trong thời gian còn hạn.
        /// </summary>
        public static List<WorkTask> TasksByAssignee(int userId)
        {
            if (userId <= 0) return new List<WorkTask>();

            var index = AppCache.GetOrAdd(AppCache.Key(TaskIndexGroup),
                () => AllTasks()
                    .Where(t => t.AssigneeUserId > 0)
                    .GroupBy(t => t.AssigneeUserId)
                    .ToDictionary(g => g.Key, g => g.ToList()));

            if (index == null) return new List<WorkTask>();

            List<WorkTask> mine;
            return index.TryGetValue(userId, out mine) ? mine.ToList() : new List<WorkTask>();
        }

        /// <summary>
        /// Bỏ chỉ mục đầu việc đang giữ trong bộ đệm. Gọi sau mỗi lần thêm/sửa/xoá đầu việc để
        /// lần xem kế tiếp dựng lại từ dữ liệu mới.
        /// </summary>
        public static void ClearTaskIndex()
        {
            AppCache.RemoveGroup(TaskIndexGroup);
        }

        public static List<WorkTask> TasksOfProject(int projectId, string kind = null,
            IEnumerable<WorkTask> source = null)
        {
            var items = (source ?? AllTasks()).Where(t => t.ProjectId == projectId);
            if (!string.IsNullOrEmpty(kind)) items = items.Where(t => t.Kind == kind);
            return Sort(items);
        }

        /// <summary>
        /// Sắp xếp thống nhất: việc quá hạn lên đầu để người dùng nhìn thấy ngay, rồi tới hạn gần
        /// nhất; việc không có hạn xuống cuối.
        /// </summary>
        public static List<WorkTask> Sort(IEnumerable<WorkTask> items)
        {
            return items
                .OrderByDescending(t => t.IsOverdue)
                .ThenBy(t => t.DueDate.HasValue ? t.DueDate.Value : DateTime.MaxValue)
                .ThenBy(t => t.SortOrder)
                .ThenBy(t => t.Title, StringComparer.CurrentCulture)
                .ToList();
        }

        /// <summary>
        /// Áp trạng thái mới lên đầu việc và giữ CompletedAt khớp theo.
        ///
        /// Mốc hoàn thành là căn cứ DUY NHẤT để chấm đúng/trễ hạn nên không được để lệch với trạng
        /// thái. Đánh dấu xong lần hai KHÔNG dời lại mốc cũ — nếu dời, người dùng bỏ tick rồi tick
        /// lại là tự biến việc trễ hạn thành đúng hạn.
        /// </summary>
        public static void ApplyState(WorkTask task, string state, int? progress = null)
        {
            task.State = state;

            if (state == TaskStates.Done)
            {
                if (!task.CompletedAt.HasValue) task.CompletedAt = DateTime.Now;
                task.Progress = 100;
                return;
            }

            task.CompletedAt = null;

            if (progress.HasValue) task.Progress = Clamp(progress.Value);
            else if (task.Progress >= 100) task.Progress = 99;
        }

        private static int Clamp(int value)
        {
            if (value < 0) return 0;
            return value > 100 ? 100 : value;
        }

        /// <summary>Điền tên dự án và tên người thực hiện để bản ghi tự đọc được về sau.</summary>
        public static void FillNames(WorkTask task)
        {
            if (task.ProjectId > 0)
            {
                var project = Repository.WorkProjects.Find(task.ProjectId);
                task.ProjectName = project == null ? null : project.Name;
            }
            else
            {
                task.ProjectName = null;
            }

            task.AssigneeName = UserFullName(task.AssigneeUserId);
        }

        /// <summary>Đếm việc theo dự án — duyệt một lượt thay vì đếm lại cho từng dòng.</summary>
        public static Dictionary<int, TaskStat> StatsByProject(IEnumerable<WorkTask> source = null)
        {
            var result = new Dictionary<int, TaskStat>();

            foreach (var task in source ?? AllTasks())
            {
                if (task.ProjectId <= 0) continue;

                TaskStat stat;
                if (!result.TryGetValue(task.ProjectId, out stat))
                {
                    stat = new TaskStat();
                    result[task.ProjectId] = stat;
                }

                stat.Total++;
                if (task.State == TaskStates.Done) stat.Done++;
                else if (task.IsOverdue) stat.Overdue++;
            }

            return result;
        }

        // ---------- Phân công ----------

        /// <summary>Các giai đoạn tham gia của một dự án, mới nhất trước.</summary>
        public static List<WorkAssignment> AssignmentsOfProject(int projectId)
        {
            return Repository.WorkAssignments.All()
                .Where(a => a.ProjectId == projectId)
                .OrderByDescending(a => a.IsActive)
                .ThenByDescending(a => a.JoinedAt)
                .ToList();
        }

        /// <summary>Người có mặt trong dự án tại một khoảng ngày (một tuần, một tháng).</summary>
        public static List<WorkAssignment> ActiveInRange(int projectId, DateTime from, DateTime to)
        {
            return Repository.WorkAssignments.All()
                .Where(a => a.ProjectId == projectId && a.OverlapsRange(from, to))
                .OrderBy(a => a.UserFullName, StringComparer.CurrentCulture)
                .ToList();
        }

        /// <summary>
        /// Người này còn giai đoạn nào ĐANG MỞ trong dự án không. Dùng để chặn tạo hai giai đoạn
        /// chồng nhau — lúc đó sẽ không biết tính theo giai đoạn nào.
        /// </summary>
        public static WorkAssignment OpenAssignment(int projectId, int userId, int excludeId = 0)
        {
            return Repository.WorkAssignments.FirstOrDefault(a =>
                a.Id != excludeId && a.ProjectId == projectId && a.UserId == userId && !a.LeftAt.HasValue);
        }

        /// <summary>Dòng PM đang hiệu lực của dự án; null nếu hiện không có ai làm PM.</summary>
        public static WorkAssignment CurrentPmAssignment(int projectId)
        {
            return Repository.WorkAssignments.FirstOrDefault(
                a => a.ProjectId == projectId && a.IsPm && !a.LeftAt.HasValue);
        }

        /// <summary>
        /// Ai làm PM dự án này tại một ngày cụ thể. Dùng cho bộ chấm KPI: PM đổi giữa chừng thì
        /// điểm cộng của PM phải tính cho đúng người của kỳ đó, không phải người đang làm hiện tại.
        /// </summary>
        public static WorkAssignment PmAt(int projectId, DateTime date)
        {
            return Repository.WorkAssignments.All()
                .Where(a => a.ProjectId == projectId && a.IsPm && a.CoversDate(date))
                .OrderByDescending(a => a.JoinedAt)
                .FirstOrDefault();
        }

        /// <summary>
        /// Tính lại PM đang hiệu lực rồi ghi vào dự án.
        ///
        /// Phải gọi sau MỌI thao tác đụng tới dòng PM (thêm, đặt làm PM, kết thúc, xoá). Hai cột
        /// PmUserId/PmName chỉ là bản sao — quên đồng bộ là quyền của PM sẽ sai: người đã bàn giao
        /// vẫn sửa được dự án, còn người mới thì không.
        /// </summary>
        public static void SyncCurrentPm(int projectId)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null) return;

            var pm = CurrentPmAssignment(projectId);
            var userId = pm == null ? 0 : pm.UserId;
            var name = pm == null ? null : pm.UserFullName;

            if (project.PmUserId == userId && project.PmName == name) return;

            project.PmUserId = userId;
            project.PmName = name;
            Repository.WorkProjects.Update(project);
        }

        // ---------- Trao đổi ----------

        /// <summary>Số lượt trao đổi của từng đầu việc, khoá theo Id đầu việc.</summary>
        public static Dictionary<int, int> CommentCounts(IEnumerable<int> taskIds)
        {
            var ids = new HashSet<int>(taskIds);
            var result = new Dictionary<int, int>();

            foreach (var comment in Repository.WorkComments.All())
            {
                if (comment.IsDeleted || !ids.Contains(comment.TaskId)) continue;

                int count;
                result.TryGetValue(comment.TaskId, out count);
                result[comment.TaskId] = count + 1;
            }

            return result;
        }

        // ---------- Báo cáo tuần ----------

        /// <summary>
        /// Hạn nộp báo cáo tuần: 17:00 thứ Sáu của chính tuần đó.
        /// CHỜ NGƯỜI DÙNG CHỐT — đổi thì sửa đúng một chỗ này.
        /// </summary>
        public const int ReportDeadlineHour = 17;

        public static DateTime ReportDeadline(int year, int week)
        {
            // FirstDayOfWeek trả về thứ Hai nên thứ Sáu là +4 ngày.
            return Infrastructure.WeekHelper.FirstDayOfWeek(year, week)
                .Date.AddDays(4).AddHours(ReportDeadlineHour);
        }

        public static WorkWeekReport FindReport(int projectId, int year, int week)
        {
            return Repository.WorkWeekReports.FirstOrDefault(
                r => r.ProjectId == projectId && r.Year == year && r.Week == week);
        }
    }
}
