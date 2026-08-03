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
                    Today = BuildToday(mine, today, y, m, byProject),
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
}
