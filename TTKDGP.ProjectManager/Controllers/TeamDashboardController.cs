using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Bảng điều khiển của Quản lý Tổ: hôm nay ai đang làm gì, KPI tạm tính của từng người, và
    /// mỗi người đang gánh bao nhiêu dự án.
    ///
    /// Chỉ đọc và tổng hợp. KPI dựng bằng <see cref="KpiService.Fill"/> chứ KHÔNG gọi CalculateUser
    /// — hàm đó ghi xuống bảng KpiMonth, mà một màn xem thì không được sửa dữ liệu chấm điểm.
    ///
    /// Quyền wteam.view chỉ nằm trong bộ mặc định của nhóm Quản lý, nên nhân sự thường không vào
    /// được: màn này bày công việc và điểm số của TẤT CẢ mọi người.
    /// </summary>
    [AppAuthorize]
    public class TeamDashboardController : BaseController
    {
        [AppAuthorize(Permission = "wteam.view")]
        public ActionResult Index(int? year, int? month)
        {
            var today = DateTime.Today;
            var y = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year.Value : today.Year;
            var m = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : today.Month;

            // Nạp một lần rồi ghép trong bộ nhớ — ba khối bên dưới đều dùng chung các danh sách này.
            var users = WorkService.ActiveUsers();
            var allTasks = WorkService.AllTasks();
            var projects = Repository.WorkProjects.All();
            var assignments = Repository.WorkAssignments.All();

            var model = new TeamDashboardViewModel
            {
                Today = today,
                Year = y,
                Month = m,
                IsCurrentMonth = y == today.Year && m == today.Month
            };

            var byProject = projects.ToDictionary(p => p.Id, p => p);

            foreach (var user in users)
            {
                var mine = allTasks.Where(t => t.AssigneeUserId == user.Id).ToList();

                model.Members.Add(new TeamMemberRow
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Today = BuildToday(mine, today, byProject),
                    Kpi = BuildKpi(user, y, m),
                    ImplementProjects = CountProjects(mine, assignments, user.Id, y, m, TaskKinds.Checklist),
                    SupportProjects = CountProjects(mine, assignments, user.Id, y, m, TaskKinds.Support)
                });
            }

            // Người chưa có việc hôm nay lên đầu — đó chính là người cần hỏi lại.
            model.Members = model.Members
                .OrderBy(r => r.Today.Count > 0)
                .ThenBy(r => r.FullName, StringComparer.CurrentCulture)
                .ToList();

            return View(model);
        }

        /// <summary>
        /// Việc mà hôm nay người này đang phải làm: chưa đóng và hôm nay nằm trong khoảng
        /// [ngày bắt đầu … hạn hoàn thành].
        ///
        /// Thiếu ngày bắt đầu thì lấy ngày tạo — cùng quy ước với bộ tính giờ công. Việc quá hạn
        /// chưa xong vẫn tính là việc của hôm nay: nó vẫn đang phải làm.
        /// </summary>
        private static List<TeamTodayTask> BuildToday(List<WorkTask> mine, DateTime today,
            Dictionary<int, WorkProject> byProject)
        {
            var result = new List<TeamTodayTask>();

            foreach (var task in mine)
            {
                if (TaskStates.IsClosed(task.State)) continue;
                if (!task.DueDate.HasValue) continue;

                var from = task.StartDate.HasValue ? task.StartDate.Value.Date : task.CreatedAt.Date;
                var to = task.DueDate.Value.Date;
                if (from > to) from = to;

                // Quá hạn thì kéo dài tới hôm nay; còn hạn thì hôm nay phải nằm trong khoảng.
                var covers = from <= today && (today <= to || task.IsOverdue);
                if (!covers) continue;

                WorkProject project;
                byProject.TryGetValue(task.ProjectId, out project);

                result.Add(new TeamTodayTask
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    ProjectId = task.ProjectId,
                    ProjectName = project != null ? project.Name : task.ProjectName,
                    State = task.State,
                    Progress = task.Progress,
                    IsOverdue = task.IsOverdue
                });
            }

            return result
                .OrderByDescending(t => t.IsOverdue)
                .ThenBy(t => t.ProjectName, StringComparer.CurrentCulture)
                .ToList();
        }

        /// <summary>
        /// KPI tháng của một người: lấy bản đã chốt nếu có, chưa có thì dựng tạm trong bộ nhớ.
        /// </summary>
        private static KpiMonth BuildKpi(User user, int year, int month)
        {
            var saved = Repository.KpiMonths.FirstOrDefault(
                k => k.Year == year && k.Month == month && k.UserId == user.Id);
            if (saved != null) return saved;

            var preview = new KpiMonth
            {
                Year = year,
                Month = month,
                UserId = user.Id,
                UserFullName = user.FullName
            };

            KpiService.Fill(preview, KpiService.TasksOfUserInMonth(user.Id, year, month));
            return preview;
        }

        /// <summary>
        /// Số dự án mà người này có việc thuộc loại đã cho trong tháng, cộng thêm những dự án họ
        /// đang được phân công mà chưa có việc nào — người mới vào dự án cũng phải đếm là đang tham gia.
        /// </summary>
        private static int CountProjects(List<WorkTask> mine, List<WorkAssignment> assignments,
            int userId, int year, int month, string kind)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var ids = new HashSet<int>(mine
                .Where(t => t.ProjectId > 0
                            && t.Kind == kind
                            && t.State != TaskStates.Cancelled
                            && KpiService.TaskInMonth(t, year, month))
                .Select(t => t.ProjectId));

            // Giai đoạn tham gia khớp loại việc: "cả hai" thì tính cho cả triển khai lẫn hỗ trợ.
            var phase = kind == TaskKinds.Support ? AssignmentPhases.Support : AssignmentPhases.Implement;

            foreach (var a in assignments)
            {
                if (a.UserId != userId || a.ProjectId <= 0) continue;
                if (!a.OverlapsRange(from, to)) continue;
                if (a.Phase != phase && a.Phase != AssignmentPhases.Both) continue;

                ids.Add(a.ProjectId);
            }

            return ids.Count;
        }

        /// <summary>
        /// Việc của một người trong tháng theo loại — nội dung hộp thoại khi bấm vào con số dự án.
        /// </summary>
        [AppAuthorize(Permission = "wteam.view")]
        public ActionResult MemberTasks(int userId, int year, int month, string kind)
        {
            var user = Repository.Users.Find(userId);
            if (user == null) return HttpNotFound();

            var wanted = kind == TaskKinds.Support ? TaskKinds.Support : TaskKinds.Checklist;

            var tasks = WorkService.Sort(WorkService.AllTasks()
                .Where(t => t.AssigneeUserId == userId
                            && t.Kind == wanted
                            && t.State != TaskStates.Cancelled
                            && KpiService.TaskInMonth(t, year, month)));

            ViewBag.MemberName = user.FullName;
            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.Kind = wanted;

            return PartialView("_MemberTasks", tasks);
        }
    }

    /// <summary>Bảng điều khiển Tổ, xem theo tháng.</summary>
    public class TeamDashboardViewModel
    {
        public DateTime Today { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public bool IsCurrentMonth { get; set; }

        public List<TeamMemberRow> Members { get; set; }

        /// <summary>Số người hôm nay không có việc nào đang phải làm.</summary>
        public int IdleCount
        {
            get { return Members.Count(m => m.Today.Count == 0); }
        }

        public TeamDashboardViewModel()
        {
            Members = new List<TeamMemberRow>();
        }
    }

    /// <summary>Một thành viên trên bảng điều khiển Tổ.</summary>
    public class TeamMemberRow
    {
        public int UserId { get; set; }
        public string FullName { get; set; }

        /// <summary>Việc hôm nay đang phải làm.</summary>
        public List<TeamTodayTask> Today { get; set; }

        public KpiMonth Kpi { get; set; }

        public int ImplementProjects { get; set; }
        public int SupportProjects { get; set; }

        public int OverdueToday
        {
            get { return Today.Count(t => t.IsOverdue); }
        }

        public TeamMemberRow()
        {
            Today = new List<TeamTodayTask>();
        }
    }

    /// <summary>Một việc trong danh sách "hôm nay đang làm".</summary>
    public class TeamTodayTask
    {
        public int TaskId { get; set; }
        public string Title { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string State { get; set; }
        public int Progress { get; set; }
        public bool IsOverdue { get; set; }
    }
}
