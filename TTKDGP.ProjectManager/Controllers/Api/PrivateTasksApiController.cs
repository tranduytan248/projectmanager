using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Models.Api;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>
    /// API Giao việc riêng cho Mobile — khép kín toàn bộ luồng tạo, xem, sửa, xóa việc ngoài dự án
    /// kèm điểm thưởng KPI và file đính kèm. Dành cho Quản lý Tổ (quyền wtasks.create).
    /// </summary>
    [ApiAuthorize]
    public class PrivateTasksApiController : BaseController
    {
        private bool CanManagePrivateTasks
        {
            get
            {
                return Can(Permissions.WorkTasks.Perm(Permissions.Create))
                    || IsTeamManager
                    || Can("*");
            }
        }

        [HttpGet]
        public ActionResult Index(string q = null, int assigneeId = 0, string state = null, bool showClosed = false)
        {
            if (!CanManagePrivateTasks) return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ mới có quyền truy cập.");

            var all = WorkService.AllTasks().Where(t => t.Kind == TaskKinds.Standalone).ToList();
            var usersDict = Repository.Users.All().ToDictionary(u => u.Id, u => u.FullName);

            foreach (var t in all)
            {
                if (string.IsNullOrEmpty(t.AssigneeName) && t.AssigneeUserId > 0 && usersDict.ContainsKey(t.AssigneeUserId))
                {
                    t.AssigneeName = usersDict[t.AssigneeUserId];
                }
            }

            var items = all.AsEnumerable();
            if (!showClosed) items = items.Where(t => !TaskStates.IsClosed(t.State));
            if (assigneeId > 0) items = items.Where(t => t.AssigneeUserId == assigneeId);
            if (!string.IsNullOrWhiteSpace(state)) items = items.Where(t => t.State == state);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                items = items.Where(t =>
                    (t.Title ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || (t.AssigneeName ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0);
            }

            var sorted = items.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).ToList();

            var members = WorkService.ActiveUsers()
                .Select(u => new AssigneeOptionDto { UserId = u.Id, FullName = u.FullName })
                .ToList();

            var openTasks = all.Where(t => !TaskStates.IsClosed(t.State)).ToList();

            var result = new PrivateTasksDataDto
            {
                TotalCount = all.Count,
                OverdueCount = openTasks.Count(t => t.IsOverdue),
                InProgressCount = openTasks.Count(t => t.State == TaskStates.InProgress),
                DoneCount = all.Count(t => t.State == TaskStates.Done),
                Members = members,
                Items = sorted.Select(t => ToDto(t, usersDict)).ToList()
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult FormOptions()
        {
            if (!CanManagePrivateTasks) return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ mới có quyền truy cập.");

            var members = WorkService.ActiveUsers()
                .Select(u => new AssigneeOptionDto { UserId = u.Id, FullName = u.FullName })
                .ToList();

            var bonusOptions = PrivateTasksController.BonusOptions.ToList();
            var priorities = TaskPriorities.All.ToList();

            return Json(new PrivateTaskFormOptionsDto
            {
                Members = members,
                BonusOptions = bonusOptions,
                Priorities = priorities
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(string title, int assigneeUserId, DateTime? startDate, DateTime? dueDate,
            string bonusPercent, string priority, string description)
        {
            if (!CanManagePrivateTasks) return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ mới có quyền truy cập.");

            if (string.IsNullOrWhiteSpace(title)) return BadRequest("Vui lòng nhập tên công việc.");
            if (assigneeUserId <= 0) return BadRequest("Vui lòng chọn người được giao việc.");
            if (!dueDate.HasValue) return BadRequest("Vui lòng nhập hạn hoàn thành.");
            if (startDate.HasValue && dueDate.Value.Date < startDate.Value.Date)
            {
                return BadRequest("Hạn hoàn thành phải sau ngày bắt đầu.");
            }

            decimal bonusValue;
            if (!decimal.TryParse((bonusPercent ?? string.Empty).Replace(',', '.'),
                    NumberStyles.Number, CultureInfo.InvariantCulture, out bonusValue)
                || bonusValue < PrivateTasksController.MinBonus || bonusValue > PrivateTasksController.MaxBonus)
            {
                return BadRequest(string.Format("Điểm cộng phải từ {0:0.#} đến {1:0.#}%.",
                    PrivateTasksController.MinBonus, PrivateTasksController.MaxBonus));
            }

            var now = DateTime.Now;
            var actor = CurrentUser == null ? null : CurrentUser.FullName;

            var task = new WorkTask
            {
                Kind = TaskKinds.Standalone,
                Title = title.Trim(),
                ProjectId = 0,
                ProjectName = null,
                ParentId = 0,
                AssigneeUserId = assigneeUserId,
                StartDate = startDate,
                DueDate = dueDate,
                BonusPercent = bonusValue,
                Priority = TaskPriorities.Parse(priority),
                Description = HtmlSanitizer.Clean(description),
                State = TaskStates.NotStarted,
                Progress = 0,
                CreatedAt = now,
                CreatedBy = actor,
                AssignedByUserId = CurrentUserId
            };

            WorkService.FillNames(task);
            WorkService.ApplyState(task, task.State, task.Progress);

            if (Request.Files.Count > 0 && Request.Files[0] != null && Request.Files[0].ContentLength > 0)
            {
                string error;
                if (!CommentAttachments.TrySave(Request.Files[0], task, out error))
                {
                    return BadRequest(error);
                }
            }

            Repository.WorkTasks.Insert(task);

            if (task.AssigneeUserId != CurrentUserId)
            {
                NotificationService.TaskAssigned(task, actor);
            }

            var usersDict = Repository.Users.All().ToDictionary(u => u.Id, u => u.FullName);
            return Json(ToDto(task, usersDict));
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Update(int id, string title, int assigneeUserId, DateTime? startDate, DateTime? dueDate,
            string bonusPercent, string priority, string description, string state, int? progress)
        {
            if (!CanManagePrivateTasks) return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ mới có quyền truy cập.");

            var current = Repository.WorkTasks.Find(id);
            if (current == null || current.Kind != TaskKinds.Standalone) return HttpNotFound();
            if (!CanEditPrivateTask(current)) return new HttpStatusCodeResult(403, "Bạn không có quyền sửa việc này.");

            if (string.IsNullOrWhiteSpace(title)) return BadRequest("Vui lòng nhập tên công việc.");
            if (assigneeUserId <= 0) return BadRequest("Vui lòng chọn người được giao việc.");
            if (!dueDate.HasValue) return BadRequest("Vui lòng nhập hạn hoàn thành.");
            if (startDate.HasValue && dueDate.Value.Date < startDate.Value.Date)
            {
                return BadRequest("Hạn hoàn thành phải sau ngày bắt đầu.");
            }

            decimal bonusValue;
            if (!decimal.TryParse((bonusPercent ?? string.Empty).Replace(',', '.'),
                    NumberStyles.Number, CultureInfo.InvariantCulture, out bonusValue)
                || bonusValue < PrivateTasksController.MinBonus || bonusValue > PrivateTasksController.MaxBonus)
            {
                return BadRequest(string.Format("Điểm cộng phải từ {0:0.#} đến {1:0.#}%.",
                    PrivateTasksController.MinBonus, PrivateTasksController.MaxBonus));
            }

            var now = DateTime.Now;
            var actor = CurrentUser == null ? null : CurrentUser.FullName;
            var previousAssignee = current.AssigneeUserId;

            current.Title = title.Trim();
            current.AssigneeUserId = assigneeUserId;
            current.StartDate = startDate;
            current.DueDate = dueDate;
            current.BonusPercent = bonusValue;
            current.Priority = TaskPriorities.Parse(priority);
            current.Description = HtmlSanitizer.Clean(description);
            current.UpdatedAt = now;
            current.UpdatedBy = actor;

            if (!string.IsNullOrEmpty(state))
            {
                WorkService.ApplyState(current, state, progress);
            }

            WorkService.FillNames(current);

            if (Request.Files.Count > 0 && Request.Files[0] != null && Request.Files[0].ContentLength > 0)
            {
                string error;
                if (!CommentAttachments.TrySave(Request.Files[0], current, out error))
                {
                    return BadRequest(error);
                }
            }

            Repository.WorkTasks.Update(current);

            if (assigneeUserId != previousAssignee && assigneeUserId != CurrentUserId)
            {
                NotificationService.TaskAssigned(current, actor);
            }

            var usersDict = Repository.Users.All().ToDictionary(u => u.Id, u => u.FullName);
            return Json(ToDto(current, usersDict));
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (!CanManagePrivateTasks) return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ mới có quyền truy cập.");

            var current = Repository.WorkTasks.Find(id);
            if (current == null || current.Kind != TaskKinds.Standalone) return HttpNotFound();
            if (!CanEditPrivateTask(current)) return new HttpStatusCodeResult(403, "Bạn không có quyền xóa việc này.");

            if (!string.IsNullOrEmpty(current.AttachmentFile))
            {
                try
                {
                    var fullPath = Server.MapPath("~/App_Data/attachments/" + current.AttachmentFile);
                    if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                }
                catch { }
            }

            Repository.WorkTasks.Delete(id);
            return Json(new { ok = true });
        }

        private PrivateTaskItemDto ToDto(WorkTask task, Dictionary<int, string> usersDict)
        {
            string assignedByName = null;
            if (task.AssignedByUserId > 0 && usersDict.ContainsKey(task.AssignedByUserId))
            {
                assignedByName = usersDict[task.AssignedByUserId];
            }

            return new PrivateTaskItemDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = HtmlSanitizer.ToPlainText(task.Description),
                State = task.State,
                Priority = task.Priority,
                Progress = task.Progress,
                AssigneeUserId = task.AssigneeUserId,
                AssigneeName = task.AssigneeName ?? (usersDict.ContainsKey(task.AssigneeUserId) ? usersDict[task.AssigneeUserId] : null),
                AssignedByUserId = task.AssignedByUserId,
                AssignedByName = assignedByName,
                StartDate = task.StartDate,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                IsOverdue = task.IsOverdue,
                BonusPercent = task.BonusPercent,
                HasAttachment = task.HasAttachment,
                AttachmentName = task.AttachmentName,
                AttachmentSize = task.AttachmentSize,
                CanEdit = CanEditPrivateTask(task),
                CanDelete = CanEditPrivateTask(task),
                CreatedAt = task.CreatedAt
            };
        }
    }
}
