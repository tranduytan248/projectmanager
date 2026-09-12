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
            //
            // Bỏ lãnh đạo Tổ khỏi bảng: họ không nhận đầu việc như nhân sự thực thi nên dòng của
            // họ luôn rỗng, vừa làm loãng danh sách vừa kéo lệch các con số trung bình.
            var users = WorkService.TrackedUsers();
            var allTasks = WorkService.AllTasks();
            var projects = Repository.WorkProjects.All();
            var todayLogs = Repository.WorkTimeLogs.All()
                .Where(l => l.WorkDate.Date == today)
                .ToList();

            var model = new TeamDashboardViewModel
            {
                Today = today,
                Year = y,
                Month = m,
                IsCurrentMonth = y == today.Year && m == today.Month
            };

            var byProject = projects.ToDictionary(p => p.Id, p => p);
            var taskById = allTasks.ToDictionary(t => t.Id, t => t);

            foreach (var user in users)
            {
                var mine = allTasks.Where(t => t.AssigneeUserId == user.Id).ToList();
                var userTodayLogs = todayLogs.Where(l => l.UserId == user.Id).ToList();

                model.Members.Add(new TeamMemberRow
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Today = BuildToday(mine, today, y, m, byProject),
                    WorkedToday = BuildWorkedToday(user.Id, mine, userTodayLogs, taskById, today, y, m, byProject),
                    TodayLoggedHours = userTodayLogs.Sum(l => l.Hours),
                    Kpi = BuildKpi(user, y, m),
                    Implement = CountProjects(mine, y, m, TaskKinds.Checklist),
                    Support = CountProjects(mine, y, m, TaskKinds.Support)
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
        /// Việc mà hôm nay người này đang phải làm: chưa đóng, THUỘC THÁNG ĐANG XEM, và hôm nay
        /// nằm trong khoảng [ngày bắt đầu … hạn hoàn thành].
        ///
        /// Thiếu ngày bắt đầu thì lấy ngày tạo — cùng quy ước với bộ tính giờ công.
        ///
        /// Việc quá hạn từ tháng trước KHÔNG hiện ở đây: cả màn đang xem theo một tháng, để lẫn
        /// việc tháng khác vào thì con số không còn khớp với tháng ghi trên đầu trang. Muốn xem
        /// thì chuyển ô chọn về tháng đó.
        /// </summary>
        private static List<TeamTodayTask> BuildToday(List<WorkTask> mine, DateTime today,
            int year, int month, Dictionary<int, WorkProject> byProject)
        {
            var result = new List<TeamTodayTask>();

            foreach (var task in mine)
            {
                if (TaskStates.IsClosed(task.State)) continue;

                // Việc tạm dừng không phải "việc hôm nay": nó đang vướng ở đâu đó chờ gỡ chứ
                // không ai làm. Để lẫn vào đây thì con số bên cột đếm nói người này đang bận,
                // trong khi thực tế họ đang rảnh — đúng chỗ Quản lý Tổ cần nhìn ra để hỏi lại.
                if (task.State == TaskStates.Paused) continue;

                if (!task.DueDate.HasValue) continue;
                if (!KpiService.TaskInMonth(task, year, month)) continue;

                var from = task.StartDate.HasValue ? task.StartDate.Value.Date : task.CreatedAt.Date;
                var to = task.DueDate.Value.Date;
                if (from > to) from = to;

                // Việc đã bắt đầu và chưa xong tính là việc của hôm nay — kể cả khi đã quá hạn,
                // vì nó vẫn đang phải làm. Việc chưa tới ngày bắt đầu thì chưa tính.
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
        /// Danh sách công việc đã hoàn thành hoặc đang thực hiện trong hôm nay của một nhân sự:
        /// 1. Công việc có ghi nhận logtime trong ngày hôm nay (WorkDate == today).
        /// 2. Công việc được giao và đã Hoàn thành trong hôm nay (CompletedAt == today hoặc chuyển Done hôm nay).
        /// 3. Công việc Đang làm (InProgress) mà hôm nay nằm trong thời hạn làm.
        /// </summary>
        private static List<TeamWorkedTodayTask> BuildWorkedToday(
            int userId,
            List<WorkTask> mine,
            List<WorkTimeLog> userTodayLogs,
            Dictionary<int, WorkTask> taskById,
            DateTime today,
            int year,
            int month,
            Dictionary<int, WorkProject> byProject)
        {
            var result = new List<TeamWorkedTodayTask>();
            var processedTaskIds = new HashSet<int>();

            // Nhóm 1: Các công việc mà người này có ghi giờ (logtime) hôm nay
            foreach (var logGroup in userTodayLogs.GroupBy(l => l.TaskId))
            {
                var taskId = logGroup.Key;
                WorkTask task;
                if (!taskById.TryGetValue(taskId, out task))
                {
                    task = Repository.WorkTasks.Find(taskId);
                }
                if (task == null) continue;

                processedTaskIds.Add(task.Id);

                WorkProject project;
                byProject.TryGetValue(task.ProjectId, out project);

                var hoursToday = logGroup.Sum(l => l.Hours);
                var notes = string.Join("; ", logGroup
                    .Where(l => !string.IsNullOrWhiteSpace(l.Note))
                    .Select(l => l.Note.Trim())
                    .Distinct());

                var isCompletedToday = (task.CompletedAt.HasValue && task.CompletedAt.Value.Date == today)
                    || (task.State == TaskStates.Done && task.UpdatedAt.HasValue && task.UpdatedAt.Value.Date == today);

                result.Add(new TeamWorkedTodayTask
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    ProjectId = task.ProjectId,
                    ProjectName = project != null ? project.Name : task.ProjectName,
                    State = task.State,
                    Progress = task.Progress,
                    IsOverdue = task.IsOverdue,
                    IsCompletedToday = isCompletedToday,
                    LoggedHoursToday = hoursToday,
                    TodayLogNote = notes
                });
            }

            // Nhóm 2: Các công việc được giao cho người này và đã Hoàn thành trong hôm nay (chưa có logtime ở trên)
            var doneToday = mine.Where(t => !processedTaskIds.Contains(t.Id) &&
                ((t.CompletedAt.HasValue && t.CompletedAt.Value.Date == today)
                 || (t.State == TaskStates.Done && t.UpdatedAt.HasValue && t.UpdatedAt.Value.Date == today)));

            foreach (var task in doneToday)
            {
                processedTaskIds.Add(task.Id);
                WorkProject project;
                byProject.TryGetValue(task.ProjectId, out project);

                result.Add(new TeamWorkedTodayTask
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    ProjectId = task.ProjectId,
                    ProjectName = project != null ? project.Name : task.ProjectName,
                    State = task.State,
                    Progress = task.Progress,
                    IsOverdue = task.IsOverdue,
                    IsCompletedToday = true,
                    LoggedHoursToday = 0,
                    TodayLogNote = null
                });
            }

            // Nhóm 3: Các công việc Đang làm (InProgress) trong hạn hôm nay (chưa được thêm ở các nhóm trên)
            var inProgressToday = mine.Where(t => !processedTaskIds.Contains(t.Id) &&
                t.State == TaskStates.InProgress &&
                KpiService.TaskInMonth(t, year, month));

            foreach (var task in inProgressToday)
            {
                if (!task.DueDate.HasValue) continue;
                var from = task.StartDate.HasValue ? task.StartDate.Value.Date : task.CreatedAt.Date;
                var to = task.DueDate.Value.Date;
                if (from > to) from = to;

                var covers = from <= today && (today <= to || task.IsOverdue);
                if (!covers) continue;

                processedTaskIds.Add(task.Id);
                WorkProject project;
                byProject.TryGetValue(task.ProjectId, out project);

                result.Add(new TeamWorkedTodayTask
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    ProjectId = task.ProjectId,
                    ProjectName = project != null ? project.Name : task.ProjectName,
                    State = task.State,
                    Progress = task.Progress,
                    IsOverdue = task.IsOverdue,
                    IsCompletedToday = false,
                    LoggedHoursToday = 0,
                    TodayLogNote = null
                });
            }

            return result
                .OrderByDescending(t => t.LoggedHoursToday > 0)
                .ThenByDescending(t => t.IsCompletedToday)
                .ThenByDescending(t => t.IsOverdue)
                .ThenBy(t => t.ProjectName, StringComparer.CurrentCulture)
                .ThenBy(t => t.Title, StringComparer.CurrentCulture)
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
        /// Số dự án và số đầu việc thuộc loại đã cho mà người này có trong tháng.
        ///
        /// CHỈ đếm theo đầu việc thật, không đếm theo dòng phân công dự án: bấm vào con số là mở
        /// danh sách công việc, nên con số phải đúng bằng số việc có trong danh sách đó. Trước đây
        /// đếm cả phân công nên người được phân vào dự án ở giai đoạn "cả hai" ra con số bằng nhau
        /// ở cả hai cột, bấm vào lại rỗng vì họ chưa có đầu việc nào.
        /// </summary>
        private static TeamProjectCount CountProjects(List<WorkTask> mine, int year, int month, string kind)
        {
            var tasks = mine
                .Where(t => t.ProjectId > 0
                            && t.Kind == kind
                            && t.State != TaskStates.Cancelled
                            && KpiService.TaskInMonth(t, year, month))
                .ToList();

            return new TeamProjectCount
            {
                Projects = tasks.Select(t => t.ProjectId).Distinct().Count(),
                Tasks = tasks.Count
            };
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

        /// <summary>Tổng số giờ logtime của cả tổ trong ngày hôm nay.</summary>
        public decimal TotalTodayLoggedHours
        {
            get { return Members.Sum(m => m.TodayLoggedHours); }
        }

        /// <summary>Tổng số việc đã hoàn thành hoặc đang làm của cả tổ trong ngày hôm nay.</summary>
        public int TotalWorkedTodayCount
        {
            get { return Members.Sum(m => m.WorkedToday.Count); }
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

        /// <summary>Việc hôm nay đang phải làm (trong hạn làm/chưa xong).</summary>
        public List<TeamTodayTask> Today { get; set; }

        /// <summary>Việc đã hoàn thành hoặc đang thực hiện trong hôm nay (kèm logtime).</summary>
        public List<TeamWorkedTodayTask> WorkedToday { get; set; }

        /// <summary>Tổng số giờ logtime của thành viên trong ngày hôm nay.</summary>
        public decimal TodayLoggedHours { get; set; }

        /// <summary>Số việc đã hoàn thành trong hôm nay.</summary>
        public int CompletedTodayCount
        {
            get { return WorkedToday.Count(t => t.IsCompletedToday); }
        }

        /// <summary>Số việc đang thực hiện trong hôm nay.</summary>
        public int InProgressTodayCount
        {
            get { return WorkedToday.Count(t => !t.IsCompletedToday); }
        }

        public KpiMonth Kpi { get; set; }

        /// <summary>
        /// Tổng điểm trừ trong tháng — cộng mức trừ trễ hạn của nhóm hỗ trợ và nhóm thực hiện
        /// (xem <see cref="KpiService.SupportLatePenalty"/>/<see cref="KpiService.ExecuteLatePenalty"/>).
        /// Việc riêng không có mức trừ, chỉ không được cộng điểm khi trễ/chưa xong nên không tính ở đây.
        /// </summary>
        public decimal TotalPenalty
        {
            get { return KpiService.SupportLatePenalty(Kpi.SupportLateCount) + KpiService.ExecuteLatePenalty(Kpi.ExecuteLateCount); }
        }

        /// <summary>Tổng số đầu việc trong tháng, gộp cả ba nhóm hỗ trợ/thực hiện/việc riêng.</summary>
        public int TotalTasks
        {
            get { return Kpi.SupportTotal + Kpi.ExecuteTotal + Kpi.AssignedTotal; }
        }

        /// <summary>Việc triển khai trong tháng: bao nhiêu dự án, bao nhiêu đầu việc.</summary>
        public TeamProjectCount Implement { get; set; }

        /// <summary>Việc hỗ trợ trong tháng.</summary>
        public TeamProjectCount Support { get; set; }

        public int OverdueToday
        {
            get { return Today.Count(t => t.IsOverdue); }
        }

        public TeamMemberRow()
        {
            Today = new List<TeamTodayTask>();
            WorkedToday = new List<TeamWorkedTodayTask>();
            Implement = new TeamProjectCount();
            Support = new TeamProjectCount();
        }
    }

    /// <summary>Số dự án và số đầu việc của một người theo một loại việc.</summary>
    public class TeamProjectCount
    {
        public int Projects { get; set; }
        public int Tasks { get; set; }
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

    /// <summary>Một việc trong danh sách "đã hoàn thành hoặc đang thực hiện hôm nay".</summary>
    public class TeamWorkedTodayTask
    {
        public int TaskId { get; set; }
        public string Title { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string State { get; set; }
        public int Progress { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsCompletedToday { get; set; }
        public decimal LoggedHoursToday { get; set; }
        public string TodayLogNote { get; set; }
    }
}
