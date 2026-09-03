using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Trung tâm Nhật ký Giờ công & Bảng chấm công:
    /// - Index: Nhật ký giờ công của chính mình (ai đăng nhập cũng xem được)
    /// - QuickLog: Ghi giờ công nhanh
    /// - DeleteLog: Xoá lượt ghi giờ công của mình
    /// - Team: Bảng ma trận chấm công cả Tổ (chỉ Quản lý Tổ có quyền wteam.view)
    /// - DayDetail: Xem chi tiết các đầu việc đã làm trong một ngày của nhân viên
    /// </summary>
    [AppAuthorize]
    public class TimesheetController : BaseController
    {
        // ----------------------------------------------------
        // 1. NHẬT KÝ GIỜ CÔNG CÁ NHÂN
        // ----------------------------------------------------

        [HttpGet]
        public ActionResult Index(int? year, int? month)
        {
            var userId = CurrentUserId;
            var today = DateTime.Today;

            var y = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year.Value : today.Year;
            var m = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : today.Month;
            var viewMonth = new DateTime(y, m, 1);
            var endMonth = viewMonth.AddMonths(1);

            // Nạp toàn bộ log của user trong tháng
            var logs = Repository.WorkTimeLogs.All()
                .Where(l => l.UserId == userId && l.WorkDate >= viewMonth && l.WorkDate < endMonth)
                .OrderByDescending(l => l.WorkDate)
                .ThenByDescending(l => l.Id)
                .ToList();

            var taskIds = logs.Select(l => l.TaskId).Distinct().ToList();
            var tasks = Repository.WorkTasks.All().Where(t => taskIds.Contains(t.Id)).ToDictionary(t => t.Id, t => t);

            var projectIds = tasks.Values.Select(t => t.ProjectId).Distinct().ToList();
            var projects = Repository.WorkProjects.All().Where(p => projectIds.Contains(p.Id)).ToDictionary(p => p.Id, p => p);

            var rows = new List<MyTimesheetRow>();
            foreach (var log in logs)
            {
                WorkTask task;
                tasks.TryGetValue(log.TaskId, out task);

                WorkProject project = null;
                if (task != null)
                {
                    projects.TryGetValue(task.ProjectId, out project);
                }

                bool canDelete = true;
                string deleteBlockedReason = null;

                if (task != null)
                {
                    if (task.State == TaskStates.Cancelled)
                    {
                        canDelete = false;
                        deleteBlockedReason = "Công việc đã huỷ.";
                    }
                    else if (task.State == TaskStates.InProgress)
                    {
                        canDelete = false;
                        deleteBlockedReason = "Việc đang \"Đang làm\" nên không xoá được giờ đã ghi.";
                    }
                }

                rows.Add(new MyTimesheetRow
                {
                    Log = log,
                    Task = task,
                    Project = project,
                    CanDelete = canDelete,
                    DeleteBlockedReason = deleteBlockedReason
                });
            }

            // Tính thống kê
            var standardDays = KpiService.StandardWorkingDays(y, m);
            var leaveDays = LeaveService.ApprovedDays(userId, y, m);
            var requiredHours = (standardDays - leaveDays) * KpiService.HoursPerDay;
            var totalHours = logs.Sum(l => l.Hours);

            // Nhóm theo ngày để đếm
            var byDay = logs.GroupBy(l => l.WorkDate.Date).ToDictionary(g => g.Key, g => g.Sum(l => l.Hours));
            int daysMetTarget = 0;
            int daysUnderTarget = 0;

            foreach (var kvp in byDay)
            {
                if (kvp.Value >= 8.0m) daysMetTarget++;
                else if (kvp.Value > 0) daysUnderTarget++;
            }

            // Đếm ngày làm việc chưa ghi giờ
            int daysInMonth = DateTime.DaysInMonth(y, m);
            int workingDaysNoLog = 0;
            var limitDay = (y == today.Year && m == today.Month) ? today.Day : daysInMonth;

            // Danh sách đơn nghỉ phép trong tháng
            var myLeaves = Repository.LeaveRequests.All()
                .Where(l => l.UserId == userId && l.State == LeaveStates.Approved && l.OverlapsRange(viewMonth, endMonth.AddDays(-1)))
                .ToList();

            for (int d = 1; d <= limitDay; d++)
            {
                var checkDate = new DateTime(y, m, d);
                if (checkDate.DayOfWeek == DayOfWeek.Saturday || checkDate.DayOfWeek == DayOfWeek.Sunday) continue;
                if (HolidayService.IsHoliday(checkDate)) continue;

                if (!byDay.ContainsKey(checkDate) || byDay[checkDate] == 0m)
                {
                    bool isLeave = myLeaves.Any(l => checkDate >= l.FromDate.Date && checkDate <= l.ToDate.Date);
                    if (!isLeave)
                    {
                        workingDaysNoLog++;
                    }
                }
            }

            // Danh sách task đang mở của user để chọn ghi giờ nhanh
            var allMyTasks = WorkService.TasksOfUser(userId);
            var openTasks = allMyTasks
                .Where(t => !TaskStates.IsClosed(t.State) && t.State != TaskStates.Cancelled)
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .ToList();

            var openTaskOptions = new List<OpenTaskOption>();
            var allProjects = Repository.WorkProjects.All().ToDictionary(p => p.Id, p => p);

            foreach (var t in openTasks)
            {
                var cap = TimeLogRules.TaskCap(t.StartDate, t.DueDate);
                var rem = TimeLogService.RemainingOfTask(t) ?? 0m;
                WorkProject p = null;
                allProjects.TryGetValue(t.ProjectId, out p);

                openTaskOptions.Add(new OpenTaskOption
                {
                    TaskId = t.Id,
                    TaskCode = t.Code,
                    TaskTitle = t.Title,
                    ProjectName = p != null ? p.Name : "",
                    StartDate = t.StartDate,
                    DueDate = t.DueDate,
                    CapHours = cap,
                    RemainingHours = rem
                });
            }

            var model = new MyTimesheetViewModel
            {
                Year = y,
                Month = m,
                Today = today,
                IsCurrentMonth = (y == today.Year && m == today.Month),
                TotalHours = totalHours,
                RequiredHours = requiredHours,
                StandardDays = standardDays,
                LeaveDays = leaveDays,
                DaysMetTarget = daysMetTarget,
                DaysUnderTarget = daysUnderTarget,
                WorkingDaysNoLog = workingDaysNoLog,
                Rows = rows,
                MyOpenTasks = openTaskOptions
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QuickLog(int taskId, string workDate, string hours, string note)
        {
            var task = Repository.WorkTasks.Find(taskId);
            if (task == null)
            {
                return Json(new { ok = false, message = "Không tìm thấy công việc." });
            }

            DateTime dateValue;
            if (!DateTime.TryParseExact((workDate ?? string.Empty).Trim(), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
            {
                return Json(new { ok = false, message = "Ngày làm không hợp lệ (định dạng yyyy-MM-dd)." });
            }

            decimal hoursValue;
            if (!decimal.TryParse((hours ?? string.Empty).Trim().Replace(',', '.'),
                    NumberStyles.Number, CultureInfo.InvariantCulture, out hoursValue))
            {
                return Json(new { ok = false, message = "Số giờ không hợp lệ. Ví dụ: 2 hoặc 2.5." });
            }

            if (CurrentUserId <= 0 || task.AssigneeUserId != CurrentUserId)
            {
                return Json(new { ok = false, message = "Chỉ người được giao việc mới ghi được giờ công." });
            }

            if (task.State == TaskStates.Cancelled)
            {
                return Json(new { ok = false, message = "Công việc đã huỷ nên không ghi thêm giờ được." });
            }

            var error = TimeLogService.Add(task, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName, dateValue, hoursValue, note);

            if (error != null)
            {
                return Json(new { ok = false, message = error });
            }

            TaskActivityLogService.Record(task.Id, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName, TaskActivityActions.TimeLogAdded,
                string.Format("Đã ghi {0} giờ ({1:dd/MM/yyyy})", hoursValue, dateValue));

            return Json(new { ok = true, message = string.Format("Đã ghi nhận {0:0.#} giờ công thành công.", hoursValue) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteLog(int logId)
        {
            var log = Repository.WorkTimeLogs.Find(logId);
            if (log == null)
            {
                return Json(new { ok = false, message = "Không tìm thấy bản ghi giờ công." });
            }

            if (CurrentUserId <= 0 || log.UserId != CurrentUserId)
            {
                return Json(new { ok = false, message = "Chỉ xoá được dòng giờ của chính bạn." });
            }

            var task = Repository.WorkTasks.Find(log.TaskId);
            if (task != null)
            {
                if (task.State == TaskStates.Cancelled)
                {
                    return Json(new { ok = false, message = "Công việc đã huỷ nên không sửa được giờ đã ghi." });
                }

                if (task.State == TaskStates.InProgress)
                {
                    return Json(new { ok = false, message = "Việc đang \"Đang làm\" nên không xoá được giờ đã ghi — xoá sẽ khiến việc mất hết căn cứ giờ công trong khi vẫn đang ở trạng thái này." });
                }
            }

            Repository.WorkTimeLogs.Delete(logId);

            if (task != null)
            {
                TaskActivityLogService.Record(task.Id, CurrentUserId,
                    CurrentUser == null ? null : CurrentUser.FullName, TaskActivityActions.TimeLogDeleted,
                    string.Format("Đã xoá lượt ghi {0} giờ ({1:dd/MM/yyyy})", log.Hours, log.WorkDate));
            }

            return Json(new { ok = true, message = "Đã xoá lượt ghi giờ công." });
        }

        // ----------------------------------------------------
        // 2. BẢNG CHẤM CÔNG CẢ TỔ (QUẢN LÝ TỔ)
        // ----------------------------------------------------

        [HttpGet]
        [AppAuthorize(Permission = "wteam.view")]
        public ActionResult Team(int? year, int? month)
        {
            var today = DateTime.Today;
            var y = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year.Value : today.Year;
            var m = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : today.Month;
            var viewMonth = new DateTime(y, m, 1);
            var endMonth = viewMonth.AddMonths(1);
            var daysInMonth = DateTime.DaysInMonth(y, m);

            var users = WorkService.TrackedUsers();
            var allUserIds = users.Select(u => u.Id).ToList();

            // Nạp toàn bộ log của các user trong tháng
            var logs = Repository.WorkTimeLogs.All()
                .Where(l => allUserIds.Contains(l.UserId) && l.WorkDate >= viewMonth && l.WorkDate < endMonth)
                .ToList();

            // Nạp toàn bộ đơn nghỉ phép đã duyệt trong tháng
            var leaves = Repository.LeaveRequests.All()
                .Where(l => allUserIds.Contains(l.UserId) && l.State == LeaveStates.Approved && l.OverlapsRange(viewMonth, endMonth.AddDays(-1)))
                .ToList();

            var standardDays = KpiService.StandardWorkingDays(y, m);

            var model = new TeamTimesheetViewModel
            {
                Year = y,
                Month = m,
                Today = today,
                DaysInMonth = daysInMonth
            };

            for (int d = 1; d <= daysInMonth; d++)
            {
                model.TeamDailyTotal[d] = 0m;
            }

            foreach (var user in users)
            {
                var userLogs = logs.Where(l => l.UserId == user.Id).ToList();
                var userLeaves = leaves.Where(l => l.UserId == user.Id).ToList();

                var row = new TeamMemberTimesheetRow
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName
                };

                decimal userTotalMonth = 0m;

                for (int d = 1; d <= daysInMonth; d++)
                {
                    var curDate = new DateTime(y, m, d);
                    var dayLogs = userLogs.Where(l => l.WorkDate.Date == curDate).ToList();
                    var dayHours = dayLogs.Sum(l => l.Hours);
                    userTotalMonth += dayHours;
                    model.TeamDailyTotal[d] += dayHours;

                    var isWeekend = curDate.DayOfWeek == DayOfWeek.Saturday || curDate.DayOfWeek == DayOfWeek.Sunday;
                    var isHoliday = HolidayService.IsHoliday(curDate);
                    var matchingLeave = userLeaves.FirstOrDefault(l => curDate >= l.FromDate.Date && curDate <= l.ToDate.Date);

                    row.Days[d] = new TeamDayCell
                    {
                        Date = curDate,
                        Day = d,
                        Hours = dayHours,
                        IsToday = (curDate == today),
                        IsWeekend = isWeekend,
                        IsHoliday = isHoliday,
                        IsLeave = (matchingLeave != null),
                        LeaveKind = matchingLeave != null ? LeaveKinds.Display(matchingLeave.Kind) : null,
                        LeaveDays = matchingLeave != null ? matchingLeave.Days : 0m,
                        LogCount = dayLogs.Count
                    };
                }

                var userLeaveDays = LeaveService.ApprovedDays(user.Id, y, m);
                var requiredHours = (standardDays - userLeaveDays) * KpiService.HoursPerDay;
                var rate = requiredHours > 0 ? Math.Round(userTotalMonth * 100 / requiredHours, 1) : 100m;

                row.TotalHours = userTotalMonth;
                row.LeaveDays = userLeaveDays;
                row.RequiredHours = requiredHours;
                row.AttendanceRate = rate > 100m ? 100m : rate;

                model.Members.Add(row);
            }

            model.TeamTotalHours = model.TeamDailyTotal.Values.Sum();

            return View(model);
        }

        [HttpGet]
        public ActionResult DayDetail(int userId, string date)
        {
            // Kiểm tra quyền: chỉ xem của mình, hoặc Quản lý Tổ mới được xem của người khác
            if (userId != CurrentUserId && !Can(Permissions.Team.Perm(Permissions.View)))
            {
                return HttpNotFound();
            }

            DateTime targetDate;
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out targetDate))
            {
                return HttpNotFound();
            }

            var logs = Repository.WorkTimeLogs.All()
                .Where(l => l.UserId == userId && l.WorkDate.Date == targetDate.Date)
                .ToList();

            var taskIds = logs.Select(l => l.TaskId).Distinct().ToList();
            var tasks = Repository.WorkTasks.All().Where(t => taskIds.Contains(t.Id)).ToDictionary(t => t.Id, t => t);

            var projectIds = tasks.Values.Select(t => t.ProjectId).Distinct().ToList();
            var projects = Repository.WorkProjects.All().Where(p => projectIds.Contains(p.Id)).ToDictionary(p => p.Id, p => p);

            var entries = new List<MyTimesheetRow>();
            foreach (var log in logs)
            {
                WorkTask task;
                tasks.TryGetValue(log.TaskId, out task);

                WorkProject project = null;
                if (task != null)
                {
                    projects.TryGetValue(task.ProjectId, out project);
                }

                entries.Add(new MyTimesheetRow
                {
                    Log = log,
                    Task = task,
                    Project = project
                });
            }

            var user = Repository.Users.Find(userId);

            var model = new TimesheetDayDetailViewModel
            {
                UserFullName = user != null ? user.FullName : ("Nhân sự #" + userId),
                Date = targetDate,
                TotalHours = logs.Sum(l => l.Hours),
                Entries = entries
            };

            return PartialView("_DayDetailModal", model);
        }
    }
}
