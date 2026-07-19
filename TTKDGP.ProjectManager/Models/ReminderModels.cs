using System;
using System.Collections.Generic;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>Các kỳ nhắc báo cáo.</summary>
    public enum ReminderKind
    {
        /// <summary>Sáng thứ Hai — điểm lại các dự án chưa báo cáo của tuần trước. Gửi vào nhóm.</summary>
        MondayPreviousWeek = 1,

        /// <summary>Chiều thứ Sáu — nhắc các dự án chưa báo cáo của tuần này. Gửi vào nhóm.</summary>
        FridayCurrentWeek = 2,

        /// <summary>
        /// Sáng thứ Bảy — tổng hợp PM nào chưa báo cáo, dạng danh sách phẳng
        /// "Tên PM - Dự án" để anh Tân theo dõi. Cũng gửi vào nhóm.
        /// </summary>
        SaturdayAdminSummary = 3
    }

    /// <summary>Một dự án còn thiếu báo cáo trong kỳ đang xét.</summary>
    public class MissingProject
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Customer { get; set; }
        public string Status { get; set; }

        /// <summary>
        /// Những thành viên đang tham gia dự án mà tuần này chưa ghi nhật ký.
        /// Chỉ cần một người thiếu là cả dự án bị tính là chưa báo cáo.
        /// </summary>
        public List<string> MissingMembers { get; set; }

        /// <summary>Tổng số thành viên đang tham gia dự án này.</summary>
        public int ActiveMemberCount { get; set; }

        /// <summary>Phần trong ngoặc sau tên dự án, các tên nối nhau bằng dấu chấm phẩy.</summary>
        public string MissingMembersText
        {
            get { return string.Join("; ", MissingMembers); }
        }

        public MissingProject()
        {
            MissingMembers = new List<string>();
        }
    }

    /// <summary>Nhóm các dự án chưa báo cáo theo từng PM phụ trách.</summary>
    public class MissingByPm
    {
        public int PmMemberId { get; set; }
        public string PmName { get; set; }
        public List<MissingProject> Projects { get; set; }

        public MissingByPm()
        {
            Projects = new List<MissingProject>();
        }
    }

    /// <summary>Kết quả rà soát một kỳ báo cáo.</summary>
    public class ReminderReport
    {
        public ReminderKind Kind { get; set; }
        public int Year { get; set; }
        public int Week { get; set; }
        public DateTime WeekFrom { get; set; }
        public DateTime WeekTo { get; set; }

        /// <summary>Các PM còn dự án chưa báo cáo, đã sắp theo tên.</summary>
        public List<MissingByPm> Groups { get; set; }

        /// <summary>Tổng số dự án chưa dừng được đưa vào rà soát.</summary>
        public int TotalOpenProjects { get; set; }

        /// <summary>Số dự án đã có báo cáo trong kỳ.</summary>
        public int ReportedProjects { get; set; }

        public int MissingProjectCount { get; set; }

        /// <summary>Nội dung tin nhắn gửi lên nhóm Telegram.</summary>
        public string Message { get; set; }

        /// <summary>Không còn dự án nào thiếu báo cáo thì không cần gửi tin.</summary>
        public bool HasSomethingToSend { get { return MissingProjectCount > 0; } }

        public string KindLabel
        {
            get
            {
                switch (Kind)
                {
                    case ReminderKind.MondayPreviousWeek: return "Sáng thứ Hai — rà soát tuần trước";
                    case ReminderKind.FridayCurrentWeek: return "Chiều thứ Sáu — rà soát tuần này";
                    default: return "Sáng thứ Bảy — báo cáo cho anh Tân";
                }
            }
        }

        public ReminderReport()
        {
            Groups = new List<MissingByPm>();
        }
    }

    /// <summary>
    /// Nhật ký mỗi lần gửi nhắc. Dùng để hiển thị lịch sử và để bộ lịch biết
    /// kỳ này đã gửi rồi, tránh gửi trùng khi ứng dụng khởi động lại.
    /// </summary>
    public class ReminderLog : IEntity
    {
        public int Id { get; set; }

        public ReminderKind Kind { get; set; }

        /// <summary>Tuần được rà soát.</summary>
        public int Year { get; set; }
        public int Week { get; set; }

        public DateTime SentAt { get; set; }

        /// <summary>Gửi tự động theo lịch hay do người dùng bấm nút.</summary>
        public bool IsManual { get; set; }

        public bool Success { get; set; }
        public string Error { get; set; }

        public int MissingProjectCount { get; set; }
        public int PmCount { get; set; }

        /// <summary>Nội dung đã gửi, giữ lại để đối chiếu.</summary>
        public string Message { get; set; }

        /// <summary>Người bấm gửi, để trống nếu do lịch tự chạy.</summary>
        public string TriggeredBy { get; set; }
    }

    /// <summary>Màn hình quản trị thông báo.</summary>
    public class NotificationsViewModel
    {
        public bool TelegramEnabled { get; set; }
        public bool HasToken { get; set; }
        public bool HasChatId { get; set; }
        public string MaskedToken { get; set; }
        public string ChatId { get; set; }
        public string BotName { get; set; }
        public string ConnectionError { get; set; }

        public int MondayHour { get; set; }
        public int FridayHour { get; set; }
        public int SaturdayHour { get; set; }

        /// <summary>Lịch tự động có đang chạy không. Ở máy phát triển thường là tắt.</summary>
        public bool AutoSend { get; set; }

        /// <summary>Xem trước nội dung của các kỳ nhắc, tính trên dữ liệu hiện tại.</summary>
        public ReminderReport MondayPreview { get; set; }
        public ReminderReport FridayPreview { get; set; }
        public ReminderReport SaturdayPreview { get; set; }

        public List<ReminderLog> History { get; set; }

        /// <summary>Các nhóm dò được khi bấm "Dò chat id".</summary>
        public List<Infrastructure.TelegramChat> DiscoveredChats { get; set; }

        public NotificationsViewModel()
        {
            History = new List<ReminderLog>();
            DiscoveredChats = new List<Infrastructure.TelegramChat>();
        }
    }
}
