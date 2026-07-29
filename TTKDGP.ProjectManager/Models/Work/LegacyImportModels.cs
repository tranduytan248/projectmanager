using System.Collections.Generic;
using System.Linq;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Một dòng trong màn xem trước khi lấy dự án từ bộ cũ sang bộ mới.</summary>
    public class LegacyProjectRow
    {
        public const string Add = "Them";
        public const string Skip = "BoQua";

        /// <summary>Bản ghi dự án ở bộ cũ.</summary>
        public Project Source { get; set; }

        /// <summary>Bản ghi đã dựng sẵn cho bộ mới, chờ ghi. Null khi dòng bị bỏ qua.</summary>
        public WorkProject Prepared { get; set; }

        public string Outcome { get; set; }

        /// <summary>Lý do bỏ qua, hoặc ghi chú về chỗ ánh xạ chưa chắc chắn.</summary>
        public string Message { get; set; }

        /// <summary>Trạng thái cũ không nằm trong bảng ánh xạ nên phải đoán.</summary>
        public bool StatusGuessed { get; set; }

        /// <summary>Tên PM cũ; rỗng nếu dự án cũ chưa có PM.</summary>
        public string LegacyPmName { get; set; }

        /// <summary>Đã nối được PM cũ sang nhân sự bộ mới chưa.</summary>
        public bool PmMatched { get; set; }
    }

    /// <summary>Màn xem toàn bộ thông tin một dự án.</summary>
    public class ProjectDetailsViewModel
    {
        public WorkProject Project { get; set; }
        public bool CanEdit { get; set; }

        /// <summary>Mọi giai đoạn tham gia, kể cả người đã rời — đây là lịch sử nhân sự dự án.</summary>
        public List<WorkAssignment> Assignments { get; set; }

        public Services.TaskStat ChecklistStat { get; set; }
        public List<WorkTask> OverdueTasks { get; set; }

        public int SupportThisWeek { get; set; }
        public int Year { get; set; }
        public int Week { get; set; }

        public WorkWeekReport CurrentReport { get; set; }
        public List<WorkWeekReport> RecentReports { get; set; }

        public int ActiveCount { get { return Assignments.Count(a => a.IsActive); } }
        public int PastCount { get { return Assignments.Count(a => !a.IsActive); } }

        public ProjectDetailsViewModel()
        {
            Assignments = new List<WorkAssignment>();
            OverdueTasks = new List<WorkTask>();
            RecentReports = new List<WorkWeekReport>();
            ChecklistStat = new Services.TaskStat();
        }
    }

    public class LegacyImportViewModel
    {
        public List<LegacyProjectRow> Rows { get; set; }

        public int AddCount { get { return Rows.Count(r => r.Outcome == LegacyProjectRow.Add); } }
        public int SkipCount { get { return Rows.Count(r => r.Outcome == LegacyProjectRow.Skip); } }
        public int GuessedCount { get { return Rows.Count(r => r.Outcome == LegacyProjectRow.Add && r.StatusGuessed); } }
        public int PmMatchedCount { get { return Rows.Count(r => r.Outcome == LegacyProjectRow.Add && r.PmMatched); } }

        public int PmMissingCount
        {
            get
            {
                return Rows.Count(r => r.Outcome == LegacyProjectRow.Add
                                       && !r.PmMatched
                                       && !string.IsNullOrWhiteSpace(r.LegacyPmName));
            }
        }

        public bool CanConfirm { get { return AddCount > 0; } }

        public LegacyImportViewModel()
        {
            Rows = new List<LegacyProjectRow>();
        }
    }
}
