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
    /// Màn tổng quan: gom những gì một người cần biết ngay khi đăng nhập — việc của mình, việc
    /// sắp tới hạn, điểm KPI tháng, nghỉ phép — và thêm phần toàn Tổ cho người có quyền quản lý.
    ///
    /// Chỉ đọc và tổng hợp, không ghi gì. Riêng KPI phải dựng bằng <see cref="KpiService.Fill"/>
    /// chứ KHÔNG gọi CalculateUser: hàm đó lưu kết quả xuống bảng KpiMonth, mà một màn xem thì
    /// không được phép sửa dữ liệu chấm điểm.
    /// </summary>
    [AppAuthorize]
    public class DashboardController : BaseController
    {
        /// <summary>Số ngày tới được coi là "sắp đến hạn" trong danh sách việc cần làm.</summary>
        private const int DueSoonDays = 7;

        /// <summary>Số dòng tối đa cho mỗi danh sách gợi ý trên màn tổng quan.</summary>
        private const int ListLimit = 5;

        /// <summary>Số tháng gần đây vẽ trên biểu đồ xu hướng hoàn thành.</summary>
        private const int TrendMonths = 6;

        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Index()
        {
            var userId = CurrentUserId;
            var today = DateTime.Today;

            // Nạp một lần rồi ghép trong bộ nhớ — mọi phần bên dưới đều dùng chung danh sách này.
            var allTasks = WorkService.AllTasks();
            var mine = WorkService.TasksOfUser(userId, allTasks);

            var model = new DashboardViewModel
            {
                UserFullName = CurrentUser == null ? "" : CurrentUser.FullName,
                Today = today,
                Year = today.Year,
                Month = today.Month,

                MyTasks = BuildMyTasks(mine, today),
                MyKpi = BuildMyKpi(userId, today),
                MyLeave = BuildMyLeave(userId, today),
                StateChart = BuildStateChart(mine),
                TrendChart = BuildTrendChart(mine, today),

                CanSeeTeam = IsTeamManager,
                CanApproveLeave = Can(Permissions.Leaves.Perm("approve"))
            };

            if (model.CanSeeTeam)
            {
                model.Team = BuildTeam(allTasks, userId);
            }

            if (model.CanApproveLeave)
            {
                model.PendingLeaveCount = LeaveService.PendingCount();
            }

            return View(model);
        }

        /// <summary>
        /// Việc của cá nhân: đếm theo trạng thái và lấy vài việc đáng làm trước.
        ///
        /// "Đáng làm trước" xếp theo: quá hạn lâu nhất, rồi đến hạn gần nhất. Việc không có hạn
        /// xuống cuối — không có hạn thì không thể nói là gấp.
        /// </summary>
        private static DashboardMyTasks BuildMyTasks(List<WorkTask> mine, DateTime today)
        {
            var open = mine.Where(t => !TaskStates.IsClosed(t.State)).ToList();
            var dueLimit = today.AddDays(DueSoonDays);

            var result = new DashboardMyTasks
            {
                OpenCount = open.Count,
                InProgressCount = open.Count(t => t.State == TaskStates.InProgress),
                NotStartedCount = open.Count(t => t.State == TaskStates.NotStarted),
                OverdueCount = open.Count(t => t.IsOverdue),
                DoneCount = mine.Count(t => t.State == TaskStates.Done)
            };

            result.DueSoonCount = open.Count(t => !t.IsOverdue && t.DueDate.HasValue
                                                  && t.DueDate.Value.Date <= dueLimit);

            result.Focus = open
                .Where(t => t.IsOverdue || (t.DueDate.HasValue && t.DueDate.Value.Date <= dueLimit))
                .OrderByDescending(t => t.IsOverdue)
                .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                .Take(ListLimit)
                .ToList();

            return result;
        }

        /// <summary>
        /// Việc của cá nhân tách theo trạng thái, cho biểu đồ tròn. Giữ đúng thứ tự của
        /// <see cref="TaskStates.All"/> để màu của mỗi trạng thái không đổi giữa các lần xem.
        /// </summary>
        private static DashboardChart BuildStateChart(List<WorkTask> mine)
        {
            var chart = new DashboardChart();

            foreach (var state in TaskStates.All)
            {
                var count = mine.Count(t => t.State == state);
                if (count == 0) continue;

                chart.Labels.Add(TaskStates.Display(state));
                chart.Values.Add(count);
            }

            return chart;
        }

        /// <summary>
        /// Số việc hoàn thành theo từng tháng trong nửa năm gần đây, cho biểu đồ đường.
        /// Mốc tính là <see cref="WorkTask.CompletedAt"/> — thời điểm việc thật sự xong.
        /// </summary>
        private static DashboardChart BuildTrendChart(List<WorkTask> mine, DateTime today)
        {
            var chart = new DashboardChart();
            var done = mine.Where(t => t.CompletedAt.HasValue).ToList();

            for (var back = TrendMonths - 1; back >= 0; back--)
            {
                var month = today.AddMonths(-back);

                chart.Labels.Add(month.ToString("MM/yyyy"));
                chart.Values.Add(done.Count(t => t.CompletedAt.Value.Year == month.Year
                                                 && t.CompletedAt.Value.Month == month.Month));
            }

            return chart;
        }

        /// <summary>
        /// Điểm KPI tháng đang diễn ra, tính tại chỗ để không phải chờ tới lúc chốt tháng mới thấy.
        /// Dùng bản đã chốt trong bảng nếu có, chưa có thì dựng tạm trong bộ nhớ.
        /// </summary>
        private static KpiMonth BuildMyKpi(int userId, DateTime today)
        {
            if (userId <= 0) return null;

            var saved = Repository.KpiMonths.FirstOrDefault(
                k => k.Year == today.Year && k.Month == today.Month && k.UserId == userId);
            if (saved != null) return saved;

            var preview = new KpiMonth
            {
                Year = today.Year,
                Month = today.Month,
                UserId = userId,
                UserFullName = WorkService.UserFullName(userId)
            };

            KpiService.Fill(preview, KpiService.TasksOfUserInMonth(userId, today.Year, today.Month));
            return preview;
        }

        /// <summary>Nghỉ phép của cá nhân trong tháng: đã nghỉ bao nhiêu và còn đơn nào chờ duyệt.</summary>
        private static DashboardMyLeave BuildMyLeave(int userId, DateTime today)
        {
            var result = new DashboardMyLeave
            {
                ApprovedDays = LeaveService.ApprovedDays(userId, today.Year, today.Month)
            };

            var ofUser = LeaveService.OfUser(userId);
            result.PendingCount = ofUser.Count(l => l.State == LeaveStates.Pending);

            // Đơn đã duyệt cho những ngày sắp tới — để người dùng nhớ mình sắp nghỉ.
            result.Upcoming = ofUser
                .Where(l => l.IsApproved && l.FromDate.Date >= today)
                .OrderBy(l => l.FromDate)
                .Take(ListLimit)
                .ToList();

            return result;
        }

        /// <summary>
        /// Phần toàn Tổ, chỉ dựng khi tài khoản là Quản lý Tổ. Bám theo cách màn "Dự án của tôi"
        /// ghép dữ liệu: nạp cả bảng một lần rồi tra trong bộ nhớ, không gọi Find trong vòng lặp.
        /// </summary>
        private static DashboardTeam BuildTeam(List<WorkTask> allTasks, int userId)
        {
            var projects = Repository.WorkProjects.All();
            var stats = WorkService.StatsByProject(allTasks);
            var openProjects = projects.Where(p => ProjectStates.IsOpen(p.State)).ToList();

            var result = new DashboardTeam
            {
                OpenProjectCount = openProjects.Count,
                MyPmCount = openProjects.Count(p => p.PmUserId == userId),
                OverdueTaskCount = allTasks.Count(t => t.IsOverdue),
                OpenTaskCount = allTasks.Count(t => !TaskStates.IsClosed(t.State))
            };

            // Dự án đang chạy có nhiều việc quá hạn nhất — chỗ cần can thiệp trước.
            result.Projects = openProjects
                .Select(p => new DashboardProjectRow
                {
                    Project = p,
                    Stat = StatOf(stats, p.Id),
                    OverdueCount = allTasks.Count(t => t.ProjectId == p.Id && t.IsOverdue)
                })
                .OrderByDescending(r => r.OverdueCount)
                .ThenBy(r => r.Stat.Percent)
                .ThenBy(r => r.Project.Name, StringComparer.CurrentCulture)
                .Take(ListLimit)
                .ToList();

            return result;
        }

        /// <summary>Số liệu của một dự án; dự án chưa có việc nào thì trả về bộ đếm rỗng.</summary>
        private static TaskStat StatOf(Dictionary<int, TaskStat> stats, int projectId)
        {
            TaskStat stat;
            return stats.TryGetValue(projectId, out stat) ? stat : new TaskStat();
        }
    }

    /// <summary>Màn tổng quan. Phần nào không có quyền xem thì để null, view tự bỏ qua.</summary>
    public class DashboardViewModel
    {
        public string UserFullName { get; set; }
        public DateTime Today { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }

        public DashboardMyTasks MyTasks { get; set; }

        /// <summary>KPI tháng đang diễn ra; null khi chưa đăng nhập.</summary>
        public KpiMonth MyKpi { get; set; }

        public DashboardMyLeave MyLeave { get; set; }

        /// <summary>Việc tách theo trạng thái — biểu đồ tròn.</summary>
        public DashboardChart StateChart { get; set; }

        /// <summary>Số việc hoàn thành theo tháng — biểu đồ đường.</summary>
        public DashboardChart TrendChart { get; set; }

        /// <summary>Phần toàn Tổ; null khi tài khoản không phải Quản lý Tổ.</summary>
        public DashboardTeam Team { get; set; }

        public bool CanSeeTeam { get; set; }
        public bool CanApproveLeave { get; set; }

        /// <summary>Số đơn nghỉ phép toàn hệ thống đang chờ duyệt; chỉ có nghĩa khi được duyệt phép.</summary>
        public int PendingLeaveCount { get; set; }

        /// <summary>
        /// Số giờ còn thiếu so với yêu cầu của tháng; 0 khi đã đủ. Thiếu giờ thì KPI bị nhân theo
        /// tỷ lệ ngày công, nên đây là con số người dùng cần biết để còn kịp bù.
        /// </summary>
        public decimal HoursShort
        {
            get
            {
                if (MyKpi == null) return 0;

                var missing = MyKpi.RequiredHours - MyKpi.WorkedHours;
                return missing > 0 ? Math.Round(missing, 1) : 0;
            }
        }

        /// <summary>Số giờ còn thiếu quy ra ngày công, cho dễ hình dung.</summary>
        public decimal DaysShort
        {
            get { return Math.Round(HoursShort / KpiService.HoursPerDay, 1); }
        }

        /// <summary>Phần trăm giờ đã làm so với yêu cầu, chặn trên 100 để thanh không tràn.</summary>
        public int HoursPercent
        {
            get
            {
                if (MyKpi == null || MyKpi.RequiredHours <= 0) return 100;

                var percent = (int)Math.Round(MyKpi.WorkedHours * 100 / MyKpi.RequiredHours);
                return percent > 100 ? 100 : percent;
            }
        }

        public DashboardViewModel()
        {
            MyTasks = new DashboardMyTasks();
            MyLeave = new DashboardMyLeave();
        }
    }

    /// <summary>
    /// Một bộ số liệu để vẽ biểu đồ: nhãn và giá trị đi theo cặp cùng chỉ số.
    /// View tự chuyển sang JSON cho Chart.js.
    /// </summary>
    public class DashboardChart
    {
        public List<string> Labels { get; set; }
        public List<int> Values { get; set; }

        /// <summary>Không có số liệu thì view hiện dòng chữ thay vì khung biểu đồ trống.</summary>
        public bool HasData
        {
            get { return Values != null && Values.Any(v => v > 0); }
        }

        public DashboardChart()
        {
            Labels = new List<string>();
            Values = new List<int>();
        }
    }

    /// <summary>Việc của cá nhân trên màn tổng quan.</summary>
    public class DashboardMyTasks
    {
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int NotStartedCount { get; set; }
        public int OverdueCount { get; set; }
        public int DueSoonCount { get; set; }
        public int DoneCount { get; set; }

        /// <summary>Vài việc nên xử lý trước: quá hạn hoặc sắp tới hạn.</summary>
        public List<WorkTask> Focus { get; set; }

        public DashboardMyTasks()
        {
            Focus = new List<WorkTask>();
        }
    }

    /// <summary>Nghỉ phép của cá nhân trên màn tổng quan.</summary>
    public class DashboardMyLeave
    {
        public decimal ApprovedDays { get; set; }
        public int PendingCount { get; set; }
        public List<LeaveRequest> Upcoming { get; set; }

        public DashboardMyLeave()
        {
            Upcoming = new List<LeaveRequest>();
        }
    }

    /// <summary>Phần toàn Tổ, chỉ dựng cho Quản lý Tổ.</summary>
    public class DashboardTeam
    {
        public int OpenProjectCount { get; set; }
        public int MyPmCount { get; set; }
        public int OpenTaskCount { get; set; }
        public int OverdueTaskCount { get; set; }

        /// <summary>Dự án cần chú ý trước, nhiều việc quá hạn nhất lên đầu.</summary>
        public List<DashboardProjectRow> Projects { get; set; }

        public DashboardTeam()
        {
            Projects = new List<DashboardProjectRow>();
        }
    }

    /// <summary>Một dự án trong bảng "Dự án cần chú ý".</summary>
    public class DashboardProjectRow
    {
        public WorkProject Project { get; set; }
        public TaskStat Stat { get; set; }
        public int OverdueCount { get; set; }
    }
}
