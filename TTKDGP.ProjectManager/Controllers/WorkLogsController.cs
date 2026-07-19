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
    /// Nhật ký công việc theo tuần của một phân công, hiển thị dưới dạng timeline.
    /// Timeline xem công khai như phần còn lại của FrontEnd; thêm/sửa/xoá thì phải đăng nhập.
    /// </summary>
    public class WorkLogsController : BaseController
    {
        /// <summary>Timeline tiến trình của một thành viên trên một dự án.</summary>
        public ActionResult Timeline(int id)
        {
            var assignment = Repository.Assignments.Find(id);
            if (assignment == null)
            {
                Response.StatusCode = 404;
                ViewData["Title"] = "Không tìm thấy phân công";
                ViewData["Message"] = "Phân công bạn tìm không tồn tại hoặc đã bị xoá.";
                return View("Error");
            }

            return View(BuildTimeline(assignment));
        }

        [HttpGet]
        [AppAuthorize]
        public ActionResult Edit(int? id, int? assignmentId)
        {
            WorkLog log;
            ProjectMember assignment;

            if (id.HasValue)
            {
                log = Repository.WorkLogs.Find(id.Value);
                if (log == null) return HttpNotFound();

                assignment = Repository.Assignments.Find(log.AssignmentId);
            }
            else
            {
                if (!assignmentId.HasValue) return HttpNotFound();

                assignment = Repository.Assignments.Find(assignmentId.Value);
                if (assignment == null) return HttpNotFound();

                // Mặc định ghi cho tuần hiện tại.
                log = new WorkLog
                {
                    AssignmentId = assignment.Id,
                    Year = WeekHelper.CurrentYear,
                    Week = WeekHelper.CurrentWeek,
                    Status = assignment.WorkStatus,
                    Content = string.Empty
                };
            }

            if (assignment == null) return HttpNotFound();

            var model = new WorkLogEditViewModel
            {
                Id = log.Id,
                AssignmentId = log.AssignmentId,
                Year = log.Year,
                Week = log.Week,
                Content = log.Content,
                Status = log.Status
            };

            PopulateContext(model, assignment);
            return View(model);
        }

        [HttpPost]
        [AppAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(WorkLogEditViewModel model)
        {
            var assignment = Repository.Assignments.Find(model.AssignmentId);
            if (assignment == null) return HttpNotFound();

            // Tuần vượt quá số tuần thực có của năm đó (52 hoặc 53).
            if (model.Year >= 2000 && model.Year <= 2100)
            {
                var maxWeek = WeekHelper.WeeksInYear(model.Year);
                if (model.Week > maxWeek)
                {
                    ModelState.AddModelError("Week",
                        string.Format("Năm {0} chỉ có {1} tuần.", model.Year, maxWeek));
                }
            }

            // Mỗi tuần chỉ có một dòng nhật ký cho mỗi phân công.
            var duplicate = Repository.WorkLogs.FirstOrDefault(w =>
                w.Id != model.Id &&
                w.AssignmentId == model.AssignmentId &&
                w.Year == model.Year &&
                w.Week == model.Week);
            if (duplicate != null)
            {
                ModelState.AddModelError("",
                    string.Format("Tuần {0}/{1} đã có nhật ký. Hãy sửa dòng đó thay vì tạo mới.",
                        model.Week, model.Year));
            }

            if (!ModelState.IsValid)
            {
                PopulateContext(model, assignment);
                return View(model);
            }

            var userName = CurrentUser == null ? null : CurrentUser.Name;

            if (model.Id == 0)
            {
                Repository.WorkLogs.Insert(new WorkLog
                {
                    AssignmentId = assignment.Id,
                    ProjectId = assignment.ProjectId,
                    MemberId = assignment.MemberId,
                    Year = model.Year,
                    Week = model.Week,
                    Content = model.Content.Trim(),
                    Status = model.Status,
                    CreatedBy = userName,
                    CreatedAt = DateTime.Now
                });
                Notify(string.Format("Đã ghi nhật ký tuần {0}/{1}.", model.Week, model.Year));
            }
            else
            {
                var log = Repository.WorkLogs.Find(model.Id);
                if (log == null) return HttpNotFound();

                log.Year = model.Year;
                log.Week = model.Week;
                log.Content = model.Content.Trim();
                log.Status = model.Status;
                log.UpdatedAt = DateTime.Now;
                Repository.WorkLogs.Update(log);

                Notify(string.Format("Đã cập nhật nhật ký tuần {0}/{1}.", model.Week, model.Year));
            }

            // Trạng thái và nội dung của phân công luôn phản ánh mốc mới nhất.
            Repository.SyncAssignmentFromLatestLog(assignment.Id);

            return RedirectToAction("Timeline", new { id = assignment.Id });
        }

        [HttpPost]
        [AppAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var log = Repository.WorkLogs.Find(id);
            if (log == null) return HttpNotFound();

            var assignmentId = log.AssignmentId;
            Repository.WorkLogs.Delete(id);
            Repository.SyncAssignmentFromLatestLog(assignmentId);

            Notify(string.Format("Đã xoá nhật ký tuần {0}/{1}.", log.Week, log.Year));
            return RedirectToAction("Timeline", new { id = assignmentId });
        }

        private static TimelineViewModel BuildTimeline(ProjectMember assignment)
        {
            var project = Repository.Projects.Find(assignment.ProjectId);
            var member = Repository.Members.Find(assignment.MemberId);
            var logs = Repository.LogsOf(assignment.Id);

            var currentYear = WeekHelper.CurrentYear;
            var currentWeek = WeekHelper.CurrentWeek;

            var model = new TimelineViewModel
            {
                AssignmentId = assignment.Id,
                ProjectId = assignment.ProjectId,
                ProjectName = project != null ? project.Name : assignment.ProjectName,
                ProjectStatus = project != null ? project.CurrentStatus : null,
                Customer = project != null ? project.Customer : null,
                MemberId = assignment.MemberId,
                MemberName = member != null ? member.FullName : assignment.MemberName,
                Role = assignment.Role,
                CurrentStatus = assignment.WorkStatus,
                IsActive = assignment.IsActive,
                TotalEntries = logs.Count,
                HasCurrentWeekLog = logs.Any(w => w.Year == currentYear && w.Week == currentWeek)
            };

            model.Years = logs
                .GroupBy(w => w.Year)
                .OrderByDescending(g => g.Key)
                .Select(g => new TimelineYear
                {
                    Year = g.Key,
                    Entries = g
                        .OrderByDescending(w => w.Week)
                        .Select(w => new TimelineEntry
                        {
                            Log = w,
                            WeekLabel = WeekHelper.LabelWithYear(w.Year, w.Week),
                            IsCurrentWeek = w.Year == currentYear && w.Week == currentWeek,
                            IsLatest = false
                        })
                        .ToList()
                })
                .ToList();

            // Mốc đầu tiên của năm mới nhất chính là mốc mới nhất.
            if (model.Years.Count > 0 && model.Years[0].Entries.Count > 0)
            {
                model.Years[0].Entries[0].IsLatest = true;
            }

            return model;
        }

        private static void PopulateContext(WorkLogEditViewModel model, ProjectMember assignment)
        {
            var project = Repository.Projects.Find(assignment.ProjectId);
            var member = Repository.Members.Find(assignment.MemberId);

            model.ProjectName = project != null ? project.Name : assignment.ProjectName;
            model.MemberName = member != null ? member.FullName : assignment.MemberName;
            model.Role = assignment.Role;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            ViewBag.WorkStatuses = Repository.FilterableWorkStatuses();
            ViewBag.YearOptions = WeekHelper.YearOptions(Repository.YearsInLogs());
        }
    }
}
