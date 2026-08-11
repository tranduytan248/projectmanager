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
    /// để hiển thị trên màn hình và dựng mail nhắc riêng cho từng thành viên.
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

            var openProjects = OpenProjects();
            var activeAssignments = ReportableAssignments();
            var reportedAssignmentIds = ReportedAssignmentIds(year, week);

            var memberNames = Repository.Members.All().ToDictionary(m => m.Id, m => m.FullName);

            var missing = new List<MissingProject>();
            var missingByProjectId = new Dictionary<int, MissingProject>();

            foreach (var project in openProjects)
            {
                var team = activeAssignments.Where(a => a.ProjectId == project.Id).ToList();

                var notYet = team
                    .Where(a => !reportedAssignmentIds.Contains(a.Id))
                    .Select(a => memberNames.ContainsKey(a.MemberId)
                        ? memberNames[a.MemberId]
                        : a.MemberName)
                    .OrderBy(n => n, StringComparer.CurrentCulture)
                    .ToList();

                // Không còn ai thiếu báo cáo thì bỏ qua. Trường hợp dự án chưa phân công ai
                // cũng rơi vào đây: không có người nào để nhắc nên không đưa vào tin.
                if (notYet.Count == 0) continue;

                var entry = new MissingProject
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    Customer = project.Customer,
                    Status = project.CurrentStatus,
                    ActiveMemberCount = team.Count,
                    MissingMembers = notYet
                };

                missing.Add(entry);
                missingByProjectId[project.Id] = entry;
            }

            report.TotalOpenProjects = openProjects.Count;
            report.ReportedProjects = openProjects.Count - missing.Count;
            report.MissingProjectCount = missing.Count;

            report.Groups = openProjects
                .Where(p => missingByProjectId.ContainsKey(p.Id))
                .GroupBy(p => p.PmMemberId)
                .Select(g => new MissingByPm
                {
                    PmMemberId = g.Key,
                    PmName = memberNames.ContainsKey(g.Key) ? memberNames[g.Key] : "(chưa gán PM)",
                    Projects = g
                        .OrderBy(p => p.Name, StringComparer.CurrentCulture)
                        .Select(p => missingByProjectId[p.Id])
                        .ToList()
                })
                .OrderByDescending(g => g.Projects.Count)
                .ThenBy(g => g.PmName, StringComparer.CurrentCulture)
                .ToList();

            return report;
        }

        // ----- Các bộ lọc dùng chung cho mọi kỳ nhắc -----

        /// <summary>Dự án chưa dừng — trạng thái được đánh dấu "coi như đã dừng" thì không cần báo cáo.</summary>
        private static List<Project> OpenProjects()
        {
            var closedStatuses = new HashSet<string>(
                Repository.ProjectStatuses.All().Where(s => s.IsClosed).Select(s => s.Name),
                StringComparer.CurrentCultureIgnoreCase);

            return Repository.Projects.All()
                .Where(p => string.IsNullOrWhiteSpace(p.CurrentStatus) || !closedStatuses.Contains(p.CurrentStatus))
                .ToList();
        }

        /// <summary>Phân công còn hiệu lực và đang ở trạng thái công việc phải báo cáo.</summary>
        private static List<ProjectMember> ReportableAssignments()
        {
            var skipWorkStatuses = new HashSet<string>(
                Repository.WorkStatuses.All().Where(s => s.IsClosed).Select(s => s.Name),
                StringComparer.CurrentCultureIgnoreCase);

            return Repository.Assignments.All()
                .Where(a => a.IsActive)
                .Where(a => string.IsNullOrWhiteSpace(a.WorkStatus) || !skipWorkStatuses.Contains(a.WorkStatus))
                .ToList();
        }

        /// <summary>
        /// Các phân công đã có nhật ký trong tuần. Tính theo từng phân công, không theo dự án:
        /// hai người cùng một dự án thì một người ghi không thay được cho người kia.
        /// </summary>
        private static HashSet<int> ReportedAssignmentIds(int year, int week)
        {
            return new HashSet<int>(
                Repository.WorkLogs.All()
                    .Where(w => w.Year == year && w.Week == week)
                    .Select(w => w.AssignmentId));
        }

        /// <summary>
        /// Nội dung mail dùng HTML nên chỉ đòi thay ba ký tự &lt; &gt; &amp;.
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

        // ===== Mail riêng cho từng thành viên (sáng thứ Sáu) =====

        /// <summary>
        /// Gom danh sách dự án mà mỗi thành viên đang tham gia còn phải báo cáo, kèm PM phụ trách
        /// và email của PM — là nơi họ gửi báo cáo về để PM tổng hợp lên hệ thống.
        ///
        /// Dùng đúng bộ lọc của tin nhắc gửi lên nhóm, nên danh sách dự án của mỗi người ở đây
        /// trùng khớp với tên của họ trong ngoặc trên tin Telegram.
        ///
        /// Sắp theo tên người để danh sách xem trước ổn định giữa các lần mở màn hình.
        /// </summary>
        public static List<MemberReminder> BuildMemberReminders(DateTime today)
        {
            var year = WeekHelper.GetYear(today);
            var week = WeekHelper.GetWeek(today);

            var openProjectIds = new HashSet<int>(OpenProjects().Select(p => p.Id));
            var reportedAssignmentIds = ReportedAssignmentIds(year, week);
            var members = Repository.Members.All().ToDictionary(m => m.Id, m => m);
            var projects = Repository.Projects.All().ToDictionary(p => p.Id, p => p);

            var byMember = new Dictionary<int, MemberReminder>();

            foreach (var assignment in ReportableAssignments())
            {
                if (!openProjectIds.Contains(assignment.ProjectId)) continue;
                if (reportedAssignmentIds.Contains(assignment.Id)) continue;

                Member member;
                if (!members.TryGetValue(assignment.MemberId, out member)) continue;

                // Người đã nghỉ thì không nhắc nữa, dù phân công còn treo.
                if (!member.IsActive) continue;

                Project project;
                if (!projects.TryGetValue(assignment.ProjectId, out project)) continue;

                Member pm;
                var hasPm = members.TryGetValue(project.PmMemberId, out pm);

                MemberReminder entry;
                if (!byMember.TryGetValue(member.Id, out entry))
                {
                    entry = new MemberReminder
                    {
                        MemberId = member.Id,
                        MemberName = member.FullName,
                        Email = member.Email
                    };
                    byMember[member.Id] = entry;
                }

                entry.Projects.Add(new MemberMissingProject
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    Customer = project.Customer,
                    Status = project.CurrentStatus,
                    PmName = hasPm ? pm.FullName : "(chưa gán PM)",
                    PmEmail = hasPm ? pm.Email : null
                });
            }

            foreach (var entry in byMember.Values)
            {
                entry.Projects = entry.Projects
                    .OrderBy(p => p.ProjectName, StringComparer.CurrentCulture)
                    .ToList();
            }

            return byMember.Values
                .OrderBy(e => e.MemberName, StringComparer.CurrentCulture)
                .ToList();
        }

        /// <summary>
        /// Nội dung mail gửi cho một thành viên: tên dự án và địa chỉ PM cần gửi báo cáo về,
        /// kèm khung nội dung báo cáo cần viết. Cố ý không đưa số liệu tổng hợp vào đây —
        /// người nhận chỉ cần biết gửi cái gì cho ai.
        /// </summary>
        public static string BuildMemberEmailBody(MemberReminder reminder, int year, int week,
            DateTime weekFrom, DateTime weekTo)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222\">");
            sb.AppendLine("<p>Kính gửi anh/chị <b>" + Escape(reminder.MemberName) + "</b>,</p>");
            sb.AppendLine(string.Format(
                "<p>Đề nghị anh/chị gửi báo cáo tuần <b>{0}/{1}</b> ({2:dd/MM} – {3:dd/MM/yyyy}) "
                + "về cho PM phụ trách theo danh sách dưới đây:</p>",
                week, year, weekFrom, weekTo));

            sb.AppendLine("<table cellpadding=\"8\" cellspacing=\"0\" "
                          + "style=\"border-collapse:collapse;border:1px solid #d0d0d0\">");
            sb.AppendLine("<tr style=\"background:#f2f4f7\">"
                          + "<th align=\"left\" style=\"border:1px solid #d0d0d0\">STT</th>"
                          + "<th align=\"left\" style=\"border:1px solid #d0d0d0\">Tên dự án</th>"
                          + "<th align=\"left\" style=\"border:1px solid #d0d0d0\">Gửi báo cáo cho</th></tr>");

            var index = 0;
            foreach (var project in reminder.Projects)
            {
                index++;

                var pmCell = Escape(project.PmName);
                pmCell += string.IsNullOrWhiteSpace(project.PmEmail)
                    ? "<br /><span style=\"color:#999\">(chưa có email)</span>"
                    : string.Format("<br /><a href=\"mailto:{0}\">{0}</a>", Escape(project.PmEmail));

                sb.AppendLine(string.Format(
                    "<tr><td style=\"border:1px solid #d0d0d0\">{0}</td>"
                    + "<td style=\"border:1px solid #d0d0d0\">{1}</td>"
                    + "<td style=\"border:1px solid #d0d0d0\">{2}</td></tr>",
                    index, Escape(project.ProjectName), pmCell));
            }

            sb.AppendLine("</table>");

            sb.AppendLine("<p><b>Nội dung báo cáo gồm hai phần:</b></p>");
            sb.AppendLine("<ol>");
            sb.AppendLine("<li>Nội dung đã thực hiện trong tuần — báo cáo tổng quát, không liệt kê chi tiết.</li>");
            sb.AppendLine("<li>Nội dung dự kiến sẽ thực hiện trong tuần sau.</li>");
            sb.AppendLine("</ol>");

            var link = AppSettings.PublicLink;
            if (!string.IsNullOrWhiteSpace(link))
            {
                sb.AppendLine(string.Format(
                    "<p>Anh/chị cũng có thể cập nhật trực tiếp tại: <a href=\"{0}\">{1}</a></p>",
                    Escape(link), Escape(AppSettings.PublicUrl)));
            }

            sb.AppendLine("<p style=\"color:#888;font-size:12px\">"
                          + "Mail này được gửi tự động, anh/chị không cần trả lời lại mail này.</p>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        /// <summary>
        /// Gửi mail nhắc gửi báo cáo cho từng thành viên. Một người gửi lỗi thì vẫn gửi tiếp
        /// những người còn lại — lỗi được gom vào nhật ký để quản trị xem lại.
        /// </summary>
        public static ReminderLog RunMemberEmails(DateTime now, bool isManual, string triggeredBy)
        {
            var year = WeekHelper.GetYear(now);
            var week = WeekHelper.GetWeek(now);

            var reminders = BuildMemberReminders(now);

            var log = new ReminderLog
            {
                Kind = ReminderKind.FridayMemberEmails,
                Year = year,
                Week = week,
                SentAt = now,
                IsManual = isManual,
                TriggeredBy = triggeredBy,
                MissingProjectCount = reminders.Sum(r => r.Projects.Count),
                PmCount = reminders.Count,
                NoEmailCount = reminders.Count(r => !r.CanSend)
            };

            if (reminders.Count == 0)
            {
                log.Success = true;
                log.Error = "Không có thành viên nào thiếu báo cáo — không gửi mail.";
                Repository.ReminderLogs.Insert(log);
                return log;
            }

            if (!AppSettings.Email.IsConfigured)
            {
                log.Success = false;
                log.Error = "Chưa cấu hình đủ máy chủ mail (cần bật tính năng, có host, tài khoản và mật khẩu).";
                Repository.ReminderLogs.Insert(log);
                return log;
            }

            var weekFrom = WeekHelper.FirstDayOfWeek(year, week);
            var weekTo = WeekHelper.LastDayOfWeek(year, week);
            var subject = string.Format("[NCPT] Nhắc báo cáo tuần {0}/{1}", week, year);

            var failures = new List<string>();
            var summary = new StringBuilder();

            foreach (var reminder in reminders)
            {
                if (!reminder.CanSend)
                {
                    summary.AppendLine(string.Format("- {0}: chưa có email, bỏ qua ({1} dự án).",
                        reminder.MemberName, reminder.Projects.Count));
                    continue;
                }

                var body = BuildMemberEmailBody(reminder, year, week, weekFrom, weekTo);
                var result = EmailClient.Send(reminder.Email, subject, body);

                if (result.Ok)
                {
                    log.SentCount++;
                    summary.AppendLine(string.Format("- {0} <{1}>: đã gửi ({2} dự án).",
                        reminder.MemberName, reminder.Email, reminder.Projects.Count));
                }
                else
                {
                    failures.Add(reminder.MemberName + ": " + result.Error);
                    summary.AppendLine(string.Format("- {0} <{1}>: LỖI — {2}",
                        reminder.MemberName, reminder.Email, result.Error));
                }
            }

            // Gửi được cho ít nhất một người thì coi như kỳ này đã chạy. Nếu đánh dấu thất bại
            // chỉ vì vài mail lỗi, bộ lịch sẽ chạy lại sau 5 phút và gửi trùng cho những người
            // đã nhận rồi. Các lỗi lẻ vẫn được ghi rõ trong nhật ký để quản trị gửi tay lại.
            // Chỉ khi không gửi nổi mail nào mới coi là thất bại để lần sau thử lại.
            var sendable = reminders.Count(r => r.CanSend);
            log.Success = log.SentCount > 0 || sendable == 0;
            log.Error = failures.Count == 0
                ? (sendable == 0 ? "Không ai trong danh sách có email — chưa gửi được mail nào." : null)
                : string.Format("{0}/{1} mail gửi lỗi: {2}",
                    failures.Count, sendable, string.Join(" | ", failures));
            log.Message = summary.ToString().TrimEnd();

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
