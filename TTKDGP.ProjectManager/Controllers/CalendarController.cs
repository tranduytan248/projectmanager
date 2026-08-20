using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Lịch công việc cá nhân theo tháng — xem việc CỦA CHÍNH MÌNH phân bố theo ngày trong một
    /// tháng, đổi tháng qua URL (?year=&amp;month=). Mở cho MỌI tài khoản đã đăng nhập, không xét
    /// quyền "wtasks.view" như "Công việc của tôi": đây thuần là dữ liệu cá nhân của người xem,
    /// không phải chức năng nghiệp vụ cần cấp quyền riêng — xem Memory.md mục "Lịch công việc
    /// cá nhân". Dùng lại đúng quy tắc đã có toàn hệ thống (KpiService.TaskInMonth, IsOverdue,
    /// ngưỡng "sắp tới hạn" của DashboardController) để không lệch số liệu với các màn khác.
    /// </summary>
    [AppAuthorize]
    public class CalendarController : BaseController
    {
        /// <summary>Số ngày tới được coi là "sắp đến hạn" — khớp DashboardController.</summary>
        private const int DueSoonDays = 7;

        /// <summary>Số dòng tối đa cho "Công việc sắp tới" — khớp DashboardController.</summary>
        private const int UpcomingLimit = 5;

        public ActionResult Index(int? year, int? month, string state, string priority)
        {
            var userId = CurrentUserId;
            var today = DateTime.Today;

            // Tháng lạ lùi về tháng hiện tại chứ không báo lỗi — khớp DashboardController.Index.
            var y = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year.Value : today.Year;
            var m = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : today.Month;

            var mine = WorkService.TasksOfUser(userId);

            if (!string.IsNullOrEmpty(state)) mine = mine.Where(t => t.State == state).ToList();
            if (!string.IsNullOrEmpty(priority)) mine = mine.Where(t => t.Priority == priority).ToList();

            var mineInMonth = mine.Where(t => KpiService.TaskInMonth(t, y, m)).ToList();
            var openInMonth = mineInMonth.Where(t => !TaskStates.IsClosed(t.State)).ToList();

            var model = new CalendarViewModel
            {
                Today = today,
                Year = y,
                Month = m,
                StateFilter = state,
                PriorityFilter = priority,

                TotalInMonth = mineInMonth.Count,
                DoneCount = mineInMonth.Count(t => t.State == TaskStates.Done),
                InProgressCount = openInMonth.Count(t => t.State == TaskStates.InProgress),
                OverdueCount = openInMonth.Count(t => t.IsOverdue),

                Weeks = BuildWeeks(y, m, mine),
                Upcoming = BuildUpcoming(mine, today)
            };

            return View(model);
        }

        /// <summary>Số ngày lệch từ Thứ Hai (0) tới Chủ Nhật (6) — lịch trong dự án luôn bắt đầu
        /// tuần bằng Thứ Hai, khớp WeekHelper dùng cho tuần báo cáo.</summary>
        private static int MondayOffset(DayOfWeek d)
        {
            return ((int)d + 6) % 7;
        }

        /// <summary>
        /// Dựng lưới các tuần phủ đủ tháng đang xem (có thể dư vài ngày đầu/cuối của tháng trước/
        /// sau để tuần luôn đủ 7 ngày). Một việc CHỈ được đặt vào đúng ô ngày trùng Hạn hoàn thành
        /// (DueDate) — việc không có hạn (chỉ tính vào tháng qua Tuần báo cáo/Mốc hoàn thành, xem
        /// KpiService.TaskInMonth) không có một ngày cụ thể để đặt lên lưới, nhưng vẫn được tính
        /// vào 4 thẻ thống kê phía trên.
        /// </summary>
        private static List<List<CalendarDayCell>> BuildWeeks(int year, int month, List<WorkTask> mine)
        {
            var firstOfMonth = new DateTime(year, month, 1);
            var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
            var gridStart = firstOfMonth.AddDays(-MondayOffset(firstOfMonth.DayOfWeek));
            var gridEnd = lastOfMonth.AddDays(6 - MondayOffset(lastOfMonth.DayOfWeek));

            var byDueDate = mine
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

        /// <summary>Y hệt DashboardController.BuildMyTasks phần "Focus" — quá hạn trước, rồi đến
        /// hạn gần nhất; việc không có hạn không lọt vào danh sách này (không thể nói là gấp).
        /// Tính theo hôm nay, KHÔNG theo tháng đang xem, nên vẫn đúng dù đang xem tháng khác.</summary>
        private static List<WorkTask> BuildUpcoming(List<WorkTask> mine, DateTime today)
        {
            var dueLimit = today.AddDays(DueSoonDays);
            return mine
                .Where(t => !TaskStates.IsClockStopped(t.State))
                .Where(t => t.IsOverdue || (t.DueDate.HasValue && t.DueDate.Value.Date <= dueLimit))
                .OrderByDescending(t => t.IsOverdue)
                .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                .Take(UpcomingLimit)
                .ToList();
        }
    }

    public class CalendarViewModel
    {
        public DateTime Today { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string StateFilter { get; set; }
        public string PriorityFilter { get; set; }

        public int TotalInMonth { get; set; }
        public int DoneCount { get; set; }
        public int InProgressCount { get; set; }
        public int OverdueCount { get; set; }

        public List<List<CalendarDayCell>> Weeks { get; set; }
        public List<WorkTask> Upcoming { get; set; }
    }

    public class CalendarDayCell
    {
        public DateTime Date { get; set; }
        public bool InCurrentMonth { get; set; }
        public List<WorkTask> Tasks { get; set; }
    }
}
