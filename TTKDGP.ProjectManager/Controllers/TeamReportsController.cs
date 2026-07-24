using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// PM xem báo cáo tuần của nhân sự tham gia dự án, rồi tổng hợp và gửi lại lên nhóm Telegram.
    /// Nguồn dữ liệu chính là nhật ký tuần do nhân sự tự ghi ở màn "Báo cáo của tôi".
    /// </summary>
    [AppAuthorize(AllowedRoles = Roles.Management)]
    public class TeamReportsController : BaseController
    {
        public ActionResult Index(int? year, int? week, int? projectId, int? memberId, bool? onlyMine)
        {
            return View(BuildModel(year, week, projectId, memberId, onlyMine));
        }

        /// <summary>
        /// PM rút gọn nội dung tuần của từng nhân sự trong một dự án rồi lưu lại.
        /// Ghi thẳng vào nhật ký tuần của phân công — đúng chỗ mà PM vẫn ghi lâu nay,
        /// nên nội dung sau khi tổng hợp chảy tiếp ra màn Tổng hợp, timeline và báo cáo thứ Bảy.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveSummary(int groupProjectId, int year, int week,
            TeamReportSaveRow[] rows, int? projectId, int? memberId, bool? onlyMine)
        {
            if (year < 2000 || year > 2100 || week < 1 || week > WeekHelper.WeeksInYear(year))
            {
                NotifyError("Tuần báo cáo không hợp lệ.");
                return Redirect(BackUrl(WeekHelper.CurrentYear, WeekHelper.CurrentWeek, projectId, memberId, onlyMine));
            }

            if (rows == null || rows.Length == 0)
            {
                NotifyError("Không có nội dung nào để lưu.");
                return Redirect(BackUrl(year, week, projectId, memberId, onlyMine));
            }

            var saved = 0;
            var skipped = 0;
            var editor = CurrentUser == null ? null : CurrentUser.Name;

            foreach (var row in rows)
            {
                var assignment = Repository.Assignments.Find(row.AssignmentId);

                // Chỉ ghi được vào phân công đang tham gia, thuộc đúng dự án của khối đang lưu.
                if (assignment == null || assignment.ProjectId != groupProjectId
                    || !assignment.IsActive || !IsReportable(assignment))
                {
                    skipped++;
                    continue;
                }

                // Để trống thì giữ nguyên nội dung cũ, tránh lỡ tay xoá mất báo cáo của nhân sự.
                if (string.IsNullOrWhiteSpace(row.Content)) continue;

                var text = row.Content.Trim();
                if (text.Length > 2000) text = text.Substring(0, 2000);

                var newStatus = string.IsNullOrWhiteSpace(row.Status)
                    ? assignment.WorkStatus : row.Status.Trim();

                var existing = Repository.WorkLogs.FirstOrDefault(w =>
                    w.AssignmentId == assignment.Id && w.Year == year && w.Week == week);

                if (existing == null)
                {
                    Repository.WorkLogs.Insert(new WorkLog
                    {
                        AssignmentId = assignment.Id,
                        ProjectId = assignment.ProjectId,
                        MemberId = assignment.MemberId,
                        Year = year,
                        Week = week,
                        Content = text,
                        Status = newStatus,
                        CreatedBy = editor,
                        CreatedAt = DateTime.Now
                    });
                    saved++;
                }
                else if (existing.Content != text || existing.Status != newStatus)
                {
                    existing.Content = text;
                    existing.Status = newStatus;
                    existing.UpdatedBy = editor;
                    existing.UpdatedAt = DateTime.Now;
                    Repository.WorkLogs.Update(existing);
                    saved++;
                }

                Repository.SyncAssignmentFromLatestLog(assignment.Id);
            }

            var project = Repository.Projects.Find(groupProjectId);
            var name = project != null ? project.Name : "dự án";

            if (saved > 0)
            {
                Notify(string.Format("Đã lưu tổng hợp tuần {0}/{1} cho {2} nhân sự — {3}.",
                    week, year, saved, name));
            }
            else if (skipped > 0)
            {
                NotifyError("Không lưu được: phân công đã ngừng tham gia hoặc không còn thuộc dự án này.");
            }
            else
            {
                Notify("Nội dung không thay đổi nên không có gì để lưu.");
            }

            return Redirect(BackUrl(year, week, projectId, memberId, onlyMine));
        }

        /// <summary>Gửi bản tổng hợp lên nhóm Telegram (dùng bot nhắc báo cáo).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Send(int year, int week, int? projectId, int? memberId, bool? onlyMine)
        {
            var model = BuildModel(year, week, projectId, memberId, onlyMine);

            if (model.Groups.Count == 0)
            {
                NotifyError("Không có nội dung nào để gửi trong tuần này.");
                return Redirect(BackUrl(year, week, projectId, memberId, onlyMine));
            }

            if (!AppSettings.Telegram.IsConfigured)
            {
                NotifyError("Chưa cấu hình Telegram nên chưa gửi được. Xem màn hình Thông báo.");
                return Redirect(BackUrl(year, week, projectId, memberId, onlyMine));
            }

            var result = TelegramClient.SendMessage(
                AppSettings.Telegram.BotToken, AppSettings.Telegram.ChatId, BuildTelegramMessage(model));

            if (result.Ok)
            {
                Notify(string.Format("Đã gửi tổng hợp tuần {0}/{1} lên nhóm.", week, year));
            }
            else
            {
                NotifyError("Gửi thất bại: " + result.Error);
            }

            return Redirect(BackUrl(year, week, projectId, memberId, onlyMine));
        }

        private string BackUrl(int year, int week, int? projectId, int? memberId, bool? onlyMine)
        {
            return Url.Action("Index", new
            {
                year = year,
                week = week,
                projectId = projectId,
                memberId = memberId,
                onlyMine = onlyMine
            });
        }

        // ---------- Dựng dữ liệu ----------

        private TeamReportViewModel BuildModel(int? year, int? week, int? projectId, int? memberId, bool? onlyMine)
        {
            var y = year ?? WeekHelper.CurrentYear;
            var w = week ?? WeekHelper.CurrentWeek;
            if (y < 2000 || y > 2100) y = WeekHelper.CurrentYear;
            var maxWeek = WeekHelper.WeeksInYear(y);
            if (w < 1) w = 1;
            if (w > maxWeek) w = maxWeek;

            var myMemberId = LinkedMemberId();
            var projects = Repository.Projects.All();

            // "Dự án tôi làm PM" chỉ bật được khi tài khoản có gắn nhân sự và nhân sự đó là PM.
            var myProjectIds = myMemberId <= 0
                ? new HashSet<int>()
                : new HashSet<int>(projects.Where(p => p.PmMemberId == myMemberId).Select(p => p.Id));

            var model = new TeamReportViewModel
            {
                Year = y,
                Week = w,
                WeekLabel = WeekHelper.LabelWithYear(y, w),
                WeekFrom = WeekHelper.FirstDayOfWeek(y, w),
                WeekTo = WeekHelper.LastDayOfWeek(y, w),
                IsCurrentWeek = y == WeekHelper.CurrentYear && w == WeekHelper.CurrentWeek,
                ProjectId = projectId,
                MemberId = memberId,
                CanFilterMine = myProjectIds.Count > 0,
                YearOptions = WeekHelper.YearOptions(Repository.YearsInLogs()),
                StatusOptions = AppSettings.Reporter.ReportableWorkStatuses,
                TelegramReady = AppSettings.Telegram.IsConfigured
            };

            // Mặc định lọc về dự án mình làm PM nếu có, cho đúng vai trò người dùng màn này.
            model.OnlyMyProjects = onlyMine ?? model.CanFilterMine;
            if (!model.CanFilterMine) model.OnlyMyProjects = false;

            var allMembers = Repository.Members.All();
            var members = allMembers.ToDictionary(m => m.Id, m => m.FullName);

            // Chỉ nhân sự đang tham gia và ở trạng thái phải báo cáo — đúng nhóm được kỳ vọng có báo cáo.
            var assignments = Repository.Assignments.All()
                .Where(a => a.IsActive && IsReportable(a))
                .ToList();

            if (model.OnlyMyProjects)
            {
                assignments = assignments.Where(a => myProjectIds.Contains(a.ProjectId)).ToList();
            }

            // Hai ô lọc dự án và nhân sự dựng từ phạm vi TRƯỚC khi lọc, nếu không chọn xong một
            // dự án là danh sách chỉ còn đúng dự án đó, muốn đổi phải quay về "Tất cả" trước.
            var scope = assignments;

            if (projectId.HasValue && projectId.Value > 0)
            {
                assignments = assignments.Where(a => a.ProjectId == projectId.Value).ToList();
            }

            if (memberId.HasValue && memberId.Value > 0)
            {
                assignments = assignments.Where(a => a.MemberId == memberId.Value).ToList();
            }

            var logs = Repository.WorkLogs.All()
                .Where(l => l.Year == y && l.Week == w)
                .GroupBy(l => l.AssignmentId)
                .ToDictionary(g => g.Key, g => g.First());

            var projectById = projects.ToDictionary(p => p.Id);

            foreach (var byProject in assignments.GroupBy(a => a.ProjectId))
            {
                Project project;
                projectById.TryGetValue(byProject.Key, out project);

                var group = new TeamReportProjectGroup
                {
                    ProjectId = byProject.Key,
                    ProjectName = project != null ? project.Name : byProject.First().ProjectName,
                    Customer = project != null ? project.Customer : null,
                    PmName = project != null && members.ContainsKey(project.PmMemberId)
                        ? members[project.PmMemberId] : null
                };

                foreach (var a in byProject)
                {
                    WorkLog log;
                    logs.TryGetValue(a.Id, out log);

                    group.Members.Add(new TeamReportMemberRow
                    {
                        AssignmentId = a.Id,
                        MemberId = a.MemberId,
                        MemberName = members.ContainsKey(a.MemberId) ? members[a.MemberId] : a.MemberName,
                        Role = a.Role,
                        WorkStatus = a.WorkStatus,
                        LogId = log != null ? log.Id : 0,
                        Content = log != null ? log.Content : null,
                        Status = log != null ? log.Status : a.WorkStatus,
                        ReportedAt = log == null ? (DateTime?)null : (log.UpdatedAt ?? log.CreatedAt),
                        ReportedBy = log != null ? log.CreatedBy : null,
                        EditedBy = log != null ? log.UpdatedBy : null,

                        // Gợi ý sẵn bản một dòng để PM chỉ việc cắt bớt, không phải gõ lại từ đầu.
                        Suggested = log != null ? OneLine(log.Content) : null
                    });
                }

                group.Members = group.Members
                    .OrderBy(m => m.MemberName, StringComparer.CurrentCulture)
                    .ToList();
                group.ReportedCount = group.Members.Count(m => m.HasReport);
                group.MissingCount = group.Members.Count - group.ReportedCount;

                model.Groups.Add(group);
            }

            model.Groups = model.Groups
                .OrderBy(g => g.ProjectName, StringComparer.CurrentCulture)
                .ToList();

            model.TotalReported = model.Groups.Sum(g => g.ReportedCount);
            model.TotalMissing = model.Groups.Sum(g => g.MissingCount);

            // Danh sách để lọc: những dự án / nhân sự có mặt trong phạm vi đang xem.
            var visibleIds = new HashSet<int>(scope.Select(a => a.ProjectId));
            if (model.OnlyMyProjects) visibleIds.UnionWith(myProjectIds);
            model.ProjectOptions = projects
                .Where(p => visibleIds.Contains(p.Id))
                .OrderBy(p => p.Name, StringComparer.CurrentCulture)
                .ToList();

            var visibleMemberIds = new HashSet<int>(scope.Select(a => a.MemberId));
            model.MemberOptions = allMembers
                .Where(m => visibleMemberIds.Contains(m.Id))
                .OrderBy(m => m.FullName, StringComparer.CurrentCulture)
                .ToList();

            model.SummaryText = BuildSummaryText(model);
            return model;
        }

        // ---------- Tổng hợp ----------

        /// <summary>Bản tổng hợp dạng chữ thuần, để PM xem trước và sao chép.</summary>
        private static string BuildSummaryText(TeamReportViewModel model)
        {
            var text = new StringBuilder();
            text.AppendFormat("TỔNG HỢP CÔNG VIỆC TUẦN {0}/{1} ({2:dd/MM} - {3:dd/MM/yyyy})",
                model.Week, model.Year, model.WeekFrom, model.WeekTo);
            text.AppendLine();

            foreach (var g in model.Groups)
            {
                text.AppendLine();
                text.Append("* ").Append(g.ProjectName);
                if (!string.IsNullOrWhiteSpace(g.PmName)) text.Append(" (PM: ").Append(g.PmName).Append(")");
                text.AppendLine();

                foreach (var m in g.Members)
                {
                    if (m.HasReport)
                    {
                        text.AppendFormat("  - {0}: {1}", m.MemberName, OneLine(m.Content));
                        text.AppendLine();
                    }
                    else
                    {
                        text.AppendFormat("  - {0}: (chưa báo cáo)", m.MemberName);
                        text.AppendLine();
                    }
                }
            }

            text.AppendLine();
            text.AppendFormat("Tổng: {0} đã báo cáo, {1} chưa báo cáo.", model.TotalReported, model.TotalMissing);
            return text.ToString();
        }

        /// <summary>Bản gửi Telegram — cùng nội dung nhưng có đánh dấu đậm, đã thoát ký tự HTML.</summary>
        private static string BuildTelegramMessage(TeamReportViewModel model)
        {
            var text = new StringBuilder();
            text.AppendFormat("📋 <b>Tổng hợp công việc tuần {0}/{1}</b> ({2:dd/MM} – {3:dd/MM/yyyy})",
                model.Week, model.Year, model.WeekFrom, model.WeekTo);
            text.AppendLine();

            foreach (var g in model.Groups)
            {
                text.AppendLine();
                text.Append("▪️ <b>").Append(Escape(g.ProjectName)).Append("</b>");
                if (!string.IsNullOrWhiteSpace(g.PmName)) text.Append(" — PM: ").Append(Escape(g.PmName));
                text.AppendLine();

                foreach (var m in g.Members)
                {
                    text.Append("   • ").Append(Escape(m.MemberName)).Append(": ");
                    text.Append(m.HasReport ? Escape(OneLine(m.Content)) : "<i>chưa báo cáo</i>");
                    text.AppendLine();
                }
            }

            text.AppendLine();
            text.AppendFormat("✅ {0} đã báo cáo · ⏳ {1} chưa báo cáo", model.TotalReported, model.TotalMissing);

            var link = AppSettings.PublicLink;
            if (!string.IsNullOrWhiteSpace(link))
            {
                text.AppendLine();
                text.AppendFormat("🌐 <a href=\"{0}\">{1}</a>", Escape(link), Escape(AppSettings.PublicUrl));
            }

            return text.ToString();
        }

        /// <summary>Gộp nội dung nhiều dòng thành một dòng cho gọn bản tổng hợp.</summary>
        private static string OneLine(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            var parts = content
                .Replace("\r", " ")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0);

            return string.Join("; ", parts);
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private int LinkedMemberId()
        {
            if (CurrentUser == null) return 0;
            var user = Repository.Users.Find(CurrentUser.UserId);
            return user == null ? 0 : user.MemberId;
        }

        private static bool IsReportable(ProjectMember assignment)
        {
            if (assignment == null || string.IsNullOrWhiteSpace(assignment.WorkStatus)) return false;

            return AppSettings.Reporter.ReportableWorkStatuses
                .Any(s => string.Equals(s, assignment.WorkStatus, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
