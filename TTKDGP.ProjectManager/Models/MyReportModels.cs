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

        /// <summary>Nội dung đã ghi cho phân công này ở TUẦN TRƯỚC — để gợi ý và bấm dùng lại.</summary>
        public string PreviousContent { get; set; }

        public bool HasPrevious { get { return !string.IsNullOrWhiteSpace(PreviousContent); } }
    }

    /// <summary>Một tuần trong phần "Lịch sử gần đây" của màn Báo cáo của tôi.</summary>
    public class MyReportWeekHistory
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public string WeekLabel { get; set; }

        /// <summary>Số phân công đã ghi báo cáo trong tuần đó.</summary>
        public int Reported { get; set; }

        /// <summary>Tổng số phân công cần báo cáo (lấy theo phân công hiện tại).</summary>
        public int Total { get; set; }

        /// <summary>Trích một đoạn nội dung của tuần đó, hiện cho dễ nhận ra.</summary>
        public string Snippet { get; set; }

        public bool Full { get { return Total > 0 && Reported >= Total; } }
        public int Missing { get { return Total - Reported < 0 ? 0 : Total - Reported; } }
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

        /// <summary>Nhãn tuần liền trước tuần đang xem, ví dụ "Tuần 29".</summary>
        public string PreviousWeekLabel { get; set; }

        /// <summary>Vài tuần gần nhất trước tuần đang xem, để tự soi tình hình báo cáo.</summary>
        public List<MyReportWeekHistory> History { get; set; }

        public int TotalRows { get { return Rows == null ? 0 : Rows.Count; } }
        public int ReportedPercent
        {
            get { return TotalRows <= 0 ? 0 : (int)System.Math.Round(ReportedCount * 100.0 / TotalRows); }
        }

        public MyReportViewModel()
        {
            Rows = new List<MyReportRow>();
            StatusOptions = new List<string>();
            YearOptions = new List<int>();
            History = new List<MyReportWeekHistory>();
        }
    }
}
