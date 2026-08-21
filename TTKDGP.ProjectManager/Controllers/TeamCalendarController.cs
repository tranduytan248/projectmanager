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
    /// Lịch công tác Tổ — dành cho Quản lý Tổ và Quản trị viên để theo dõi toàn bộ công việc
    /// của các thành viên trong tổ phân bố theo ngày trong tháng, hỗ trợ lọc theo từng thành viên.
    /// Bảo vệ bởi quyền wteam.view.
    /// </summary>
    [AppAuthorize(Permission = "wteam.view")]
    public class TeamCalendarController : BaseController
    {
        private const int DueSoonDays = 7;
        private const int UpcomingLimit = 10;

        public ActionResult Index(int? year, int? month, int? userId, string state, string priority)
        {
            // Kiểm tra bảo đảm chỉ Quản lý Tổ hoặc Quản trị mới vào được
            if (!Can(Permissions.Team.Perm(Permissions.View)) && !IsTeamManager)
            {
                return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ hoặc Quản trị viên mới có quyền truy cập.");
            }

            var today = DateTime.Today;
            var y = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year.Value : today.Year;
            var m = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : today.Month;

            var members = WorkService.ActiveUsers();
            var usersDict = Repository.Users.All().ToDictionary(u => u.Id, u => u.FullName);
            var projectsDict = Repository.WorkProjects.All().ToDictionary(p => p.Id, p => p.Name);

            List<WorkTask> tasks;
            if (userId.HasValue && userId.Value > 0)
            {
                tasks = WorkService.TasksOfUser(userId.Value);
            }
            else
            {
                tasks = WorkService.AllTasks();
            }
            tasks = tasks.Where(t => t.State != TaskStates.Paused).ToList();

            // Điền tên người thực hiện và tên dự án để bản ghi đầy đủ
            foreach (var t in tasks)
            {
                if (string.IsNullOrEmpty(t.AssigneeName) && t.AssigneeUserId > 0 && usersDict.ContainsKey(t.AssigneeUserId))
                {
                    t.AssigneeName = usersDict[t.AssigneeUserId];
                }
                if (string.IsNullOrEmpty(t.ProjectName) && t.ProjectId > 0 && projectsDict.ContainsKey(t.ProjectId))
                {
                    t.ProjectName = projectsDict[t.ProjectId];
                }
            }

            if (!string.IsNullOrEmpty(state)) tasks = tasks.Where(t => t.State == state).ToList();
            if (!string.IsNullOrEmpty(priority)) tasks = tasks.Where(t => t.Priority == priority).ToList();

            var tasksInMonth = tasks.Where(t => KpiService.TaskInMonth(t, y, m)).ToList();
            var openInMonth = tasksInMonth.Where(t => !TaskStates.IsClosed(t.State)).ToList();

            var model = new TeamCalendarViewModel
            {
                Today = today,
                Year = y,
                Month = m,
                UserFilter = userId,
                StateFilter = state,
                PriorityFilter = priority,
                Members = members,

                TotalInMonth = tasksInMonth.Count,
                DoneCount = tasksInMonth.Count(t => t.State == TaskStates.Done),
                InProgressCount = openInMonth.Count(t => t.State == TaskStates.InProgress),
                OverdueCount = openInMonth.Count(t => t.IsOverdue),

                Weeks = BuildWeeks(y, m, tasks),
                Upcoming = BuildUpcoming(tasks, today)
            };

            return View(model);
        }

        /// <summary>
        /// Danh sách chi tiết toàn bộ công việc của một ngày cụ thể — mở trong modal khi bấm "+N việc khác" hoặc bấm vào ô ngày.
        /// </summary>
        public ActionResult DayTasks(string date, int? userId, string state, string priority)
        {
            if (!Can(Permissions.Team.Perm(Permissions.View)) && !IsTeamManager)
            {
                return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ hoặc Quản trị viên mới có quyền truy cập.");
            }

            DateTime targetDate;
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out targetDate))
            {
                if (!DateTime.TryParse(date, out targetDate))
                {
                    return HttpNotFound();
                }
            }

            List<WorkTask> tasks;
            if (userId.HasValue && userId.Value > 0)
            {
                tasks = WorkService.TasksOfUser(userId.Value);
            }
            else
            {
                tasks = WorkService.AllTasks();
            }
            tasks = tasks.Where(t => t.State != TaskStates.Paused).ToList();

            var usersDict = Repository.Users.All().ToDictionary(u => u.Id, u => u.FullName);
            var projectsDict = Repository.WorkProjects.All().ToDictionary(p => p.Id, p => p.Name);

            foreach (var t in tasks)
            {
                if (string.IsNullOrEmpty(t.AssigneeName) && t.AssigneeUserId > 0 && usersDict.ContainsKey(t.AssigneeUserId))
                {
                    t.AssigneeName = usersDict[t.AssigneeUserId];
                }
                if (string.IsNullOrEmpty(t.ProjectName) && t.ProjectId > 0 && projectsDict.ContainsKey(t.ProjectId))
                {
                    t.ProjectName = projectsDict[t.ProjectId];
                }
            }

            if (!string.IsNullOrEmpty(state)) tasks = tasks.Where(t => t.State == state).ToList();
            if (!string.IsNullOrEmpty(priority)) tasks = tasks.Where(t => t.Priority == priority).ToList();

            var dayTasks = tasks
                .Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == targetDate.Date)
                .OrderByDescending(t => t.IsOverdue)
                .ThenBy(t => t.AssigneeName, StringComparer.CurrentCulture)
                .ThenBy(t => t.Title, StringComparer.CurrentCulture)
                .ToList();

            ViewBag.Date = targetDate;
            ViewBag.UserFilter = userId;
            ViewBag.IsTeamView = true;

            return PartialView("_DayTasks", dayTasks);
        }

        private static int MondayOffset(DayOfWeek d)
        {
            return ((int)d + 6) % 7;
        }

        private static List<List<CalendarDayCell>> BuildWeeks(int year, int month, List<WorkTask> tasks)
        {
            var firstOfMonth = new DateTime(year, month, 1);
            var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
            var gridStart = firstOfMonth.AddDays(-MondayOffset(firstOfMonth.DayOfWeek));
            var gridEnd = lastOfMonth.AddDays(6 - MondayOffset(lastOfMonth.DayOfWeek));

            var byDueDate = tasks
                .Where(t => t.DueDate.HasValue)
                .GroupBy(t => t.DueDate.Value.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var weeks = new List<List<CalendarDayCell>>();
            for (var weekStart = gridStart; weekStart <= gridEnd; weekStart = weekStart.AddDays(7))
            {
                var week = new List<CalendarDayCell>();
                for (var i = 0; i < 7; i++)
                {
                    var day = weekStart.AddDays(i);
                    List<WorkTask> tasksOfDay;
                    byDueDate.TryGetValue(day, out tasksOfDay);
                    week.Add(new CalendarDayCell
                    {
                        Date = day,
                        InCurrentMonth = day.Month == month && day.Year == year,
                        Tasks = tasksOfDay ?? new List<WorkTask>()
                    });
                }
                weeks.Add(week);
            }
            return weeks;
        }

        private static List<WorkTask> BuildUpcoming(List<WorkTask> tasks, DateTime today)
        {
            var dueLimit = today.AddDays(DueSoonDays);
            return tasks
                .Where(t => !TaskStates.IsClockStopped(t.State))
                .Where(t => t.IsOverdue || (t.DueDate.HasValue && t.DueDate.Value.Date <= dueLimit))
                .OrderByDescending(t => t.IsOverdue)
                .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                .Take(UpcomingLimit)
                .ToList();
        }
    }

    public class TeamCalendarViewModel
    {
        public DateTime Today { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int? UserFilter { get; set; }
        public string StateFilter { get; set; }
        public string PriorityFilter { get; set; }

        public List<User> Members { get; set; }

        public int TotalInMonth { get; set; }
        public int DoneCount { get; set; }
        public int InProgressCount { get; set; }
        public int OverdueCount { get; set; }

        public List<List<CalendarDayCell>> Weeks { get; set; }
        public List<WorkTask> Upcoming { get; set; }

        public TeamCalendarViewModel()
        {
            Members = new List<User>();
            Weeks = new List<List<CalendarDayCell>>();
            Upcoming = new List<WorkTask>();
        }
    }
}
