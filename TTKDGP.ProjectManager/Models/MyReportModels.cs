using System;
using System.Collections.Generic;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Một phân công của chính người đăng nhập, kèm nhật ký của tuần đang xem.</summary>
    public class MyReportRow
    {
        public int AssignmentId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Customer { get; set; }

        /// <summary>Vai trò trong dự án.</summary>
        public string Role { get; set; }

        /// <summary>Trạng thái tham gia hiện tại của phân công.</summary>
        public string WorkStatus { get; set; }

        /// <summary>Id nhật ký của tuần đang xem; 0 nghĩa là tuần này chưa báo cáo.</summary>
        public int LogId { get; set; }

        /// <summary>Nội dung đã báo cáo cho tuần đang xem (rỗng nếu chưa có).</summary>
        public string Content { get; set; }

        /// <summary>Trạng thái ghi trong nhật ký tuần đang xem.</summary>
        public string Status { get; set; }

        public DateTime? ReportedAt { get; set; }

        public bool HasReport { get { return LogId > 0; } }
    }

    /// <summary>Màn hình "Báo cáo của tôi": các phân công đang thực hiện/hỗ trợ theo từng tuần.</summary>
    public class MyReportViewModel
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public string WeekLabel { get; set; }
        public DateTime WeekFrom { get; set; }
        public DateTime WeekTo { get; set; }

        /// <summary>Đang xem đúng tuần hiện tại hay không.</summary>
        public bool IsCurrentWeek { get; set; }

        public string MemberName { get; set; }

        /// <summary>Tài khoản chưa được gắn nhân sự — không biết báo cáo cho phân công nào.</summary>
        public bool NotLinked { get; set; }

        public List<MyReportRow> Rows { get; set; }

        /// <summary>Danh sách trạng thái cho ô chọn khi báo cáo.</summary>
        public List<string> StatusOptions { get; set; }

        public List<int> YearOptions { get; set; }

        public int ReportedCount { get; set; }
        public int MissingCount { get; set; }

        public MyReportViewModel()
        {
            Rows = new List<MyReportRow>();
            StatusOptions = new List<string>();
            YearOptions = new List<int>();
        }
    }
}
