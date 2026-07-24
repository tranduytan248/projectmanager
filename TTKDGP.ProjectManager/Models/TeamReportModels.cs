using System;
using System.Collections.Generic;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Báo cáo tuần của một nhân sự trên một dự án.</summary>
    public class TeamReportMemberRow
    {
        public int AssignmentId { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; }

        /// <summary>Vai trò trong dự án (Chủ trì, Hỗ trợ...).</summary>
        public string Role { get; set; }

        /// <summary>Trạng thái tham gia của phân công.</summary>
        public string WorkStatus { get; set; }

        /// <summary>Nhật ký tuần đang có của phân công này; 0 nếu tuần này chưa ghi.</summary>
        public int LogId { get; set; }

        public string Content { get; set; }
        public string Status { get; set; }
        public DateTime? ReportedAt { get; set; }
        public string ReportedBy { get; set; }

        /// <summary>Người sửa gần nhất — để PM biết nội dung còn nguyên văn hay đã được tổng hợp lại.</summary>
        public string EditedBy { get; set; }

        /// <summary>Bản rút gọn máy gợi ý sẵn, đổ vào ô soạn thảo cho PM sửa lại.</summary>
        public string Suggested { get; set; }

        public bool HasReport { get { return !string.IsNullOrWhiteSpace(Content); } }
    }

    /// <summary>Một dòng PM gửi lên khi lưu bản tổng hợp của dự án.</summary>
    public class TeamReportSaveRow
    {
        public int AssignmentId { get; set; }
        public string Content { get; set; }
        public string Status { get; set; }
    }

    /// <summary>Các nhân sự của một dự án trong tuần đang xem.</summary>
    public class TeamReportProjectGroup
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Customer { get; set; }
        public string PmName { get; set; }

        public List<TeamReportMemberRow> Members { get; set; }

        public int ReportedCount { get; set; }
        public int MissingCount { get; set; }

        public TeamReportProjectGroup()
        {
            Members = new List<TeamReportMemberRow>();
        }
    }

    /// <summary>
    /// Màn hình PM xem báo cáo của nhân sự tham gia dự án theo tuần, kèm phần tổng hợp để
    /// gửi lại lên nhóm.
    /// </summary>
    public class TeamReportViewModel
    {
        public int Year { get; set; }
        public int Week { get; set; }
        public string WeekLabel { get; set; }
        public DateTime WeekFrom { get; set; }
        public DateTime WeekTo { get; set; }
        public bool IsCurrentWeek { get; set; }

        public List<TeamReportProjectGroup> Groups { get; set; }

        // Bộ lọc
        public int? ProjectId { get; set; }

        /// <summary>Lọc về đúng một nhân sự, xem người đó báo cáo những gì trong tuần.</summary>
        public int? MemberId { get; set; }

        public bool OnlyMyProjects { get; set; }

        /// <summary>Tài khoản có gắn nhân sự và đang là PM của ít nhất một dự án.</summary>
        public bool CanFilterMine { get; set; }

        public List<Project> ProjectOptions { get; set; }

        /// <summary>Nhân sự có mặt trong phạm vi đang xem, đổ vào ô lọc theo nhân sự.</summary>
        public List<Member> MemberOptions { get; set; }

        public List<int> YearOptions { get; set; }

        /// <summary>Các trạng thái tham gia được phép chọn khi PM lưu tổng hợp.</summary>
        public List<string> StatusOptions { get; set; }

        public int TotalReported { get; set; }
        public int TotalMissing { get; set; }

        /// <summary>Nội dung tổng hợp đã dựng sẵn, để xem trước / sao chép / gửi lên nhóm.</summary>
        public string SummaryText { get; set; }

        /// <summary>Đủ cấu hình Telegram để bấm gửi hay chưa.</summary>
        public bool TelegramReady { get; set; }

        public TeamReportViewModel()
        {
            Groups = new List<TeamReportProjectGroup>();
            ProjectOptions = new List<Project>();
            MemberOptions = new List<Member>();
            YearOptions = new List<int>();
            StatusOptions = new List<string>();
        }
    }
}
