using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Giao việc riêng: Quản lý Tổ giao thẳng một việc (ngoài dự án) cho một cá nhân trong Tổ,
    /// kèm điểm cộng KPI 0,2–1,5% khi hoàn thành đúng hạn và file đính kèm nếu cần.
    ///
    /// Toàn màn dùng quyền wtasks.create ("Giao việc riêng") — chỉ Quản lý Tổ có. Người được
    /// giao thấy việc ở "Công việc của tôi" và báo cáo tiến độ như mọi việc khác.
    /// </summary>
    [AppAuthorize]
    public class PrivateTasksController : BaseController
    {
        /// <summary>Điểm cộng thấp nhất/cao nhất chọn được khi giao việc.</summary>
        public const decimal MinBonus = 0.2m;
        public const decimal MaxBonus = 1.5m;

        /// <summary>
        /// Các mức điểm cộng chọn được (0,2 → 1,5, bước 0,1). Đưa ra dropdown thay vì ô nhập tự
        /// do: khỏi lo người dùng gõ dấu phẩy/dấu chấm lệch culture, và không bao giờ lệch khung.
        /// </summary>
        public static readonly decimal[] BonusOptions = Enumerable.Range(2, 14)
            .Select(i => i / 10m)
            .ToArray();

        [AppAuthorize(Permission = "wtasks.create")]
        public ActionResult Index(string q, int assigneeId = 0, string state = null,
            bool showClosed = false, int page = 1)
        {
            var items = WorkService.AllTasks()
                .Where(t => t.Kind == TaskKinds.Standalone);

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

            var list = WorkService.Sort(items);

            ViewBag.Query = q;
            ViewBag.AssigneeId = assigneeId;
            ViewBag.State = state;
            ViewBag.ShowClosed = showClosed;
            ViewBag.Users = WorkService.ActiveUsers();
            ViewBag.OverdueCount = list.Count(t => t.IsOverdue);

            return View(PagedList<WorkTask>.From(list, page, WorkService.PageSize));
        }

        [HttpGet]
        [AppAuthorize(Permission = "wtasks.create")]
        public ActionResult Edit(int? id)
        {
            WorkTask task;
            if (id.HasValue)
            {
                task = Repository.WorkTasks.Find(id.Value);
                if (task == null || task.Kind != TaskKinds.Standalone) return HttpNotFound();
            }
            else
            {
                task = new WorkTask
                {
                    Kind = TaskKinds.Standalone,
                    State = TaskStates.NotStarted,
                    StartDate = DateTime.Today,
                    DueDate = DateTime.Today.AddDays(3),
                    BonusPercent = MinBonus
                };
            }

            PopulateLists();

            // Mở từ hộp thoại thì chỉ trả phần form; mở thẳng bằng đường dẫn thì trả cả trang.
            return Request.IsAjaxRequest() ? (ActionResult)PartialView("_EditForm", task) : View(task);
        }

        // Điểm cộng nhận dạng CHUỖI rồi tự parse theo InvariantCulture — model binder mặc định
        // đọc số thập phân theo culture vi-VN (dấu phẩy) sẽ hiểu sai "0.5".
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.create")]
        public ActionResult Edit(WorkTask model, string bonus, HttpPostedFileBase file)
        {
            model.Kind = TaskKinds.Standalone;
            model.ProjectId = 0;
            model.ProjectName = null;
            model.ParentId = 0;

            decimal bonusValue;
            if (!decimal.TryParse((bonus ?? string.Empty).Replace(',', '.'),
                    NumberStyles.Number, CultureInfo.InvariantCulture, out bonusValue)
                || bonusValue < MinBonus || bonusValue > MaxBonus)
            {
                ModelState.AddModelError("BonusPercent",
                    string.Format("Điểm cộng phải từ {0:0.#} đến {1:0.#}%.", MinBonus, MaxBonus));
            }
            model.BonusPercent = bonusValue;

            if (model.AssigneeUserId <= 0)
            {
                ModelState.AddModelError("AssigneeUserId", "Hãy chọn người được giao việc.");
            }

            if (!model.DueDate.HasValue)
            {
                ModelState.AddModelError("DueDate", "Vui lòng nhập hạn hoàn thành.");
            }
            else if (model.StartDate.HasValue && model.DueDate.Value.Date < model.StartDate.Value.Date)
            {
                ModelState.AddModelError("DueDate", "Hạn hoàn thành phải sau ngày bắt đầu.");
            }

            WorkTask current = null;
            if (model.Id != 0)
            {
                current = Repository.WorkTasks.Find(model.Id);
                if (current == null || current.Kind != TaskKinds.Standalone) return HttpNotFound();
            }

            if (!ModelState.IsValid) return EditFormWithErrors(model);

            WorkService.FillNames(model);
            var now = DateTime.Now;
            var actor = CurrentUser == null ? null : CurrentUser.FullName;

            if (model.Id == 0)
            {
                model.CreatedAt = now;
                model.CreatedBy = actor;
                model.AssignedByUserId = CurrentUserId;
                WorkService.ApplyState(model, model.State, model.Progress);

                string error;
                if (!CommentAttachments.TrySave(file, model, out error))
                {
                    ModelState.AddModelError("", error);
                    return EditFormWithErrors(model);
                }

                Repository.WorkTasks.Insert(model);

                // Báo web + email cho người được giao (trừ khi tự giao cho chính mình).
                if (model.AssigneeUserId != CurrentUserId)
                {
                    NotificationService.TaskAssigned(model, actor);
                }

                Notify(string.Format("Đã giao việc \"{0}\" cho {1} — đúng hạn được cộng {2:0.#}% KPI.",
                    model.Title, model.AssigneeName, model.BonusPercent));
            }
            else
            {
                // Giữ dấu vết tạo, file đính kèm cũ và mốc hoàn thành — người sửa không ghi đè lịch sử.
                model.CreatedAt = current.CreatedAt;
                model.CreatedBy = current.CreatedBy;
                model.AssignedByUserId = current.AssignedByUserId;
                model.CompletedAt = current.CompletedAt;
                model.SortOrder = current.SortOrder;
                model.AttachmentFile = current.AttachmentFile;
                model.AttachmentName = current.AttachmentName;
                model.AttachmentSize = current.AttachmentSize;
                model.UpdatedAt = now;
                model.UpdatedBy = actor;

                string error;
                if (!CommentAttachments.TrySave(file, model, out error))
                {
                    ModelState.AddModelError("", error);
                    return EditFormWithErrors(model);
                }

                WorkService.ApplyState(model, model.State, model.Progress);
                Repository.WorkTasks.Update(model);

                // Đổi người thực hiện thì báo cho người MỚI như một lần giao việc.
                if (model.AssigneeUserId != current.AssigneeUserId && model.AssigneeUserId != CurrentUserId)
                {
                    NotificationService.TaskAssigned(model, actor);
                }

                Notify(string.Format("Đã cập nhật việc \"{0}\".", model.Title));
            }

            return Saved("Index");
        }

        /// <summary>
        /// Trả lại form kèm lỗi. Mở trong hộp thoại thì chỉ trả phần form để lỗi hiện tại chỗ,
        /// không mất dữ liệu đã nhập; mở thẳng bằng đường dẫn thì trả cả trang.
        /// </summary>
        private ActionResult EditFormWithErrors(WorkTask model)
        {
            PopulateLists();

            return Request.IsAjaxRequest()
                ? (ActionResult)PartialView("_EditForm", model)
                : View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.delete")]
        public ActionResult Delete(int id)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || task.Kind != TaskKinds.Standalone) return HttpNotFound();

            // Việc đã vào phiếu chấm KPI thì không xoá — xoá đi là phiếu đã duyệt mất căn cứ.
            if (Repository.KpiLines.All().Any(l => l.TaskId == id))
            {
                NotifyError(string.Format(
                    "Không xoá được \"{0}\" vì đã nằm trong bảng chấm KPI. Hãy chuyển trạng thái sang Huỷ.",
                    task.Title));
                return RedirectToAction("Index");
            }

            // Dọn file trên đĩa: của chính việc và của các trao đổi kèm theo.
            if (task.HasAttachment) CommentAttachments.Delete(task.AttachmentFile);
            foreach (var c in Repository.WorkComments.All().Where(c => c.TaskId == id && c.HasAttachment))
            {
                CommentAttachments.Delete(c.AttachmentFile);
            }

            Repository.WorkComments.DeleteWhere(c => c.TaskId == id);
            Repository.WorkTasks.Delete(id);

            Notify(string.Format("Đã xoá việc \"{0}\".", task.Title));
            return RedirectToAction("Index");
        }

        private void PopulateLists()
        {
            ViewBag.Users = WorkService.ActiveUsers();
            ViewBag.States = TaskStates.All;
            ViewBag.BonusOptions = BonusOptions;
        }
    }
}
