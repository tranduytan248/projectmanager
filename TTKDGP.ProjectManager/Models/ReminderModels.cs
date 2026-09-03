using System;
using System.Collections.Generic;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Các kỳ nhắc báo cáo.
    ///
    /// Ba giá trị 1-3 là các kỳ gửi tin lên nhóm Telegram — nay đã bỏ, chỉ còn dùng làm
    /// mốc tuần khi rà soát trên màn hình. Giữ lại giá trị để các dòng nhật ký cũ trong
    /// cơ sở dữ liệu vẫn đọc được.
    /// </summary>
    public enum ReminderKind
    {
        /// <summary>Rà soát các dự án chưa báo cáo của tuần trước.</summary>
        MondayPreviousWeek = 1,

        /// <summary>Rà soát các dự án chưa báo cáo của tuần này.</summary>
        FridayCurrentWeek = 2,

        /// <summary>Kỳ tổng hợp cũ gửi lên nhóm — không còn dùng.</summary>
        SaturdayAdminSummary = 3,

        /// <summary>
        /// Sáng thứ Sáu — mail riêng cho từng thành viên, liệt kê những dự án của
        /// TUẦN NÀY mà họ còn phải báo cáo cho PM.
        /// </summary>
        FridayMemberEmails = 4,

        /// <summary>
        /// Nhắc hạn công việc qua SMS, lượt buổi SÁNG của ngày làm việc.
        /// Đếm số việc hết hạn đúng trong ngày hôm đó.
        /// </summary>
        TaskDueSmsMorning = 5,

        /// <summary>Nhắc hạn công việc qua SMS, lượt buổi CHIỀU của cùng ngày.</summary>
        TaskDueSmsAfternoon = 6,

        /// <summary>
        /// Thông báo tổng số giờ logtime trong ngày qua SMS vào lúc 17h chiều.
        /// </summary>
        DailyLogTimeSms = 7
    }

    /// <summary>Một dự án mà thành viên còn phải báo cáo, kèm nơi gửi báo cáo.</summary>
    public class MemberMissingProject
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Customer { get; set; }
        public string Status { get; set; }

        public string PmName { get; set; }

        /// <summary>Có thể trống nếu PM chưa được điền email.</summary>
        public string PmEmail { get; set; }
    }

    /// <summary>Phần việc còn nợ của một thành viên, dùng để dựng mail gửi riêng cho họ.</summary>
    public class MemberReminder
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; }
        public string Email { get; set; }

        public List<MemberMissingProject> Projects { get; set; }

        /// <summary>Không có email thì không gửi được, nhưng vẫn đếm để báo cho quản trị biết.</summary>
        public bool CanSend { get { return !string.IsNullOrWhiteSpace(Email); } }

        public MemberReminder()
        {
            Projects = new List<MemberMissingProject>();
        }
    }

    /// <summary>Kết quả một lượt gửi mail cho từng thành viên.</summary>
    public class MemberEmailOutcome
    {
        public string MemberName { get; set; }
        public string Email { get; set; }
        public int ProjectCount { get; set; }
        public bool Sent { get; set; }
        public string Error { get; set; }
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

        /// <summary>Không còn dự án nào thiếu báo cáo thì không cần nhắc.</summary>
        public bool HasSomethingToSend { get { return MissingProjectCount > 0; } }

        public string KindLabel
        {
            get
            {
                switch (Kind)
                {
                    case ReminderKind.MondayPreviousWeek: return "Sáng thứ Hai — rà soát tuần trước";
                    case ReminderKind.FridayCurrentWeek: return "Chiều thứ Sáu — rà soát tuần này";
                    case ReminderKind.FridayMemberEmails: return "Sáng thứ Sáu — mail riêng từng thành viên";
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

        /// <summary>
        /// Số tin đã gửi được — mail với kỳ nhắc báo cáo, tin nhắn với kỳ nhắc hạn công việc.
        /// </summary>
        public int SentCount { get; set; }

        /// <summary>
        /// Số người lẽ ra phải nhận nhưng thiếu nơi nhận: chưa điền email (kỳ mail),
        /// hoặc chưa có số điện thoại (kỳ SMS).
        /// </summary>
        public int NoEmailCount { get; set; }

        /// <summary>Nội dung đã gửi, giữ lại để đối chiếu.</summary>
        public string Message { get; set; }

        /// <summary>Người bấm gửi, để trống nếu do lịch tự chạy.</summary>
        public string TriggeredBy { get; set; }

        /// <summary>Nhãn ngắn của kỳ nhắc, dùng trong bảng lịch sử.</summary>
        public string KindLabel
        {
            get
            {
                switch (Kind)
                {
                    case ReminderKind.MondayPreviousWeek: return "Thứ Hai · nhóm (đã bỏ)";
                    case ReminderKind.FridayCurrentWeek: return "Thứ Sáu · nhóm (đã bỏ)";
                    case ReminderKind.FridayMemberEmails: return "Thứ Sáu · mail";
                    case ReminderKind.TaskDueSmsMorning: return "Hạn công việc · SMS sáng";
                    case ReminderKind.TaskDueSmsAfternoon: return "Hạn công việc · SMS chiều";
                    case ReminderKind.DailyLogTimeSms: return "Giờ công LogTime · SMS 17h";
                    default: return "Thứ Bảy · nhóm (đã bỏ)";
                }
            }
        }

        /// <summary>Kỳ nhắc hạn công việc bằng SMS (sáng hoặc chiều).</summary>
        public bool IsTaskSmsKind
        {
            get
            {
                return Kind == ReminderKind.TaskDueSmsMorning
                    || Kind == ReminderKind.TaskDueSmsAfternoon;
            }
        }

        /// <summary>
        /// Kỳ đếm theo SỐ NGƯỜI NHẬN (đã gửi / cần gửi) thay vì theo số PM — gồm kỳ gửi mail
        /// riêng cho từng thành viên, các kỳ nhắn tin nhắc hạn công việc và SMS logtime.
        /// </summary>
        public bool IsEmailKind
        {
            get { return Kind == ReminderKind.FridayMemberEmails || IsTaskSmsKind || Kind == ReminderKind.DailyLogTimeSms; }
        }
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

        // ----- Bot Telegram thứ hai: đăng nhập GoConnect -----
        // Hệ thống dùng HAI bot khác nhau. Hiển thị cả hai cạnh nhau để nhìn là biết ngay
        // tin nhắc báo cáo và tin hỏi OTP đang đi ra từ đúng bot của nó.
        public bool GoConnectEnabled { get; set; }
        public bool GoConnectHasToken { get; set; }
        public bool GoConnectHasChatId { get; set; }
        public string GoConnectMaskedToken { get; set; }
        public string GoConnectChatId { get; set; }
        public string GoConnectBotName { get; set; }
        public string GoConnectConnectionError { get; set; }

        /// <summary>Hai chức năng đang dùng chung một bot — cấu hình sai, cần báo đỏ.</summary>
        public bool SharesBotWithReminder { get; set; }

        /// <summary>Hai chức năng đang gửi vào cùng một nơi nhận.</summary>
        public bool SharesChatIdWithReminder { get; set; }

        // ----- Kênh mail -----
        public bool EmailEnabled { get; set; }
        public bool EmailHasHost { get; set; }
        public bool EmailHasAccount { get; set; }
        public string EmailHost { get; set; }
        public int EmailPort { get; set; }
        public string EmailUser { get; set; }
        public string EmailFrom { get; set; }
        public string EmailDisplayName { get; set; }
        public string EmailMaskedPassword { get; set; }

        /// <summary>Xem trước danh sách người sẽ nhận mail sáng thứ Sáu.</summary>
        public List<MemberReminder> MemberReminders { get; set; }

        public bool EmailReady { get { return EmailEnabled && EmailHasHost && EmailHasAccount; } }

        public int MemberEmailHour { get; set; }

        /// <summary>Lịch tự động có đang chạy không. Ở máy phát triển thường là tắt.</summary>
        public bool AutoSend { get; set; }

        // ----- Nhắc hạn công việc bằng SMS -----
        public bool TaskSmsEnabled { get; set; }
        public int TaskSmsMorningHour { get; set; }
        public int TaskSmsAfternoonHour { get; set; }

        /// <summary>Tổng đài SMS đã cấu hình đủ để gửi chưa.</summary>
        public bool SmsReady { get; set; }

        /// <summary>Xem trước: hôm nay ai có việc đến hạn, ai chưa có số điện thoại.</summary>
        public Services.TaskDueSmsService.Preview TaskSmsPreview { get; set; }

        // ----- Thông báo tổng số giờ Logtime bằng SMS (17h hàng ngày) -----
        public bool LogTimeSmsEnabled { get; set; }
        public int LogTimeSmsHour { get; set; }
        public decimal LogTimeStandardHours { get; set; }
        public bool LogTimeFcmPushEnabled { get; set; }
        public Services.DailyLogTimeSmsService.Preview LogTimeSmsPreview { get; set; }

        public List<ReminderLog> History { get; set; }

        /// <summary>Các nhóm dò được khi bấm "Dò chat id".</summary>
        public List<Infrastructure.TelegramChat> DiscoveredChats { get; set; }

        public NotificationsViewModel()
        {
            History = new List<ReminderLog>();
            DiscoveredChats = new List<Infrastructure.TelegramChat>();
            MemberReminders = new List<MemberReminder>();
        }
    }
}
