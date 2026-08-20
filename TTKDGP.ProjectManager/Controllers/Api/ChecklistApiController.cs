using System;
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
    /// Danh sach + tao moi dau viec cua mot du an — man "Checklist" cua mobile, tuong duong
    /// ChecklistController.Index/Edit ben web (ban GRID, khong phai Kanban) nhung tra danh sach
    /// phang mot lan (khong PagedList, khong cay cha/con) — mobile tu loc/tim theo tu khoa o
    /// client. Form tao moi RUT GON so voi web: khong co Muc cha (luon tao muc goc), Ngay bat dau
    /// (tu gan hom nay), Tuan/Nam (tu suy tu Han hoan thanh), Trang thai (luon "Chua bat dau" luc
    /// tao) — quyet dinh nay ghi trong Memory.md o goc repo, muc "Nut Thêm mới trong Checklist mobile".
    /// </summary>
    [ApiAuthorize]
    public class ChecklistApiController : BaseController
    {
        [HttpGet]
        public ActionResult Index(int projectId)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null || !CanViewProject(projectId)) return HttpNotFound();

            // Y het ChecklistController.Index: chi lay Checklist + HoTro, KHONG ke viec ngoai du
            // an (NgoaiDuAn khong bao gio gan ProjectId nen von da khong lot vao day).
            var tasks = WorkService.TasksOfProject(projectId)
                .Where(t => TaskKinds.InProject.Contains(t.Kind))
                .ToList();

            var dto = new ChecklistDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                PmName = project.PmName,
                CanEdit = CanEditProject(projectId),
                TotalCount = tasks.Count,
                DoneCount = tasks.Count(t => t.State == TaskStates.Done),
                OverdueCount = tasks.Count(t => t.IsOverdue),
                Tasks = tasks.Select(t => ApiMappers.ToDto(t, CanEditTask(t))).ToList(),
                Assignees = ActiveMemberIds(projectId)
                    .Select(id => Repository.Users.Find(id))
                    .Where(u => u != null)
                    .OrderBy(u => u.FullName, System.StringComparer.CurrentCulture)
                    .Select(u => new AssigneeOptionDto { UserId = u.Id, FullName = u.FullName })
                    .ToList()
            };
            dto.DonePercent = dto.TotalCount == 0 ? 0 : (int)System.Math.Round(dto.DoneCount * 100.0 / dto.TotalCount);

            return Json(dto, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Chi tiet DAY DU mot dau viec — man "Chi tiet cong viec" cua mobile, tuong duong hop
        /// thoai ChecklistController.Detail/_Detail.cshtml ben web nhung gop LUON ca 3 khoi
        /// tuong tac (Gio cong/Viec can lam/Trao doi) trong MOT lan goi, thay vi 4 request rieng
        /// nhu web (Detail tra HTML, 3 khoi con lai nap AJAX rieng khi mo hop thoai). Dung
        /// CanSeeTask thay vi CanViewProject — y het luat ben web: giao cho minh HOAC thanh vien
        /// du an HOAC Quan ly To — nen xem duoc ca viec ngoai du an cua chinh minh.
        /// </summary>
        [HttpGet]
        public ActionResult Detail(int id)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            return Json(BuildFullDetail(task), JsonRequestBehavior.AllowGet);
        }

        /// <summary>Dung toan bo goi TaskFullDetailDto — dung lai NGUYEN VEN cac service/quyen da
        /// co san (TimeLogService.BuildViewModel, BaseController.BuildTaskTodos/CanManageTodos,
        /// WorkService.CommentsOfTask), khong tinh lai luat o day.</summary>
        private TaskFullDetailDto BuildFullDetail(WorkTask task)
        {
            var parent = task.ParentId > 0 ? Repository.WorkTasks.Find(task.ParentId) : null;
            var parentTitle = parent == null
                ? null
                : (string.IsNullOrWhiteSpace(parent.Code) ? parent.Title : parent.Code + " · " + parent.Title);

            var taskOpen = !TaskStates.IsClosed(task.State);

            return new TaskFullDetailDto
            {
                Task = ApiMappers.ToFullTaskDto(task, parentTitle, CanEditTask(task), CanEditAllOfTask(task)),
                TimeLog = ApiMappers.ToDto(TimeLogService.BuildViewModel(task, CurrentUserId), taskOpen),
                Todo = ApiMappers.ToDto(BuildTaskTodos(task)),
                Comments = BuildCommentsDto(task)
            };
        }

        /// <summary>Y het ChecklistController.BuildComments — gom ca Participants de mobile dung
        /// goi y khi go @.</summary>
        private CommentsSummaryDto BuildCommentsDto(WorkTask task)
        {
            return ApiMappers.ToCommentsDto(WorkService.CommentsOfTask(task.Id),
                WorkService.TaskParticipants(task), CurrentUserId, CanEditProject(task.ProjectId));
        }

        // ---------- Giờ công ----------

        /// <summary>Mirror ChecklistController.LogTime — cach doc "hours"/"workDate" GIU NGUYEN
        /// (InvariantCulture, khong de model binder tu suy dien theo culture vi-VN).</summary>
        [HttpPost]
        public ActionResult LogTime(int id, string workDate, string hours, string note)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            DateTime dateValue;
            if (!DateTime.TryParseExact((workDate ?? string.Empty).Trim(), "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out dateValue))
            {
                return BadRequest("Ngày làm không hợp lệ.");
            }

            decimal hoursValue;
            if (!decimal.TryParse((hours ?? string.Empty).Trim().Replace(',', '.'),
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out hoursValue))
            {
                return BadRequest("Số giờ không hợp lệ. Nhập ví dụ 2 hoặc 2,5.");
            }

            if (CurrentUserId <= 0 || task.AssigneeUserId != CurrentUserId)
            {
                return BadRequest("Chỉ người được giao việc mới ghi được giờ công.");
            }

            if (TaskStates.IsClosed(task.State))
            {
                return BadRequest("Công việc đã đóng nên không ghi thêm giờ được.");
            }

            var error = TimeLogService.Add(task, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName, dateValue, hoursValue, note);
            if (error != null) return BadRequest(error);

            TaskActivityLogService.Record(task.Id, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName, TaskActivityActions.TimeLogAdded,
                string.Format("Đã ghi {0} giờ ({1:dd/MM/yyyy})", hoursValue, dateValue));

            return Json(ApiMappers.ToDto(TimeLogService.BuildViewModel(task, CurrentUserId),
                !TaskStates.IsClosed(task.State)));
        }

        /// <summary>Mirror ChecklistController.DeleteTimeLog — chi xoa duoc dong cua chinh minh,
        /// viec chua dong.</summary>
        [HttpPost]
        public ActionResult DeleteTimeLog(int id, int logId)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            var log = Repository.WorkTimeLogs.Find(logId);
            if (log == null || log.TaskId != id) return HttpNotFound();

            if (CurrentUserId <= 0 || log.UserId != CurrentUserId)
            {
                return BadRequest("Chỉ xoá được dòng giờ của chính bạn.");
            }

            if (TaskStates.IsClosed(task.State))
            {
                return BadRequest("Công việc đã đóng nên không sửa được giờ đã ghi.");
            }

            // Y het ChecklistController.DeleteTimeLog — Dang lam chi dat duoc sau khi da ghi gio
            // (TimeLogService.ValidateStateChange), xoa mat gio ma van giu trang thai do la sai logic.
            if (task.State == TaskStates.InProgress)
            {
                return BadRequest(
                    "Việc đang \"Đang làm\" nên không xoá được giờ đã ghi — xoá sẽ khiến việc " +
                    "mất hết căn cứ giờ công trong khi vẫn đang ở trạng thái này.");
            }

            Repository.WorkTimeLogs.Delete(logId);
            TaskActivityLogService.Record(task.Id, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName, TaskActivityActions.TimeLogDeleted,
                string.Format("Đã xoá lượt ghi {0} giờ ({1:dd/MM/yyyy})", log.Hours, log.WorkDate));

            return Json(ApiMappers.ToDto(TimeLogService.BuildViewModel(task, CurrentUserId),
                !TaskStates.IsClosed(task.State)));
        }

        // ---------- Việc cần làm ----------

        [HttpPost]
        public ActionResult AddTodo(int id, string content)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();
            if (!CanManageTodos(task)) return BadRequest("Bạn không có quyền sửa việc cần làm của việc này.");

            var text = (content ?? string.Empty).Trim();
            if (text.Length == 0) return BadRequest("Vui lòng nhập nội dung.");
            if (text.Length > 300) return BadRequest("Nội dung tối đa 300 ký tự.");

            var nextOrder = Repository.WorkTaskTodos.All().Count(t => t.TaskId == id);

            var todo = Repository.WorkTaskTodos.Insert(new WorkTaskTodo
            {
                TaskId = id,
                Content = text,
                SortOrder = nextOrder,
                CreatedByUserId = CurrentUserId,
                CreatedByName = CurrentUser == null ? null : CurrentUser.FullName,
                CreatedAt = DateTime.Now
            });

            NotificationService.TodoAdded(task, todo, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName);

            TaskActivityLogService.Record(task.Id, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName, TaskActivityActions.TodoAdded,
                string.Format("Đã thêm việc cần làm \"{0}\"", text));

            return Json(ApiMappers.ToDto(BuildTaskTodos(task)));
        }

        [HttpPost]
        public ActionResult ToggleTodo(int id, int todoId)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();
            if (!CanManageTodos(task)) return BadRequest("Bạn không có quyền sửa việc cần làm của việc này.");

            var todo = Repository.WorkTaskTodos.Find(todoId);
            if (todo == null || todo.TaskId != id) return HttpNotFound();

            todo.IsDone = !todo.IsDone;
            if (todo.IsDone)
            {
                todo.DoneAt = DateTime.Now;
                todo.DoneByUserId = CurrentUserId;
            }
            else
            {
                todo.DoneAt = null;
                todo.DoneByUserId = 0;
            }

            Repository.WorkTaskTodos.Update(todo);

            NotificationService.TodoToggled(task, todo, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName);

            TaskActivityLogService.Record(task.Id, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName, TaskActivityActions.TodoToggled,
                string.Format("Đã đánh dấu {0} việc cần làm \"{1}\"",
                    todo.IsDone ? "xong" : "chưa xong", todo.Content));

            return Json(ApiMappers.ToDto(BuildTaskTodos(task)));
        }

        [HttpPost]
        public ActionResult EditTodo(int id, int todoId, string content)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();
            if (!CanManageTodos(task)) return BadRequest("Bạn không có quyền sửa việc cần làm của việc này.");

            var todo = Repository.WorkTaskTodos.Find(todoId);
            if (todo == null || todo.TaskId != id) return HttpNotFound();

            var text = (content ?? string.Empty).Trim();
            if (text.Length == 0) return BadRequest("Vui lòng nhập nội dung.");
            if (text.Length > 300) return BadRequest("Nội dung tối đa 300 ký tự.");

            var oldContent = todo.Content;
            todo.Content = text;
            Repository.WorkTaskTodos.Update(todo);

            if (!string.Equals(oldContent, text, StringComparison.Ordinal))
            {
                TaskActivityLogService.Record(task.Id, CurrentUserId,
                    CurrentUser == null ? null : CurrentUser.FullName, TaskActivityActions.TodoEdited,
                    string.Format("Đã sửa việc cần làm: \"{0}\" → \"{1}\"", oldContent, text));
            }

            return Json(ApiMappers.ToDto(BuildTaskTodos(task)));
        }

        [HttpPost]
        public ActionResult DeleteTodo(int id, int todoId)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();
            if (!CanManageTodos(task)) return BadRequest("Bạn không có quyền sửa việc cần làm của việc này.");

            var todo = Repository.WorkTaskTodos.Find(todoId);
            if (todo == null || todo.TaskId != id) return HttpNotFound();

            Repository.WorkTaskTodos.Delete(todoId);
            TaskActivityLogService.Record(task.Id, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName, TaskActivityActions.TodoDeleted,
                string.Format("Đã xoá việc cần làm \"{0}\"", todo.Content));

            return Json(ApiMappers.ToDto(BuildTaskTodos(task)));
        }

        // ---------- Trao đổi ----------

        /// <summary>Mirror ChecklistController.Comment — mobile gio co trinh soan thao rich text
        /// rieng (AppRichEditor) tu xuat dung whitelist the cua HtmlSanitizer, nen lai dung
        /// Clean() y het web (khac ban truoc: luc do mobile chi gui van ban thuan nen Clean() lam
        /// mat noi dung co ky tu "&lt;"/"&gt;" ngau nhien — nay dau vao luon la HTML hop le that
        /// nen khong con mau thuan). [ValidateInput(false)] bat buoc vi content la HTML, giong
        /// ChecklistController.Comment ben web.</summary>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Comment(int id, string content, HttpPostedFileBase file)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            var cleaned = HtmlSanitizer.Clean(content);
            var hasText = !string.IsNullOrWhiteSpace(cleaned);
            var hasFile = file != null && file.ContentLength > 0;

            if (!hasText && !hasFile) return BadRequest("Vui lòng nhập nội dung.");

            var comment = new WorkComment
            {
                TaskId = id,
                UserId = CurrentUserId,
                AuthorName = CurrentUser == null ? "(không rõ)" : CurrentUser.FullName,
                Content = hasText ? cleaned : null,
                CreatedAt = DateTime.Now
            };

            string error;
            if (!CommentAttachments.TrySave(file, comment, out error))
            {
                return BadRequest(error);
            }

            Repository.WorkComments.Insert(comment);

            NotificationService.Mentions(task, comment, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName);
            NotificationService.CommentAdded(task, comment, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName);

            TaskActivityLogService.Record(task.Id, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName, TaskActivityActions.CommentAdded,
                "Đã thêm bình luận");

            return Json(BuildCommentsDto(task));
        }

        /// <summary>Mirror ChecklistController.Attachment — ai xem duoc dau viec chua binh luan
        /// nay thi tai duoc file. Luon tra octet-stream de trinh duyet/thiet bi TAI VE chu khong
        /// nhung/thuc thi, dung y het ly do bao mat ben web.</summary>
        [HttpGet]
        public ActionResult Attachment(int commentId)
        {
            var comment = Repository.WorkComments.Find(commentId);
            if (comment == null || comment.IsDeleted || !comment.HasAttachment) return HttpNotFound();

            var task = Repository.WorkTasks.Find(comment.TaskId);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            var path = CommentAttachments.FullPath(comment.AttachmentFile);
            if (path == null) return HttpNotFound();

            return File(path, "application/octet-stream",
                string.IsNullOrWhiteSpace(comment.AttachmentName) ? "tep-dinh-kem" : comment.AttachmentName);
        }

        /// <summary>Mirror ChecklistController.RecallComment — chi chinh chu, PM du an hoac Quan
        /// ly To moi thu hoi duoc.</summary>
        [HttpPost]
        public ActionResult RecallComment(int id, int commentId)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            var comment = Repository.WorkComments.Find(commentId);
            if (comment == null || comment.TaskId != id) return HttpNotFound();

            if (comment.UserId != CurrentUserId && !CanEditProject(task.ProjectId)) return HttpNotFound();

            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.Now;
            Repository.WorkComments.Update(comment);

            TaskActivityLogService.Record(task.Id, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName, TaskActivityActions.CommentRecalled,
                "Đã thu hồi bình luận");

            return Json(BuildCommentsDto(task));
        }

        // ---------- Trang thai + Lich su ----------

        /// <summary>
        /// Doi trang thai + tien do — mirror MyWorkController.Report (chon Trang thai + Tien do% +
        /// ghi chu tuy chon), GIU NGUYEN luat chan "chua ghi gio thi khong duoc chuyen Dang lam/Hoan
        /// thanh" (TimeLogService.ValidateStateChange) dung y het web, khong noi long cho mobile.
        /// </summary>
        [HttpPost]
        public ActionResult UpdateStatus(int id, string state, int progress, string note)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();
            if (!CanEditTask(task)) return BadRequest("Bạn không có quyền cập nhật trạng thái việc này.");

            if (!TaskStates.All.Contains(state)) return BadRequest("Trạng thái không hợp lệ.");

            var logError = TimeLogService.ValidateStateChange(task, state);
            if (logError != null) return BadRequest(logError);

            var before = TaskActivityLogService.Snapshot(task);
            WorkService.ApplyState(task, state, progress);

            if (!string.IsNullOrWhiteSpace(note))
            {
                var stamp = string.Format("[{0:dd/MM/yyyy HH:mm} — {1}] ",
                    DateTime.Now, CurrentUser == null ? "?" : CurrentUser.FullName);

                task.Description = string.IsNullOrWhiteSpace(task.Description)
                    ? stamp + note.Trim()
                    : task.Description + Environment.NewLine + stamp + note.Trim();
            }

            task.UpdatedAt = DateTime.Now;
            task.UpdatedBy = CurrentUser == null ? null : CurrentUser.FullName;
            Repository.WorkTasks.Update(task);
            TaskActivityLogService.RecordFieldChanges(before, task, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName);

            return Json(BuildFullDetail(task));
        }

        /// <summary>Nhat ky thao tac cua mot dau viec, moi nhat truoc — ai xem duoc cong viec thi
        /// xem duoc lich su (CanSeeTask), khong thu hep hon.</summary>
        [HttpGet]
        public ActionResult ActivityLog(int id)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            var items = TaskActivityLogService.GetForTask(id).Select(ApiMappers.ToDto).ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        /// <summary>Danh sach thanh vien dang hoat dong cua du an chua dau viec nay — do form "Doi
        /// nguoi thuc hien" tren mobile can truoc khi mo. Chi PM du an hoac Quan ly To moi xem duoc
        /// (CanEditAllOfTask) — nguoi thuc hien thuong khong duoc doi truong nay, xem quy dinh o
        /// BaseController.CanEditAllOfTask.</summary>
        [HttpGet]
        public ActionResult AssigneeOptions(int id)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();
            if (!CanEditAllOfTask(task)) return HttpNotFound();

            var options = ActiveMemberIds(task.ProjectId)
                .Select(uid => Repository.Users.Find(uid))
                .Where(u => u != null)
                .OrderBy(u => u.FullName, System.StringComparer.CurrentCulture)
                .Select(u => new AssigneeOptionDto { UserId = u.Id, FullName = u.FullName })
                .ToList();

            return Json(options, JsonRequestBehavior.AllowGet);
        }

        /// <summary>Doi nguoi thuc hien cua mot dau viec da co — chi PM du an hoac Quan ly To
        /// (CanEditAllOfTask), y het luat "sua MOI truong" da ap dung cho web (ChecklistController.
        /// Edit, nhanh canEditAll). assigneeUserId=0 nghia la go giao (chua giao lai cho ai).</summary>
        [HttpPost]
        public ActionResult UpdateAssignee(int id, int assigneeUserId)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();
            if (!CanEditAllOfTask(task))
            {
                return BadRequest("Chỉ PM dự án hoặc Quản lý Tổ mới đổi được người thực hiện.");
            }

            if (assigneeUserId > 0 && !ActiveMemberIds(task.ProjectId).Contains(assigneeUserId))
            {
                return BadRequest("Người thực hiện phải đang tham gia dự án này.");
            }

            var before = TaskActivityLogService.Snapshot(task);
            var previousAssignee = task.AssigneeUserId;

            task.AssigneeUserId = assigneeUserId;
            WorkService.FillNames(task);
            task.UpdatedAt = DateTime.Now;
            task.UpdatedBy = CurrentUser == null ? null : CurrentUser.FullName;
            Repository.WorkTasks.Update(task);
            TaskActivityLogService.RecordFieldChanges(before, task, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName);

            // Bao cho nguoi MOI duoc giao (tru khi tu giao cho minh) — y het Create/Edit ben web.
            if (assigneeUserId > 0 && assigneeUserId != previousAssignee && assigneeUserId != CurrentUserId)
            {
                NotificationService.ProjectTaskAssigned(task, CurrentUser == null ? null : CurrentUser.FullName);
            }

            return Json(BuildFullDetail(task));
        }

        /// <summary>
        /// Them mot dau viec moi — chi PM du an hoac Quan ly To (CanEditProject). Form rut gon:
        /// khong nhan ParentId/StartDate/Week/Year/State tu client, tu gan gia tri mac dinh y het
        /// quyet dinh trong Memory.md. Ma viec (Code) KHONG bat buoc va KHONG kiem trung — dung
        /// dung thuc te ben web (WorkTask.Code chi co [StringLength], khong co [Required] hay
        /// kiem trung, du _EditForm.cshtml co ghi chu "chi can duy nhat" nhu mot quy uoc, khong
        /// phai rang buoc thuc su). [todos] la danh sach "Viec can lam" nhap ngay luc tao (man
        /// mobile moi gop todo-list vao form Them cong viec) — tao het trong CUNG mot request thay
        /// vi bat client goi AddTodo nhieu lan noi tiep, tranh nua chung lam nua bo do loi mang.
        /// [ValidateInput(false)] bat buoc vi description gio la HTML that tu AppRichEditor (truoc
        /// day luon la chu thuong nen chua can) — giong het ly do action Comment/Edit da tat.
        /// </summary>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(int projectId, string title, string code, string kind,
            string priority, int assigneeUserId, DateTime? startDate, DateTime? dueDate,
            string description, string[] todos)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null || !CanEditProject(projectId)) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(title)) return BadRequest("Vui lòng nhập tên công việc.");
            if (!dueDate.HasValue) return BadRequest("Vui lòng nhập hạn hoàn thành.");

            // Loc bo dong rong TRUOC khi tao task — kiem tra do dai ngay tai day de khong bao gio
            // roi vao canh "task da tao nhung todo loi", giong tinh than validate-truoc-khi-luu
            // dang dung cho Ten/Han hoan thanh o tren.
            var todoLines = (todos ?? new string[0])
                .Select(t => (t ?? string.Empty).Trim())
                .Where(t => t.Length > 0)
                .ToList();
            if (todoLines.Any(t => t.Length > 300))
            {
                return BadRequest("Mỗi việc cần làm tối đa 300 ký tự.");
            }

            var memberIds = ActiveMemberIds(projectId);
            if (assigneeUserId > 0 && !memberIds.Contains(assigneeUserId))
            {
                return BadRequest("Người thực hiện phải đang tham gia dự án này.");
            }

            var due = dueDate.Value;
            var task = new WorkTask
            {
                ProjectId = projectId,
                ParentId = 0,
                Kind = TaskKinds.InProject.Contains(kind) ? kind : TaskKinds.Checklist,
                Priority = TaskPriorities.Parse(priority),
                Title = title.Trim(),
                Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
                AssigneeUserId = assigneeUserId,
                StartDate = startDate ?? DateTime.Today,
                DueDate = due,
                Week = WeekHelper.GetWeek(due),
                Year = WeekHelper.GetYear(due),
                State = TaskStates.NotStarted,
                Progress = 0,
                Description = HtmlSanitizer.Clean(description)
            };

            WorkService.FillNames(task);

            var siblings = Repository.WorkTasks.All()
                .Where(t => t.ProjectId == projectId && TaskKinds.InProject.Contains(t.Kind) && t.ParentId == 0)
                .ToList();
            task.SortOrder = siblings.Count == 0 ? 10 : siblings.Max(t => t.SortOrder) + 10;

            task.CreatedAt = DateTime.Now;
            task.CreatedBy = CurrentUser == null ? null : CurrentUser.FullName;
            task.AssignedByUserId = CurrentUserId;

            Repository.WorkTasks.Insert(task);

            // Y het ChecklistController.Edit: bao cho nguoi duoc giao (tru khi tu giao cho minh).
            if (task.AssigneeUserId > 0 && task.AssigneeUserId != CurrentUserId)
            {
                NotificationService.ProjectTaskAssigned(task, CurrentUser == null ? null : CurrentUser.FullName);
            }

            // Tao san "Viec can lam" nhap luc tao — mirror dung hanh vi AddTodo (thong bao + ghi
            // log) cho TUNG dong, de lich su thao tac nhat quan du todo duoc them luc tao hay sau.
            var actorName = CurrentUser == null ? null : CurrentUser.FullName;
            for (var i = 0; i < todoLines.Count; i++)
            {
                var todo = Repository.WorkTaskTodos.Insert(new WorkTaskTodo
                {
                    TaskId = task.Id,
                    Content = todoLines[i],
                    SortOrder = i,
                    CreatedByUserId = CurrentUserId,
                    CreatedByName = actorName,
                    CreatedAt = DateTime.Now
                });

                NotificationService.TodoAdded(task, todo, CurrentUserId, actorName);
                TaskActivityLogService.Record(task.Id, CurrentUserId, actorName,
                    TaskActivityActions.TodoAdded,
                    string.Format("Đã thêm việc cần làm \"{0}\"", todoLines[i]));
            }

            return Json(ApiMappers.ToDto(task));
        }

        /// <summary>Thanh vien DANG HOAT DONG cua du an (chua roi) — y het ProjectMembers ben web,
        /// nhung chi tra UserId (khong can gop nhieu dong assignment/nguoi nhu ben do).</summary>
        private static int[] ActiveMemberIds(int projectId)
        {
            return WorkService.AssignmentsOfProject(projectId)
                .Where(a => a.IsActive)
                .Select(a => a.UserId)
                .Distinct()
                .ToArray();
        }

    }
}
