using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// "Báo cáo của tôi": người được phân công tự ghi nhật ký công việc theo tuần cho những
    /// phân công của CHÍNH MÌNH và đang ở trạng thái được báo cáo (mặc định: Đang thực hiện,
    /// Đang hỗ trợ — khai trong Reporter:WorkStatuses).
    ///
    /// Mở cho mọi tài khoản đã đăng nhập: tài khoản Báo cáo công việc chỉ có màn này và màn
    /// Tổng hợp; tài khoản Quản lý/Quản trị có gắn nhân sự cũng dùng được cho phần việc của họ.
    /// Mọi thao tác ghi đều kiểm phân công có đúng của người đăng nhập không.
    /// </summary>
    [AppAuthorize]
    public class MyReportsController : BaseController
    {
        [AppAuthorize(Permission = "myreports.view")]
        public ActionResult Index(int? year, int? week)
        {
            var model = BuildModel(year, week);
            return View(model);
        }

        /// <summary>Ghi hoặc cập nhật nhật ký tuần cho một phân công của chính mình.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "myreports.report")]
        public ActionResult Save(int assignmentId, int year, int week, string content, string status)
        {
            var memberId = LinkedMemberId();
            if (memberId <= 0)
            {
                NotifyError("Tài khoản chưa được gắn nhân sự nên chưa báo cáo được. Liên hệ quản trị.");
                return RedirectToAction("Index", new { year = year, week = week });
            }

            var assignment = Repository.Assignments.Find(assignmentId);

            // Chốt chặn quan trọng: chỉ cho ghi vào phân công của chính mình và đang được báo cáo.
            if (assignment == null || assignment.MemberId != memberId || !IsReportable(assignment))
            {
                NotifyError("Bạn không có quyền báo cáo cho phân công này.");
                return RedirectToAction("Index", new { year = year, week = week });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                NotifyError("Vui lòng nhập nội dung công việc trong tuần.");
                return RedirectToAction("Index", new { year = year, week = week });
            }

            if (year < 2000 || year > 2100 || week < 1 || week > WeekHelper.WeeksInYear(year))
            {
                NotifyError("Tuần báo cáo không hợp lệ.");
                return RedirectToAction("Index");
            }

            var text = content.Trim();
            if (text.Length > 2000) text = text.Substring(0, 2000);

            // Trạng thái để trống thì giữ nguyên trạng thái đang có của phân công.
            var newStatus = string.IsNullOrWhiteSpace(status) ? assignment.WorkStatus : status.Trim();

            var existing = Repository.WorkLogs.FirstOrDefault(w =>
                w.AssignmentId == assignmentId && w.Year == year && w.Week == week);

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
                    CreatedBy = CurrentUser == null ? null : CurrentUser.Name,
                    CreatedAt = DateTime.Now
                });
                Notify(string.Format("Đã báo cáo tuần {0}/{1}.", week, year));
            }
            else
            {
                existing.Content = text;
                existing.Status = newStatus;
                existing.UpdatedBy = CurrentUser == null ? null : CurrentUser.Name;
                existing.UpdatedAt = DateTime.Now;
                Repository.WorkLogs.Update(existing);
                Notify(string.Format("Đã cập nhật báo cáo tuần {0}/{1}.", week, year));
            }

            // Trạng thái và nội dung của phân công luôn theo mốc nhật ký mới nhất.
            Repository.SyncAssignmentFromLatestLog(assignment.Id);

            return RedirectToAction("Index", new { year = year, week = week });
        }

        // ---------- Dựng dữ liệu màn hình ----------

        private MyReportViewModel BuildModel(int? year, int? week)
        {
            var y = year ?? WeekHelper.CurrentYear;
            var w = week ?? WeekHelper.CurrentWeek;

            if (y < 2000 || y > 2100) y = WeekHelper.CurrentYear;
            var maxWeek = WeekHelper.WeeksInYear(y);
            if (w < 1) w = 1;
            if (w > maxWeek) w = maxWeek;

            var model = new MyReportViewModel
            {
                Year = y,
                Week = w,
                WeekLabel = WeekHelper.LabelWithYear(y, w),
                WeekFrom = WeekHelper.FirstDayOfWeek(y, w),
                WeekTo = WeekHelper.LastDayOfWeek(y, w),
                IsCurrentWeek = y == WeekHelper.CurrentYear && w == WeekHelper.CurrentWeek,
                StatusOptions = AppSettings.Reporter.ReportableWorkStatuses,
                YearOptions = WeekHelper.YearOptions(Repository.YearsInLogs())
            };

            var memberId = LinkedMemberId();
            if (memberId <= 0)
            {
                model.NotLinked = true;
                return model;
            }

            var member = Repository.Members.Find(memberId);
            model.MemberName = member != null ? member.FullName : null;

            // Chỉ phân công của mình, đang tham gia, và ở trạng thái được báo cáo.
            var assignments = Repository.Assignments.All()
                .Where(a => a.MemberId == memberId && a.IsActive && IsReportable(a))
                .ToList();

            var projects = Repository.Projects.All().ToDictionary(p => p.Id);

            // Nhật ký của đúng tuần đang xem, tra theo phân công.
            var logs = Repository.WorkLogs.All()
                .Where(l => l.Year == y && l.Week == w)
                .GroupBy(l => l.AssignmentId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var a in assignments)
            {
                Project project;
                projects.TryGetValue(a.ProjectId, out project);

                WorkLog log;
                logs.TryGetValue(a.Id, out log);

                model.Rows.Add(new MyReportRow
                {
                    AssignmentId = a.Id,
                    ProjectId = a.ProjectId,
                    ProjectName = project != null ? project.Name : a.ProjectName,
                    Customer = project != null ? project.Customer : null,
                    Role = a.Role,
                    WorkStatus = a.WorkStatus,
                    LogId = log != null ? log.Id : 0,
                    Content = log != null ? log.Content : null,
                    Status = log != null ? log.Status : a.WorkStatus,
                    ReportedAt = log == null ? (DateTime?)null : (log.UpdatedAt ?? log.CreatedAt)
                });
            }

            model.Rows = model.Rows
                .OrderBy(r => r.ProjectName, StringComparer.CurrentCulture)
                .ToList();

            model.ReportedCount = model.Rows.Count(r => r.HasReport);
            model.MissingCount = model.Rows.Count - model.ReportedCount;

            // ----- Nội dung tuần trước + lịch sử vài tuần gần nhất -----
            var myAssignmentIds = new HashSet<int>(assignments.Select(a => a.Id));
            var myLogs = Repository.WorkLogs.All()
                .Where(l => myAssignmentIds.Contains(l.AssignmentId))
                .ToList();

            int py, pw;
            PrevWeek(y, w, out py, out pw);
            model.PreviousWeekLabel = "Tuần " + pw;

            var prevByAssignment = myLogs
                .Where(l => l.Year == py && l.Week == pw)
                .GroupBy(l => l.AssignmentId)
                .ToDictionary(g => g.Key, g => g.First().Content);

            foreach (var row in model.Rows)
            {
                string prev;
                if (prevByAssignment.TryGetValue(row.AssignmentId, out prev))
                {
                    row.PreviousContent = prev;
                }
            }

            // Bốn tuần liền trước tuần đang xem.
            int hy = y, hw = w;
            for (var i = 0; i < 4; i++)
            {
                PrevWeek(hy, hw, out hy, out hw);

                var weekLogs = myLogs.Where(l => l.Year == hy && l.Week == hw).ToList();
                var reported = weekLogs.Select(l => l.AssignmentId).Distinct().Count();
                var snippet = weekLogs
                    .Select(l => l.Content)
                    .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));

                model.History.Add(new MyReportWeekHistory
                {
                    Year = hy,
                    Week = hw,
                    WeekLabel = "Tuần " + hw,
                    Reported = reported,
                    Total = model.Rows.Count,
                    Snippet = Snippet(snippet)
                });
            }

            return model;
        }

        /// <summary>Tuần liền trước (year, week), tự lùi năm khi ở tuần 1.</summary>
        private static void PrevWeek(int year, int week, out int py, out int pw)
        {
            if (week > 1)
            {
                py = year;
                pw = week - 1;
            }
            else
            {
                py = year - 1;
                pw = WeekHelper.WeeksInYear(py);
            }
        }

        /// <summary>Cắt ngắn một dòng nội dung để hiện trong lịch sử.</summary>
        private static string Snippet(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            var oneLine = content.Replace("\r", " ").Replace("\n", " ").Trim();
            return oneLine.Length > 90 ? oneLine.Substring(0, 90) + "…" : oneLine;
        }

        /// <summary>Nhân sự gắn với tài khoản đang đăng nhập; 0 nếu chưa gắn.</summary>
        private int LinkedMemberId()
        {
            if (CurrentUser == null) return 0;

            var user = Repository.Users.Find(CurrentUser.UserId);
            return user == null ? 0 : user.MemberId;
        }

        /// <summary>Phân công có đang ở trạng thái được phép báo cáo không.</summary>
        private static bool IsReportable(ProjectMember assignment)
        {
            if (assignment == null || string.IsNullOrWhiteSpace(assignment.WorkStatus)) return false;

            return AppSettings.Reporter.ReportableWorkStatuses
                .Any(s => string.Equals(s, assignment.WorkStatus, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
