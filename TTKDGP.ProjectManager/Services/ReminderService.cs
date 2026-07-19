using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Rà soát các dự án chưa dừng mà chưa có báo cáo trong tuần, gom theo PM phụ trách
    /// và dựng nội dung nhắc gửi lên nhóm Telegram.
    /// </summary>
    public static class ReminderService
    {
        /// <summary>
        /// Dựng báo cáo cho một kỳ nhắc.
        /// Thứ Hai xét tuần trước, thứ Sáu xét tuần đang diễn ra.
        /// </summary>
        public static ReminderReport Build(ReminderKind kind, DateTime today)
        {
            var target = kind == ReminderKind.MondayPreviousWeek ? today.AddDays(-7) : today;
            var year = WeekHelper.GetYear(target);
            var week = WeekHelper.GetWeek(target);

            var report = new ReminderReport
            {
                Kind = kind,
                Year = year,
                Week = week,
                WeekFrom = WeekHelper.FirstDayOfWeek(year, week),
                WeekTo = WeekHelper.LastDayOfWeek(year, week)
            };

            // Trạng thái nào coi như dự án đã dừng thì không nhắc nữa.
            var closedStatuses = new HashSet<string>(
                Repository.ProjectStatuses.All().Where(s => s.IsClosed).Select(s => s.Name),
                StringComparer.CurrentCultureIgnoreCase);

            var openProjects = Repository.Projects.All()
                .Where(p => string.IsNullOrWhiteSpace(p.CurrentStatus) || !closedStatuses.Contains(p.CurrentStatus))
                .ToList();

            // Dự án được coi là đã báo cáo nếu có ít nhất một dòng nhật ký trong tuần đó.
            var reportedProjectIds = new HashSet<int>(
                Repository.WorkLogs.All()
                    .Where(w => w.Year == year && w.Week == week)
                    .Select(w => w.ProjectId));

            var missing = openProjects.Where(p => !reportedProjectIds.Contains(p.Id)).ToList();

            report.TotalOpenProjects = openProjects.Count;
            report.ReportedProjects = openProjects.Count - missing.Count;
            report.MissingProjectCount = missing.Count;

            var memberNames = Repository.Members.All().ToDictionary(m => m.Id, m => m.FullName);

            report.Groups = missing
                .GroupBy(p => p.PmMemberId)
                .Select(g => new MissingByPm
                {
                    PmMemberId = g.Key,
                    PmName = memberNames.ContainsKey(g.Key) ? memberNames[g.Key] : "(chưa gán PM)",
                    Projects = g
                        .OrderBy(p => p.Name, StringComparer.CurrentCulture)
                        .Select(p => new MissingProject
                        {
                            ProjectId = p.Id,
                            ProjectName = p.Name,
                            Customer = p.Customer,
                            Status = p.CurrentStatus
                        })
                        .ToList()
                })
                .OrderByDescending(g => g.Projects.Count)
                .ThenBy(g => g.PmName, StringComparer.CurrentCulture)
                .ToList();

            report.Message = BuildMessage(report);
            return report;
        }

        /// <summary>
        /// Dựng nội dung tin nhắn. Dùng HTML vì Telegram gửi kèm parse_mode=HTML;
        /// mọi giá trị lấy từ dữ liệu đều phải được mã hoá để không vỡ định dạng.
        /// </summary>
        private static string BuildMessage(ReminderReport report)
        {
            if (!report.HasSomethingToSend) return string.Empty;

            return report.Kind == ReminderKind.SaturdayAdminSummary
                ? BuildAdminSummary(report)
                : BuildGroupMessage(report);
        }

        /// <summary>Tin gửi vào nhóm: gom theo PM, mỗi PM một danh sách đánh số.</summary>
        private static string BuildGroupMessage(ReminderReport report)
        {
            var sb = new StringBuilder();

            var heading = report.Kind == ReminderKind.MondayPreviousWeek
                ? "Nhắc báo cáo đầu tuần"
                : "Nhắc báo cáo cuối tuần";

            sb.AppendLine("<b>" + Escape(heading) + "</b>");
            sb.AppendLine(string.Format("Tuần {0}/{1} ({2:dd/MM} – {3:dd/MM/yyyy})",
                report.Week, report.Year, report.WeekFrom, report.WeekTo));
            sb.AppendLine();
            sb.AppendLine("<b>Nội dung cần báo cáo</b>");

            foreach (var group in report.Groups)
            {
                sb.AppendLine();
                sb.AppendLine("<b>" + Escape(group.PmName) + "</b>");

                var index = 0;
                foreach (var project in group.Projects)
                {
                    index++;
                    sb.AppendLine(string.Format("{0}. {1}", index, Escape(project.ProjectName)));
                }
            }

            sb.AppendLine();
            sb.AppendLine(string.Format("<i>Tổng {0} dự án chưa báo cáo trên {1} dự án đang chạy.</i>",
                report.MissingProjectCount, report.TotalOpenProjects));
            AppendLink(sb);

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Tin tổng hợp sáng thứ Bảy gửi lên nhóm, dành cho người phụ trách theo dõi:
        /// danh sách phẳng theo mẫu "Tên PM - Dự án".
        /// </summary>
        private static string BuildAdminSummary(ReminderReport report)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<b>Báo cáo cho anh Tân</b>");
            sb.AppendLine(string.Format("Tuần {0}/{1} ({2:dd/MM} – {3:dd/MM/yyyy})",
                report.Week, report.Year, report.WeekFrom, report.WeekTo));
            sb.AppendLine();
            sb.AppendLine("<b>Các PM chưa báo cáo</b>");
            sb.AppendLine();

            // Anh Tân chỉ cần biết PM nào còn nợ và nợ bao nhiêu, không cần liệt kê từng dự án.
            foreach (var group in report.Groups)
            {
                sb.AppendLine(string.Format("{0} - {1} dự án",
                    Escape(group.PmName), group.Projects.Count));
            }

            sb.AppendLine();
            sb.AppendLine(string.Format("<i>{0} PM, {1} dự án chưa báo cáo trên {2} dự án đang chạy.</i>",
                report.Groups.Count, report.MissingProjectCount, report.TotalOpenProjects));
            AppendLink(sb);

            return sb.ToString().TrimEnd();
        }

        /// <summary>Đính kèm địa chỉ hệ thống ở cuối tin để người nhận vào cập nhật ngay.</summary>
        private static void AppendLink(StringBuilder sb)
        {
            var link = AppSettings.PublicLink;
            if (string.IsNullOrWhiteSpace(link)) return;

            sb.AppendLine();
            sb.AppendLine(string.Format("Vui lòng truy cập: <a href=\"{0}\">{1}</a>",
                Escape(link), Escape(AppSettings.PublicUrl)));
        }

        /// <summary>
        /// Telegram (parse_mode=HTML) chỉ đòi thay ba ký tự &lt; &gt; &amp;.
        /// Không dùng HtmlEncode vì nó đổi cả chữ có dấu thành &amp;#225; khiến tin khó đọc.
        /// </summary>
        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        /// <summary>
        /// Rà soát rồi gửi tin lên nhóm. Không có gì để nhắc thì không gửi,
        /// nhưng vẫn ghi nhật ký để biết lịch đã chạy.
        /// </summary>
        public static ReminderLog Run(ReminderKind kind, DateTime now, bool isManual, string triggeredBy)
        {
            var report = Build(kind, now);

            var log = new ReminderLog
            {
                Kind = kind,
                Year = report.Year,
                Week = report.Week,
                SentAt = now,
                IsManual = isManual,
                TriggeredBy = triggeredBy,
                MissingProjectCount = report.MissingProjectCount,
                PmCount = report.Groups.Count,
                Message = report.Message
            };

            if (!report.HasSomethingToSend)
            {
                log.Success = true;
                log.Error = "Không có dự án nào thiếu báo cáo — không gửi tin.";
                Repository.ReminderLogs.Insert(log);
                return log;
            }

            if (!AppSettings.Telegram.IsConfigured)
            {
                log.Success = false;
                log.Error = "Chưa cấu hình đủ Telegram (cần bật tính năng, có token và chat id).";
                Repository.ReminderLogs.Insert(log);
                return log;
            }

            var result = TelegramClient.SendMessage(
                AppSettings.Telegram.BotToken,
                AppSettings.Telegram.ChatId,
                report.Message);

            log.Success = result.Ok;
            log.Error = result.Ok ? null : result.Error;
            Repository.ReminderLogs.Insert(log);
            return log;
        }

        /// <summary>
        /// Kỳ nhắc này đã gửi thành công chưa. Dùng để bộ lịch không gửi trùng
        /// khi ứng dụng khởi động lại nhiều lần trong ngày.
        /// </summary>
        public static bool AlreadySent(ReminderKind kind, int year, int week)
        {
            return Repository.ReminderLogs.All()
                .Any(l => l.Kind == kind && l.Year == year && l.Week == week && l.Success && !l.IsManual);
        }
    }
}
