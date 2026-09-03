using System;
using System.Collections.Generic;
using System.Linq;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Dòng hiển thị một lượt logtime cá nhân kèm thông tin task và project liên quan.</summary>
    public class MyTimesheetRow
    {
        public WorkTimeLog Log { get; set; }
        public WorkTask Task { get; set; }
        public WorkProject Project { get; set; }
        public bool CanDelete { get; set; }
        public string DeleteBlockedReason { get; set; }
    }

    /// <summary>Task đang mở mà nhân sự có thể chọn để ghi giờ công nhanh.</summary>
    public class OpenTaskOption
    {
        public int TaskId { get; set; }
        public string TaskCode { get; set; }
        public string TaskTitle { get; set; }
        public string ProjectName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? CapHours { get; set; }
        public decimal RemainingHours { get; set; }
    }

    /// <summary>ViewModel cho màn hình "Nhật ký giờ công của tôi".</summary>
    public class MyTimesheetViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime Today { get; set; }
        public bool IsCurrentMonth { get; set; }

        public decimal TotalHours { get; set; }
        public decimal RequiredHours { get; set; }
        public int StandardDays { get; set; }
        public decimal LeaveDays { get; set; }

        public int DaysMetTarget { get; set; } // >= 8h
        public int DaysUnderTarget { get; set; } // >0 & < 8h
        public int WorkingDaysNoLog { get; set; } // ngày làm việc 0h

        public List<MyTimesheetRow> Rows { get; set; }
        public List<OpenTaskOption> MyOpenTasks { get; set; }

        public MyTimesheetViewModel()
        {
            Rows = new List<MyTimesheetRow>();
            MyOpenTasks = new List<OpenTaskOption>();
        }
    }

    /// <summary>Dữ liệu chấm công của một ngày cho một nhân viên.</summary>
    public class TeamDayCell
    {
        public DateTime Date { get; set; }
        public int Day { get; set; }
        public decimal Hours { get; set; }
        public bool IsToday { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsHoliday { get; set; }
        public string HolidayName { get; set; }
        public bool IsLeave { get; set; }
        public string LeaveKind { get; set; }
        public decimal LeaveDays { get; set; }
        public int LogCount { get; set; }
    }

    /// <summary>Một hàng nhân viên trên Bảng chấm công cả tổ.</summary>
    public class TeamMemberTimesheetRow
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public decimal TotalHours { get; set; }
        public decimal RequiredHours { get; set; }
        public decimal LeaveDays { get; set; }
        public decimal AttendanceRate { get; set; }
        public Dictionary<int, TeamDayCell> Days { get; set; }

        public TeamMemberTimesheetRow()
        {
            Days = new Dictionary<int, TeamDayCell>();
        }
    }

    /// <summary>ViewModel cho màn hình "Bảng chấm công Tổ".</summary>
    public class TeamTimesheetViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime Today { get; set; }
        public int DaysInMonth { get; set; }

        public List<TeamMemberTimesheetRow> Members { get; set; }
        public Dictionary<int, decimal> TeamDailyTotal { get; set; } // Tổng giờ cả tổ theo ngày
        public decimal TeamTotalHours { get; set; }

        public TeamTimesheetViewModel()
        {
            Members = new List<TeamMemberTimesheetRow>();
            TeamDailyTotal = new Dictionary<int, decimal>();
        }
    }

    /// <summary>DTO xem chi tiết các đầu việc đã làm trong một ngày của một nhân viên.</summary>
    public class TimesheetDayDetailViewModel
    {
        public string UserFullName { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalHours { get; set; }
        public List<MyTimesheetRow> Entries { get; set; }

        public TimesheetDayDetailViewModel()
        {
            Entries = new List<MyTimesheetRow>();
        }
    }
}
