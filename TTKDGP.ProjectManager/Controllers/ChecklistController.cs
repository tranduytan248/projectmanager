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
    /// Checklist công việc của một dự án — nguồn của 80% điểm chất lượng.
    ///
    /// Mọi action đều kiểm HAI lớp: AppAuthorize chặn ở mức chức năng, CanEditProject/CanViewProject
    /// chặn theo đúng dự án. Vì "PM" không phải nhóm quyền nên ai cũng mang quyền checklist ở mức
    /// toàn cục — thiếu lớp thứ hai là PM dự án A sửa được dự án B bằng cách đổi id trên URL.
    /// </summary>
    [AppAuthorize]
    public class ChecklistController : BaseController
    {
        private static readonly string[] DateFormats =
        {
            "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "dd.MM.yyyy"
        };

        /// <summary>
        /// Việc trong một dự án gồm CẢ Triển khai lẫn Hỗ trợ — màn này quản cả hai, người dùng
        /// chọn loại ngay lúc thêm mới. Chỉ việc ngoài dự án là không thuộc về đây.
        /// </summary>
        private static List<WorkTask> ProjectTasks(int projectId)
        {
            return WorkService.Sort(WorkService.AllTasks()
                .Where(t => t.ProjectId == projectId && TaskKinds.InProject.Contains(t.Kind)));
        }

        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Index(int projectId, int page = 1, string view = null,
            int wlYear = 0, int wlMonth = 0,
            string q = null, int assigneeUserId = 0, string state = null, string kind = null,
            string due = null)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null || !CanViewProject(projectId)) return HttpNotFound();

            var tasks = ProjectTasks(projectId);
            var allRows = BuildTree(tasks, WorkService.CommentCounts(tasks.Select(t => t.Id)));

            // Quyền tính một lần cho cả dự án rồi mới xét từng dòng — CanEditProject phải đọc CSDL,
            // gọi lại cho mỗi đầu việc là hàng trăm lượt đọc thừa.
            var canEditAll = CanEditProject(projectId);
            var userId = CurrentUserId;

            foreach (var row in allRows)
            {
                row.CanEditAll = canEditAll;
                row.CanEdit = canEditAll || (userId > 0 && row.Task.AssigneeUserId == userId);
            }

            var model = new ChecklistViewModel
            {
                Project = project,
                CanEdit = canEditAll,
                View = TaskViews.Parse(view),
                AllRows = allRows,
                Users = WorkService.ActiveUsers(),

                Query = q,
                AssigneeUserId = assigneeUserId,
                State = state,
                Kind = kind,
                Due = ChecklistDueFilters.Parse(due)
            };

            // Các con số tổng ở đầu trang vẫn tính trên TOÀN BỘ checklist (AllRows), còn danh sách
            // bên dưới thì theo bộ lọc — lọc mà tổng cũng đổi theo thì không còn mốc để đối chiếu.
            var shownRows = ApplyFilter(allRows, model);
            model.MatchCount = shownRows.Count;

            // Thống kê khối lượng là số liệu quản lý — chỉ dựng khi tài khoản có quyền, để nó
            // không lộ ra ở HTML của người thường dù khối có đang gập lại.
            if (Can(Permissions.Workload.Perm(Permissions.View)))
            {
                var wYear = wlYear > 0 ? wlYear : DateTime.Today.Year;
                var wMonth = wlMonth >= 1 && wlMonth <= 12 ? wlMonth : DateTime.Today.Month;

                model.ShowWorkload = true;
                model.WorkloadYear = wYear;
                model.WorkloadMonth = wMonth;
                model.Workload = WorkloadService.Build(projectId, wYear, wMonth);

                // Khối tự mở sẵn khi người dùng vừa đổi tháng — nếu không, đổi tháng xong trang
                // nạp lại và khối gập lại như cũ, nhìn như bấm không ăn.
                model.WorkloadOpen = wlYear > 0 || wlMonth > 0;
            }

            if (model.View == TaskViews.Kanban)
            {
                // Bảng Kanban chỉ hiện việc LÀM ĐƯỢC: bỏ mục cha gom nhóm (kéo mục cha sang cột
                // khác không có nghĩa) và bỏ việc chưa đặt hạn hoàn thành — thẻ trên bảng là để
                // canh hạn, việc không hạn nằm ở dạng lưới.
                var parentIds = new HashSet<int>(tasks.Where(t => t.ParentId > 0).Select(t => t.ParentId));
                var cardRows = shownRows
                    .Where(r => !parentIds.Contains(r.Task.Id) && r.Task.DueDate.HasValue)
                    .ToList();

                model.Columns = BuildColumns(cardRows);
                ViewBag.KanbanCardCount = cardRows.Count;
                ViewBag.KanbanHiddenCount = shownRows.Count - cardRows.Count;
            }
            else
            {
                model.Rows = PageByRoot(shownRows, page);
            }

            return View(model);
        }

        /// <summary>
        /// Lọc cây checklist theo điều kiện đang chọn.
        ///
        /// Giữ nguyên cấu trúc cây: một mục con khớp thì các mục cha của nó cũng được giữ lại, dù
        /// bản thân chúng không khớp. Thiếu bước này thì mục con hiện ra mà không biết thuộc nhóm
        /// nào, và phần thụt lề trông như lỗi hiển thị. Mục cha giữ theo diện này KHÔNG tính vào
        /// số dòng khớp — nó chỉ ở đó làm ngữ cảnh.
        /// </summary>
        private static List<ChecklistRow> ApplyFilter(List<ChecklistRow> rows, ChecklistViewModel filter)
        {
            if (!filter.HasFilter) return rows;

            var matched = rows.Where(r => Matches(r.Task, filter)).ToList();
            if (matched.Count == 0) return new List<ChecklistRow>();

            // Đi ngược từ dưới lên: gặp dòng cần giữ thì đánh dấu luôn mục cha của nó. Nhờ duyệt
            // ngược, đến lượt mục cha thì nó đã được đánh dấu sẵn.
            var keep = new HashSet<int>(matched.Select(r => r.Task.Id));
            for (var i = rows.Count - 1; i >= 0; i--)
            {
                var task = rows[i].Task;
                if (task.ParentId > 0 && keep.Contains(task.Id)) keep.Add(task.ParentId);
            }

            return rows.Where(r => keep.Contains(r.Task.Id)).ToList();
        }

        /// <summary>Một đầu việc có khớp toàn bộ điều kiện lọc đang bật không.</summary>
        private static bool Matches(WorkTask task, ChecklistViewModel filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var needle = filter.Query.Trim();
                var inTitle = (task.Title ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0;
                var inCode = (task.Code ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0;
                if (!inTitle && !inCode) return false;
            }

            // Số âm nghĩa là lọc riêng nhóm việc chưa giao cho ai.
            if (filter.AssigneeUserId > 0 && task.AssigneeUserId != filter.AssigneeUserId) return false;
            if (filter.AssigneeUserId < 0 && task.AssigneeUserId > 0) return false;

            if (!string.IsNullOrWhiteSpace(filter.State) && task.State != filter.State) return false;
            if (!string.IsNullOrWhiteSpace(filter.Kind) && task.Kind != filter.Kind) return false;

            return MatchesDue(task, filter.Due);
        }

        /// <summary>Khớp bộ lọc nhanh theo hạn hoàn thành.</summary>
        private static bool MatchesDue(WorkTask task, string due)
        {
            if (string.IsNullOrWhiteSpace(due)) return true;

            if (due == ChecklistDueFilters.NoDue) return !task.DueDate.HasValue;
            if (due == ChecklistDueFilters.Overdue) return task.IsOverdue;

            if (due == ChecklistDueFilters.Soon)
            {
                if (TaskStates.IsClosed(task.State) || !task.DueDate.HasValue) return false;

                var limit = DateTime.Today.AddDays(ChecklistDueFilters.SoonDays);
                return !task.IsOverdue && task.DueDate.Value.Date <= limit;
            }

            return true;
        }

        /// <summary>
        /// Xếp việc vào các cột Kanban theo trạng thái. Giữ nguyên thứ tự đã sắp ở lưới, nhưng bỏ
        /// thụt lề cây: một mục cha và mục con của nó thường ở hai trạng thái khác nhau nên không
        /// thể nằm chung cột để mà lồng vào nhau.
        /// </summary>
        private static List<KanbanColumn> BuildColumns(List<ChecklistRow> rows)
        {
            var columns = TaskStates.All
                .Select(s => new KanbanColumn { State = s, Title = TaskStates.Display(s) })
                .ToList();

            var byState = columns.ToDictionary(c => c.State, c => c);

            foreach (var row in rows)
            {
                KanbanColumn column;
                // Trạng thái lạ (dữ liệu cũ) vẫn phải thấy được, dồn vào cột đầu chứ không bỏ rơi.
                if (!byState.TryGetValue(row.Task.State ?? string.Empty, out column)) column = columns[0];
                column.Cards.Add(row);
            }

            return columns;
        }

        /// <summary>
        /// Đổi trạng thái một đầu việc — điểm cuối của thao tác kéo thả trên bảng Kanban.
        /// Trả JSON để trang tự cập nhật, không nạp lại cả màn hình.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult SetState(int id, string state)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null) return Json(new { ok = false, message = "Không tìm thấy công việc." });

            // Kiểm quyền theo ĐÚNG đầu việc: thành viên chỉ kéo được thẻ việc của chính mình.
            if (!CanEditTask(task))
            {
                return Json(new { ok = false, message = "Bạn chỉ đổi được trạng thái công việc của mình." });
            }

            if (!TaskStates.All.Contains(state))
            {
                return Json(new { ok = false, message = "Trạng thái không hợp lệ." });
            }

            WorkService.ApplyState(task, state);
            task.UpdatedAt = DateTime.Now;
            task.UpdatedBy = CurrentUser == null ? null : CurrentUser.FullName;
            Repository.WorkTasks.Update(task);

            return Json(new
            {
                ok = true,
                state = task.State,
                progress = task.Progress,
                onTime = task.IsOnTime,
                overdue = task.IsOverdue,
                dueSoon = task.IsDueSoon
            });
        }

        /// <summary>
        /// Phân trang theo MỤC GỐC chứ không theo dòng: cắt theo dòng sẽ đẩy mục con sang trang
        /// khác, mất luôn ngữ cảnh cha của nó. Mỗi trang lấy 10 mục gốc kèm toàn bộ con cháu.
        /// </summary>
        private static PagedList<ChecklistRow> PageByRoot(List<ChecklistRow> rows, int page)
        {
            var rootIndexes = new List<int>();
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Depth == 0) rootIndexes.Add(i);
            }

            var pager = new PagerInfo
            {
                PageSize = WorkService.PageSize,
                TotalItems = rootIndexes.Count
            };
            pager.Page = PagedList<ChecklistRow>.Clamp(page, pager.TotalPages);

            var skip = (pager.Page - 1) * pager.PageSize;
            var take = pager.PageSize;

            var from = skip < rootIndexes.Count ? rootIndexes[skip] : rows.Count;
            var to = skip + take < rootIndexes.Count ? rootIndexes[skip + take] : rows.Count;

            return new PagedList<ChecklistRow>
            {
                Pager = pager,
                Items = rows.Skip(from).Take(to - from).ToList()
            };
        }

        /// <summary>
        /// Thông tin chi tiết một đầu việc, hiện trong hộp thoại khi bấm vào dòng hoặc thẻ Kanban.
        /// Ai xem được dự án thì xem được; nút Cập nhật hiện hay không tuỳ quyền trên ĐÚNG việc đó.
        /// </summary>
        [HttpGet]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Detail(int id)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            ViewBag.CanEdit = CanEditTask(task);
            ViewBag.CanEditAll = CanEditAllOfTask(task);
            ViewBag.Project = Repository.WorkProjects.Find(task.ProjectId);
            ViewBag.CommentsModel = BuildComments(task);

            var parent = task.ParentId > 0 ? Repository.WorkTasks.Find(task.ParentId) : null;
            ViewBag.ParentTitle = parent == null
                ? null
                : (string.IsNullOrWhiteSpace(parent.Code) ? parent.Title : parent.Code + " · " + parent.Title);

            return PartialView("_Detail", task);
        }

        // ---------- Trao đổi ngay trong hộp thoại chi tiết ----------

        /// <summary>
        /// Dựng khung trao đổi: toàn bộ lượt trao đổi của đầu việc và danh sách người nhắc được
        /// bằng @ — người đang tham gia dự án (kể cả PM) cộng người đang được giao chính việc này.
        /// </summary>
        private TaskCommentsViewModel BuildComments(WorkTask task)
        {
            var participants = WorkService.TaskParticipants(task)
                .Select(u => new TaskMentionOption
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    UserName = u.UserName
                })
                .ToList();

            return new TaskCommentsViewModel
            {
                TaskId = task.Id,
                CurrentUserId = CurrentUserId,
                CanModerate = CanEditProject(task.ProjectId),
                Comments = Repository.WorkComments.All()
                    .Where(c => c.TaskId == task.Id)
                    .OrderBy(c => c.CreatedAt)
                    .ToList(),
                Participants = participants
            };
        }

        /// <summary>
        /// Gửi một lượt trao đổi, kèm được một file đính kèm. Trả lại partial danh sách để hộp
        /// thoại tự vẽ lại; file bị từ chối thì trả 400 kèm lý do để chỗ gửi hiện thông báo.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Comment(int id, string content, HttpPostedFileBase file)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            var hasText = !string.IsNullOrWhiteSpace(content);
            var hasFile = file != null && file.ContentLength > 0;

            if (hasText || hasFile)
            {
                var comment = new WorkComment
                {
                    TaskId = id,
                    UserId = CurrentUserId,
                    AuthorName = CurrentUser == null ? "(không rõ)" : CurrentUser.FullName,
                    Content = hasText ? content.Trim() : null,
                    CreatedAt = DateTime.Now
                };

                string error;
                if (!CommentAttachments.TrySave(file, comment, out error))
                {
                    Response.StatusCode = 400;
                    Response.TrySkipIisCustomErrors = true;
                    return Content(error);
                }

                Repository.WorkComments.Insert(comment);

                // Báo cho những người bị nhắc tên bằng @ trong nội dung (web + email).
                NotificationService.Mentions(task, comment, CurrentUserId,
                    CurrentUser == null ? null : CurrentUser.FullName);
            }

            return PartialView("_Comments", BuildComments(task));
        }

        /// <summary>
        /// Tải file đính kèm của một lượt trao đổi. Kiểm quyền theo ĐÚNG đầu việc chứa nó — ai
        /// xem được việc thì tải được file; nội dung đã thu hồi thì file cũng khoá theo.
        /// </summary>
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Attachment(int commentId)
        {
            var comment = Repository.WorkComments.Find(commentId);
            if (comment == null || comment.IsDeleted || !comment.HasAttachment) return HttpNotFound();

            var task = Repository.WorkTasks.Find(comment.TaskId);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            var path = CommentAttachments.FullPath(comment.AttachmentFile);
            if (path == null) return HttpNotFound();

            // Luôn trả kiểu tải-về chung chung: trình duyệt tải file chứ không thực thi/nhúng.
            return File(path, "application/octet-stream",
                string.IsNullOrWhiteSpace(comment.AttachmentName) ? "tep-dinh-kem" : comment.AttachmentName);
        }

        /// <summary>Thu hồi nội dung. Không xoá cứng để mạch hội thoại không đứt quãng.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult RecallComment(int id, int commentId)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            var comment = Repository.WorkComments.Find(commentId);
            if (comment == null || comment.TaskId != id) return HttpNotFound();

            // Chỉ chính chủ, PM dự án hoặc Quản lý Tổ mới thu hồi được.
            if (comment.UserId != CurrentUserId && !CanEditProject(task.ProjectId)) return HttpNotFound();

            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.Now;
            Repository.WorkComments.Update(comment);

            return PartialView("_Comments", BuildComments(task));
        }

        // ---------- Thêm / sửa một mục ----------

        [HttpGet]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Edit(int projectId, int? id)
        {
            var project = Repository.WorkProjects.Find(projectId);
            if (project == null) return HttpNotFound();

            WorkTask task;
            if (id.HasValue)
            {
                task = Repository.WorkTasks.Find(id.Value);
                if (task == null || task.ProjectId != projectId
                    || !TaskKinds.InProject.Contains(task.Kind))
                {
                    return HttpNotFound();
                }

                // Thành viên mở được việc của chính mình; việc người khác thì không.
                if (!CanEditTask(task)) return HttpNotFound();
            }
            else
            {
                // Thêm việc mới là quyền của PM và Quản lý Tổ.
                if (!CanEditProject(projectId)) return HttpNotFound();

                task = new WorkTask
                {
                    Kind = TaskKinds.Checklist,
                    ProjectId = projectId,
                    State = TaskStates.NotStarted,
                    StartDate = DateTime.Today,
                    DueDate = DateTime.Today.AddDays(7)
                };
            }

            PopulateEditLists(task);
            ViewBag.Project = project;
            ViewBag.CanEditAll = CanEditAllOfTask(task) || task.Id == 0;

            // Mở từ hộp thoại thì chỉ trả phần form; mở thẳng bằng đường dẫn thì trả cả trang.
            return Request.IsAjaxRequest() ? (ActionResult)PartialView("_EditForm", task) : View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Edit(WorkTask model)
        {
            var project = Repository.WorkProjects.Find(model.ProjectId);
            if (project == null) return HttpNotFound();

            WorkTask current = null;
            if (model.Id != 0)
            {
                current = Repository.WorkTasks.Find(model.Id);
                if (current == null || current.ProjectId != model.ProjectId) return HttpNotFound();
                if (!CanEditTask(current)) return HttpNotFound();
            }
            else if (!CanEditProject(model.ProjectId))
            {
                return HttpNotFound();
            }

            var canEditAll = current == null || CanEditAllOfTask(current);
            ViewBag.CanEditAll = canEditAll;

            if (canEditAll)
            {
                // Chỉ nhận hai loại việc trong dự án; gõ tay giá trị khác thì coi như Triển khai.
                if (!TaskKinds.InProject.Contains(model.Kind)) model.Kind = TaskKinds.Checklist;
                model.Priority = TaskPriorities.Parse(model.Priority);

                // Tuần và năm đi liền nhau: có tuần mà quên năm thì lấy năm hiện tại, bỏ tuần thì xoá năm.
                if (model.Week > 0 && model.Year <= 0) model.Year = WeekHelper.CurrentYear;
                if (model.Week <= 0) model.Year = 0;
            }
            else
            {
                // Người thực hiện chỉ được báo cáo tiến độ. Mọi trường ngoài phạm vi đó lấy lại từ
                // bản ghi đang có — KHÔNG tin dữ liệu gửi lên, vì form gửi đi sửa tay được.
                model.Kind = current.Kind;
                model.Code = current.Code;
                model.Title = current.Title;
                model.ParentId = current.ParentId;
                model.AssigneeUserId = current.AssigneeUserId;
                model.StartDate = current.StartDate;
                model.DueDate = current.DueDate;
                model.Priority = current.Priority;
                model.Week = current.Week;
                model.Year = current.Year;

                // Form rút gọn không gửi các trường trên nên chúng vào đây rỗng và đã bị đánh lỗi.
                // Lấy lại giá trị cũ xong thì phải kiểm lại từ đầu, nếu không lưu sẽ luôn báo
                // "vui lòng nhập tên công việc".
                ModelState.Clear();
                TryValidateModel(model);
            }

            if (!model.DueDate.HasValue)
            {
                ModelState.AddModelError("DueDate", "Vui lòng nhập hạn hoàn thành.");
            }

            if (model.DueDate.HasValue && model.StartDate.HasValue
                && model.DueDate.Value.Date < model.StartDate.Value.Date)
            {
                ModelState.AddModelError("DueDate", "Hạn hoàn thành phải sau ngày bắt đầu.");
            }

            ValidateParent(model);

            // Người thực hiện phải đang tham gia dự án. Người đã lưu từ trước vẫn giữ được
            // (kể cả đã rời dự án), nhưng ĐỔI sang người ngoài dự án là chặn — form sửa tay được.
            if (canEditAll && model.AssigneeUserId > 0
                && model.AssigneeUserId != (current == null ? 0 : current.AssigneeUserId)
                && ProjectMembers(model.ProjectId).All(m => m.User.Id != model.AssigneeUserId))
            {
                ModelState.AddModelError("AssigneeUserId", "Người thực hiện phải đang tham gia dự án này.");
            }

            if (!ModelState.IsValid)
            {
                PopulateEditLists(model);
                ViewBag.Project = project;

                // Trả lại form kèm lỗi để hộp thoại hiện tại chỗ, không mất dữ liệu đã nhập.
                return Request.IsAjaxRequest()
                    ? (ActionResult)PartialView("_EditForm", model)
                    : View(model);
            }

            WorkService.FillNames(model);
            var now = DateTime.Now;
            var actor = CurrentUser == null ? null : CurrentUser.FullName;

            if (model.Id == 0)
            {
                model.CreatedAt = now;
                model.CreatedBy = actor;
                model.AssignedByUserId = CurrentUserId;
                model.SortOrder = NextSortOrder(model.ProjectId, model.ParentId);
                Repository.WorkTasks.Insert(model);

                // Báo web + email cho người được giao (trừ khi tự giao cho chính mình).
                if (model.AssigneeUserId > 0 && model.AssigneeUserId != CurrentUserId)
                {
                    NotificationService.TaskAssigned(model, actor);
                }

                Notify(string.Format("Đã thêm mục \"{0}\".", model.Title));
            }
            else
            {
                // Giữ dấu vết tạo và mốc hoàn thành — người sửa không được ghi đè lịch sử.
                model.CreatedAt = current.CreatedAt;
                model.CreatedBy = current.CreatedBy;
                model.AssignedByUserId = current.AssignedByUserId;
                model.CompletedAt = current.CompletedAt;
                model.SortOrder = current.SortOrder;
                model.UpdatedAt = now;
                model.UpdatedBy = actor;

                WorkService.ApplyState(model, model.State, model.Progress);
                Repository.WorkTasks.Update(model);

                // Chuyển việc sang người khác thì báo cho người MỚI như một lần giao việc.
                if (model.AssigneeUserId > 0
                    && model.AssigneeUserId != current.AssigneeUserId
                    && model.AssigneeUserId != CurrentUserId)
                {
                    NotificationService.TaskAssigned(model, actor);
                }

                Notify(string.Format("Đã cập nhật mục \"{0}\".", model.Title));
            }

            return Saved("Index", new { projectId = model.ProjectId });
        }

        private void ValidateParent(WorkTask model)
        {
            if (model.ParentId <= 0) return;

            if (model.ParentId == model.Id)
            {
                ModelState.AddModelError("ParentId", "Mục không thể là cha của chính nó.");
                return;
            }

            var parent = Repository.WorkTasks.Find(model.ParentId);
            if (parent == null || parent.ProjectId != model.ProjectId)
            {
                ModelState.AddModelError("ParentId", "Mục cha không hợp lệ.");
                return;
            }

            // Chọn con của chính mình làm cha sẽ tạo chu trình, làm treo vòng dựng cây.
            if (model.Id > 0 && IsDescendantOf(parent, model.Id))
            {
                ModelState.AddModelError("ParentId", "Không thể chọn mục con của chính nó làm mục cha.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Delete(int projectId, int id)
        {
            if (!CanEditProject(projectId)) return HttpNotFound();

            var task = Repository.WorkTasks.Find(id);
            if (task == null || task.ProjectId != projectId) return HttpNotFound();

            // Mục đã vào phiếu chấm KPI thì không xoá — xoá đi là phiếu đã duyệt mất căn cứ.
            if (Repository.KpiLines.All().Any(l => l.TaskId == id))
            {
                NotifyError(string.Format(
                    "Không xoá được \"{0}\" vì đã nằm trong bảng chấm KPI. Hãy chuyển trạng thái sang Huỷ.",
                    task.Title));
                return RedirectToAction("Index", new { projectId = projectId });
            }

            // Xoá kèm mục con, nếu không chúng thành mồ côi và biến mất khỏi cây.
            var all = ProjectTasks(projectId);
            var descendants = DescendantIds(id, all);

            // Xoá file đính kèm trên đĩa TRƯỚC khi xoá bản ghi — xoá bản ghi rồi thì không còn
            // gì trỏ tới file, chúng thành rác nằm lại App_Data mãi mãi.
            var deletingIds = new HashSet<int>(descendants) { id };
            foreach (var c in Repository.WorkComments.All()
                         .Where(c => deletingIds.Contains(c.TaskId) && c.HasAttachment))
            {
                CommentAttachments.Delete(c.AttachmentFile);
            }

            foreach (var childId in descendants)
            {
                Repository.WorkComments.DeleteWhere(c => c.TaskId == childId);
                Repository.WorkTasks.Delete(childId);
            }
            Repository.WorkComments.DeleteWhere(c => c.TaskId == id);
            Repository.WorkTasks.Delete(id);

            Notify(descendants.Count > 0
                ? string.Format("Đã xoá mục \"{0}\" và {1} mục con.", task.Title, descendants.Count)
                : string.Format("Đã xoá mục \"{0}\".", task.Title));

            return RedirectToAction("Index", new { projectId = projectId });
        }

        // ---------- Tải mẫu ----------

        /// <summary>
        /// File mẫu. Tải từ trong một dự án thì sheet nhân sự đổ sẵn ĐÚNG người đang tham gia dự án
        /// đó — người dùng chỉ việc chép tài khoản sang cột người thực hiện, khỏi tự tra.
        /// Không có dự án thì mới rơi về ví dụ minh hoạ.
        /// </summary>
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Template(int? projectId)
        {
            WorkProject project = null;
            if (projectId.HasValue && CanViewProject(projectId.Value))
            {
                project = Repository.WorkProjects.Find(projectId.Value);
            }

            // Sheet 1 — công việc. Người thực hiện tham chiếu bằng TÀI KHOẢN đăng nhập ở sheet 2,
            // không dùng họ tên: tên trùng nhau hoặc gõ sai dấu là ghép nhầm người.
            var tasks = new SheetData(SheetTasks,
                "MaCongViec", "TenCongViec", "MoTa", "MaCongViecCha", "TaiKhoanThucHien",
                "NgayBatDau", "HanHoanThanh", "NgayHoanThanh", "DoUuTien", "Tuan", "Nam");

            var members = new SheetData(SheetMembers, "TaiKhoan", "TenNhanVien", "VaiTro");

            var team = project == null ? new List<ProjectMember>() : ProjectMembers(project.Id);
            foreach (var item in team)
            {
                members.Add(item.User.UserName, item.User.FullName, item.Role);
            }

            // Tài khoản trong dòng ví dụ phải là tài khoản CÓ THẬT trong dự án, nếu không người
            // dùng giữ nguyên dòng mẫu là import xong lại báo không tìm thấy người.
            var account1 = team.Count > 0 ? team[0].User.UserName : string.Empty;
            var account2 = team.Count > 1 ? team[1].User.UserName : account1;

            tasks.Add("CV-00", "Giai đoạn 1 — Khảo sát", "Mục cha để gom nhóm", "", "",
                "", "15/08/2026", "", "Cao", "", "");
            tasks.Add("CV-01", "Dựng màn hình đăng nhập", "Gồm cả quên mật khẩu", "CV-00", account1,
                "01/08/2026", "15/08/2026", "", "Cao", "33", "2026");
            tasks.Add("CV-02", "Kiểm thử chức năng đăng nhập", "", "CV-00", account2,
                "16/08/2026", "20/08/2026", "20/08/2026", "Trung bình", "34", "2026");

            if (members.Rows.Count == 0)
            {
                members.Add("nhanvien.a", "Ví dụ — thay bằng tài khoản thật", "Thành viên");
            }

            var name = project == null
                ? "Mau-import-checklist"
                : "Mau-import-checklist-" + FileNamePart(project.Code, project.Name);

            return File(SpreadsheetFile.Build(tasks, members),
                SpreadsheetFile.ContentType, name + SpreadsheetFile.Extension);
        }

        private class ProjectMember
        {
            public User User { get; set; }
            public string Role { get; set; }
        }

        /// <summary>
        /// Người đang tham gia dự án, PM lên đầu. Lấy từ phân công CHƯA rời đi — người đã rời dự
        /// án không nên xuất hiện trong file mẫu để giao việc mới.
        /// </summary>
        private static List<ProjectMember> ProjectMembers(int projectId)
        {
            var result = new List<ProjectMember>();

            var open = WorkService.AssignmentsOfProject(projectId)
                .Where(a => !a.LeftAt.HasValue)
                .OrderByDescending(a => a.IsPm)
                .ThenBy(a => a.UserFullName)
                .ToList();

            var seen = new HashSet<int>();

            foreach (var assignment in open)
            {
                if (!seen.Add(assignment.UserId)) continue;

                var person = Repository.Users.Find(assignment.UserId);
                if (person == null) continue;

                result.Add(new ProjectMember
                {
                    User = person,
                    Role = assignment.IsPm
                        ? "PM"
                        : (string.IsNullOrWhiteSpace(assignment.Role) ? "Thành viên" : assignment.Role)
                });
            }

            return result;
        }

        /// <summary>Phần tên file lấy từ dự án: bỏ dấu, chỉ giữ chữ số và gạch ngang.</summary>
        private static string FileNamePart(string code, string name)
        {
            var source = string.IsNullOrWhiteSpace(code) ? name : code;
            var plain = WorkService.RemoveDiacritics(source ?? string.Empty);
            var cleaned = new string(plain.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
                .Trim('-');

            if (cleaned.Length == 0) return "du-an";
            return cleaned.Length > 40 ? cleaned.Substring(0, 40) : cleaned;
        }

        /// <summary>Tên hai trang tính trong file mẫu. Đọc theo tên trước, không thấy thì theo thứ tự.</summary>
        private const string SheetTasks = "CongViec";
        private const string SheetMembers = "NhanSu";

        // ---------- Import ----------

        /// <summary>Form chọn file import, mở trong hộp thoại từ màn checklist.</summary>
        [HttpGet]
        [AppAuthorize(Permission = "wtasks.import")]
        public ActionResult ImportForm(int projectId)
        {
            if (!CanEditProject(projectId)) return HttpNotFound();

            var project = Repository.WorkProjects.Find(projectId);
            if (project == null) return HttpNotFound();

            return PartialView("_ImportForm", project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.import")]
        public ActionResult Import(int projectId, HttpPostedFileBase file)
        {
            if (!CanEditProject(projectId)) return HttpNotFound();

            var project = Repository.WorkProjects.Find(projectId);
            if (project == null) return HttpNotFound();

            var model = new ImportPreviewViewModel { Project = project };

            if (file == null || file.ContentLength == 0)
            {
                model.FileError = "Chưa chọn file.";
                return View("ImportPreview", model);
            }

            // Chặn file quá lớn TRƯỚC khi đọc, tránh nạp cả trăm MB lên bộ nhớ.
            if (file.ContentLength > 5 * 1024 * 1024)
            {
                model.FileError = "File vượt quá 5 MB.";
                return View("ImportPreview", model);
            }

            model.FileName = file.FileName;

            string error;
            var sheets = SpreadsheetFile.ReadAll(file.FileName, file.InputStream, out error);
            if (sheets == null)
            {
                model.FileError = error;
                return View("ImportPreview", model);
            }

            var taskSheet = FindSheet(sheets, SheetTasks, 0);
            var memberSheet = FindSheet(sheets, SheetMembers, 1);

            if (taskSheet == null || taskSheet.Rows.Count == 0)
            {
                model.FileError = "Không tìm thấy dữ liệu công việc. Sheet đầu tiên phải là danh sách công việc.";
                return View("ImportPreview", model);
            }

            // Thiếu cột bắt buộc thì dừng ngay, đỡ báo lỗi lặp trên từng dòng.
            var first = taskSheet.Rows[0];
            if (!first.ContainsKey("tencongviec"))
            {
                model.FileError = "Sheet công việc thiếu cột bắt buộc \"TenCongViec\". "
                                + "Hãy tải file mẫu và điền vào đó.";
                return View("ImportPreview", model);
            }

            // Nhân sự phải xử lý TRƯỚC công việc: sheet công việc trỏ người thực hiện bằng tài
            // khoản đăng nhập, nên phải biết tài khoản nào hợp lệ rồi mới ghép được.
            if (memberSheet == null)
            {
                model.FileWarning = "File chỉ có một sheet nên bỏ qua phần nhân sự. "
                                  + "Việc nào ghi tài khoản mà người đó chưa tham gia dự án sẽ để trống người thực hiện.";
            }
            else
            {
                model.MemberRows = AnalyzeMembers(projectId, memberSheet.Rows);
            }

            model.Rows = AnalyzeTasks(projectId, taskSheet.Rows, model.MemberRows);

            TempData["ImportRows"] = model.Rows.Where(r => r.Outcome == ImportRow.Add).ToList();
            TempData["ImportMembers"] = model.MemberRows.Where(r => r.Outcome == ImportOutcomes.Add).ToList();
            TempData["ImportProjectId"] = projectId;

            return View("ImportPreview", model);
        }

        /// <summary>
        /// Tìm trang tính theo tên; không khớp tên thì lấy theo vị trí. Người dùng hay đổi tên
        /// sheet khi lưu lại file, nên không thể chỉ dựa vào tên.
        /// </summary>
        private static SpreadsheetFile.SheetRecords FindSheet(
            List<SpreadsheetFile.SheetRecords> sheets, string name, int fallbackIndex)
        {
            var byName = sheets.FirstOrDefault(s =>
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            if (byName != null) return byName;

            return fallbackIndex < sheets.Count ? sheets[fallbackIndex] : null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.import")]
        public ActionResult ImportConfirm(int projectId)
        {
            if (!CanEditProject(projectId)) return HttpNotFound();

            var rows = TempData["ImportRows"] as List<ImportRow>;
            var memberRows = TempData["ImportMembers"] as List<ImportMemberRow>;
            var savedId = TempData["ImportProjectId"] as int?;

            if (rows == null || !savedId.HasValue || savedId.Value != projectId)
            {
                NotifyError("Phiên import đã hết hạn. Hãy chọn lại file.");
                return RedirectToAction("Index", new { projectId = projectId });
            }

            var addedMembers = AddMembersFromImport(projectId, memberRows);
            var now = DateTime.Now;
            var actor = CurrentUser == null ? null : CurrentUser.FullName;
            var sortOrder = NextSortOrder(projectId, 0);

            // Ghi HAI lượt: lượt đầu tạo bản ghi để có Id, lượt sau mới nối mục cha theo mã. Làm một
            // lượt thì mục cha nằm sau mục con trong file sẽ chưa có Id để trỏ tới.
            var byCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var task = row.Prepared;
                if (task == null) continue;

                task.CreatedAt = now;
                task.CreatedBy = actor;
                task.AssignedByUserId = CurrentUserId;
                task.SortOrder = sortOrder++;
                WorkService.FillNames(task);

                var saved = Repository.WorkTasks.Insert(task);
                if (!string.IsNullOrWhiteSpace(saved.Code)) byCode[saved.Code.Trim()] = saved.Id;
            }

            var linked = LinkParents(projectId, rows, byCode);

            var parts = new List<string> { string.Format("Đã import {0} công việc", rows.Count) };
            if (linked > 0) parts.Add(string.Format("nối {0} mục vào mục cha", linked));
            if (addedMembers > 0) parts.Add(string.Format("thêm {0} nhân sự vào dự án", addedMembers));

            Notify(string.Join(", ", parts) + ".");
            return RedirectToAction("Index", new { projectId = projectId });
        }

        /// <summary>
        /// Thêm nhân sự của sheet 2 vào dự án. Chỉ thêm người chưa có giai đoạn nào đang mở —
        /// import lại lần nữa không được tạo phân công trùng.
        /// </summary>
        private int AddMembersFromImport(int projectId, List<ImportMemberRow> memberRows)
        {
            if (memberRows == null || memberRows.Count == 0) return 0;

            var project = Repository.WorkProjects.Find(projectId);
            if (project == null) return 0;

            var now = DateTime.Now;
            var added = 0;

            foreach (var row in memberRows)
            {
                if (row.UserId <= 0) continue;
                if (WorkService.OpenAssignment(projectId, row.UserId) != null) continue;

                var person = Repository.Users.Find(row.UserId);
                if (person == null) continue;

                Repository.WorkAssignments.Insert(new WorkAssignment
                {
                    ProjectId = projectId,
                    ProjectName = project.Name,
                    UserId = person.Id,
                    UserFullName = person.FullName,
                    Role = string.IsNullOrWhiteSpace(row.Role) ? null : row.Role.Trim(),
                    Phase = AssignmentPhases.Both,
                    JoinedAt = DateTime.Today,
                    CreatedAt = now
                });
                added++;

                if (person.Id != CurrentUserId) NotificationService.ProjectAdded(person.Id, project);
            }

            if (added > 0) WorkService.SyncCurrentPm(projectId);
            return added;
        }

        private static int LinkParents(int projectId, List<ImportRow> rows, Dictionary<string, int> byCode)
        {
            var linked = 0;

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.ParentCode) || string.IsNullOrWhiteSpace(row.Code)) continue;

                int childId;
                if (!byCode.TryGetValue(row.Code.Trim(), out childId)) continue;

                int parentId;
                if (!byCode.TryGetValue(row.ParentCode.Trim(), out parentId))
                {
                    // Mục cha có thể đã có sẵn trong dự án từ lần import trước.
                    var existing = Repository.WorkTasks.FirstOrDefault(t =>
                        t.ProjectId == projectId && TaskKinds.InProject.Contains(t.Kind)
                        && string.Equals(t.Code, row.ParentCode.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (existing == null) continue;
                    parentId = existing.Id;
                }

                var child = Repository.WorkTasks.Find(childId);
                if (child == null || child.ParentId == parentId) continue;

                child.ParentId = parentId;
                Repository.WorkTasks.Update(child);
                linked++;
            }

            return linked;
        }

        /// <summary>
        /// Đọc sheet nhân sự (sheet 2). Ghép theo tài khoản đăng nhập; không có thì thử theo họ
        /// tên. Người không ghép được là lỗi của DÒNG đó, không chặn cả file.
        /// </summary>
        private List<ImportMemberRow> AnalyzeMembers(int projectId, List<Dictionary<string, string>> records)
        {
            var users = WorkService.ActiveUsers();
            var already = WorkService.AssignmentsOfProject(projectId)
                .Where(a => !a.LeftAt.HasValue)
                .Select(a => a.UserId)
                .ToList();

            var seen = new HashSet<int>();
            var rows = new List<ImportMemberRow>();
            var line = 1;

            foreach (var record in records)
            {
                line++;
                var row = new ImportMemberRow
                {
                    LineNumber = line,
                    // Nhận cả tên cột cũ "MaNhanVien" để file mẫu đời trước vẫn đọc được.
                    Account = FirstOf(record, "taikhoan", "manhanvien"),
                    FullName = Get(record, "tennhanvien"),
                    Role = Get(record, "vaitro")
                };

                if (string.IsNullOrWhiteSpace(row.Account) && string.IsNullOrWhiteSpace(row.FullName))
                {
                    continue; // dòng trống ở cuối sheet
                }

                var found = string.IsNullOrWhiteSpace(row.Account)
                    ? null
                    : users.FirstOrDefault(u => string.Equals(u.UserName.Trim(), row.Account,
                        StringComparison.OrdinalIgnoreCase));

                if (found == null && !string.IsNullOrWhiteSpace(row.FullName))
                {
                    found = users.FirstOrDefault(u => string.Equals(u.FullName.Trim(), row.FullName,
                        StringComparison.CurrentCultureIgnoreCase));
                }

                if (found == null)
                {
                    row.Outcome = ImportOutcomes.Error;
                    row.Message = "Không tìm thấy người dùng nào có tài khoản hoặc họ tên này.";
                    rows.Add(row);
                    continue;
                }

                row.UserId = found.Id;
                if (string.IsNullOrWhiteSpace(row.FullName)) row.FullName = found.FullName;

                if (already.Contains(found.Id) || !seen.Add(found.Id))
                {
                    row.Outcome = ImportOutcomes.Skip;
                    row.AlreadyInProject = true;
                    row.Message = "Đã tham gia dự án — giữ nguyên phân công hiện có.";
                }
                else
                {
                    row.Outcome = ImportOutcomes.Add;
                }

                rows.Add(row);
            }

            return rows;
        }

        /// <summary>
        /// Đọc sheet công việc (sheet 1): dòng nào thêm, dòng nào bỏ qua, dòng nào lỗi.
        /// Người thực hiện ghép bằng tài khoản đăng nhập — ưu tiên sheet nhân sự, sau đó tới
        /// toàn bộ người dùng đang hoạt động.
        /// </summary>
        private List<ImportRow> AnalyzeTasks(int projectId, List<Dictionary<string, string>> records,
            List<ImportMemberRow> memberRows)
        {
            var seenCodes = new HashSet<string>(
                Repository.WorkTasks.All()
                    .Where(t => t.ProjectId == projectId && TaskKinds.InProject.Contains(t.Kind)
                                && !string.IsNullOrWhiteSpace(t.Code))
                    .Select(t => t.Code.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var users = WorkService.ActiveUsers();

            // Tài khoản -> Id, gộp từ sheet nhân sự trước rồi mới tới toàn bộ người dùng.
            var byAccount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var u in users)
            {
                if (!string.IsNullOrWhiteSpace(u.UserName)) byAccount[u.UserName.Trim()] = u.Id;
            }
            if (memberRows != null)
            {
                foreach (var m in memberRows)
                {
                    if (m.UserId > 0 && !string.IsNullOrWhiteSpace(m.Account))
                        byAccount[m.Account.Trim()] = m.UserId;
                }
            }

            var rows = new List<ImportRow>();
            var line = 1;

            foreach (var record in records)
            {
                line++;
                var row = new ImportRow
                {
                    LineNumber = line,
                    Code = Get(record, "macongviec"),
                    Title = Get(record, "tencongviec"),
                    Note = Get(record, "mota"),
                    ParentCode = Get(record, "macongvieccha"),
                    // Nhận cả tên cột cũ "MaNhanVienThucHien" để file mẫu đời trước vẫn đọc được.
                    AssigneeAccount = FirstOf(record, "taikhoanthuchien", "manhanvienthuchien"),
                    StartDateText = Get(record, "ngaybatdau"),
                    DueDateText = Get(record, "hanhoanthanh"),
                    DoneDateText = Get(record, "ngayhoanthanh"),
                    PriorityText = Get(record, "douutien"),
                    WeekText = Get(record, "tuan"),
                    YearText = Get(record, "nam")
                };

                if (string.IsNullOrWhiteSpace(row.Title))
                {
                    // Dòng trống hoàn toàn thì bỏ qua im lặng, đừng bắt người dùng xoá tay.
                    if (string.IsNullOrWhiteSpace(row.Code)) continue;

                    Fail(rows, row, "Thiếu tên công việc.");
                    continue;
                }

                DateTime due;
                if (!TryParseDate(row.DueDateText, out due))
                {
                    Fail(rows, row, string.IsNullOrWhiteSpace(row.DueDateText)
                        ? "Thiếu hạn hoàn thành."
                        : "Hạn hoàn thành sai định dạng (cần dd/MM/yyyy).");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(row.Code) && !seenCodes.Add(row.Code.Trim()))
                {
                    row.Outcome = ImportRow.Skip;
                    row.Message = "Mã đã có trong dự án hoặc trùng với dòng phía trên.";
                    rows.Add(row);
                    continue;
                }

                DateTime start;
                var hasStart = TryParseDate(row.StartDateText, out start);
                if (!hasStart && !string.IsNullOrWhiteSpace(row.StartDateText))
                {
                    Fail(rows, row, "Ngày bắt đầu sai định dạng (cần dd/MM/yyyy).");
                    continue;
                }

                if (hasStart && due.Date < start.Date)
                {
                    Fail(rows, row, "Hạn hoàn thành sớm hơn ngày bắt đầu.");
                    continue;
                }

                DateTime done;
                var hasDone = TryParseDate(row.DoneDateText, out done);
                if (!hasDone && !string.IsNullOrWhiteSpace(row.DoneDateText))
                {
                    Fail(rows, row, "Ngày hoàn thành sai định dạng (cần dd/MM/yyyy).");
                    continue;
                }

                // Tài khoản gõ sai KHÔNG làm hỏng cả file — chỉ để trống người thực hiện và cảnh báo.
                var assigneeId = 0;
                if (!string.IsNullOrWhiteSpace(row.AssigneeAccount))
                    byAccount.TryGetValue(row.AssigneeAccount.Trim(), out assigneeId);

                if (assigneeId > 0)
                {
                    var person = users.FirstOrDefault(u => u.Id == assigneeId);
                    if (person != null) row.AssigneeName = person.FullName;
                }

                row.Outcome = ImportRow.Add;
                if (!string.IsNullOrWhiteSpace(row.AssigneeAccount) && assigneeId == 0)
                {
                    row.Message = "Không tìm thấy tài khoản \"" + row.AssigneeAccount + "\" — để trống người thực hiện.";
                }

                int week, year;
                int.TryParse(row.WeekText, out week);
                int.TryParse(row.YearText, out year);
                if (week < 1 || week > 53) week = 0;
                if (year < 2000 || year > 2100) year = 0;

                // Có tuần mà không ghi năm thì hiểu là năm hiện tại, đỡ phải điền lặp cả cột.
                if (week > 0 && year == 0) year = WeekHelper.CurrentYear;

                row.Prepared = new WorkTask
                {
                    Kind = TaskKinds.Checklist,
                    ProjectId = projectId,
                    Code = string.IsNullOrWhiteSpace(row.Code) ? null : row.Code.Trim(),
                    Title = row.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(row.Note) ? null : row.Note.Trim(),
                    AssigneeUserId = assigneeId,
                    StartDate = hasStart ? start : (DateTime?)null,
                    DueDate = due,
                    CompletedAt = hasDone ? done : (DateTime?)null,
                    Priority = TaskPriorities.Parse(row.PriorityText),
                    Week = week,
                    Year = year,
                    State = hasDone ? TaskStates.Done : TaskStates.NotStarted,
                    Progress = hasDone ? 100 : 0
                };

                rows.Add(row);
            }

            return rows;
        }

        private static void Fail(List<ImportRow> rows, ImportRow row, string message)
        {
            row.Outcome = ImportRow.Error;
            row.Message = message;
            rows.Add(row);
        }

        // ---------- Hàm phụ ----------

        private static string Get(Dictionary<string, string> record, string key)
        {
            string value;
            return record.TryGetValue(key, out value) ? (value ?? string.Empty).Trim() : string.Empty;
        }

        /// <summary>Giá trị của cột đầu tiên có dữ liệu trong danh sách tên cột đưa vào.</summary>
        private static string FirstOf(Dictionary<string, string> record, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = Get(record, key);
                if (value.Length > 0) return value;
            }
            return string.Empty;
        }

        private static bool TryParseDate(string text, out DateTime value)
        {
            value = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(text)) return false;

            return DateTime.TryParseExact(text.Trim(), DateFormats,
                       CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
                   || DateTime.TryParse(text.Trim(), CultureInfo.GetCultureInfo("vi-VN"),
                       DateTimeStyles.None, out value);
        }

        /// <summary>Dàn cây thành danh sách phẳng theo đúng thứ tự hiển thị, kèm độ sâu.</summary>
        private static List<ChecklistRow> BuildTree(List<WorkTask> tasks, Dictionary<int, int> commentCounts)
        {
            var rows = new List<ChecklistRow>();
            var byParent = tasks.GroupBy(t => t.ParentId)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.SortOrder).ToList());

            // Mục có cha đã bị xoá thì coi như mục gốc, để không biến mất khỏi màn hình.
            var ids = new HashSet<int>(tasks.Select(t => t.Id));
            var roots = tasks.Where(t => t.ParentId == 0 || !ids.Contains(t.ParentId))
                .OrderBy(t => t.SortOrder).ToList();

            var visited = new HashSet<int>();
            foreach (var root in roots) AddBranch(rows, root, 0, byParent, commentCounts, visited);

            return rows;
        }

        private static void AddBranch(List<ChecklistRow> rows, WorkTask task, int depth,
            Dictionary<int, List<WorkTask>> byParent, Dictionary<int, int> commentCounts,
            HashSet<int> visited)
        {
            // Chống chu trình cha-con do dữ liệu hỏng, nếu không vòng đệ quy sẽ treo.
            if (!visited.Add(task.Id)) return;

            int comments;
            commentCounts.TryGetValue(task.Id, out comments);
            rows.Add(new ChecklistRow { Task = task, Depth = depth, CommentCount = comments });

            List<WorkTask> children;
            if (!byParent.TryGetValue(task.Id, out children)) return;

            foreach (var child in children)
            {
                AddBranch(rows, child, depth + 1, byParent, commentCounts, visited);
            }
        }

        private static List<int> DescendantIds(int parentId, List<WorkTask> all)
        {
            var result = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(parentId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var child in all.Where(t => t.ParentId == current))
                {
                    if (result.Contains(child.Id)) continue;
                    result.Add(child.Id);
                    queue.Enqueue(child.Id);
                }
            }

            return result;
        }

        private static bool IsDescendantOf(WorkTask candidate, int ancestorId)
        {
            var guard = 0;
            var current = candidate;

            while (current != null && current.ParentId > 0 && guard++ < 50)
            {
                if (current.ParentId == ancestorId) return true;
                current = Repository.WorkTasks.Find(current.ParentId);
            }

            return false;
        }

        private static int NextSortOrder(int projectId, int parentId)
        {
            var siblings = Repository.WorkTasks.All()
                .Where(t => t.ProjectId == projectId && TaskKinds.InProject.Contains(t.Kind)
                            && t.ParentId == parentId)
                .ToList();

            return siblings.Count == 0 ? 10 : siblings.Max(t => t.SortOrder) + 10;
        }

        private void PopulateEditLists(WorkTask task)
        {
            ViewBag.Users = AssignableUsers(task);
            ViewBag.States = TaskStates.All;
            ViewBag.ParentOptions = ProjectTasks(task.ProjectId)
                .Where(t => t.Id != task.Id)
                .ToList();
        }

        /// <summary>
        /// Người giao được việc: thành viên ĐANG tham gia dự án, PM lên đầu. Người thực hiện
        /// hiện tại của việc vẫn được giữ trong danh sách dù đã rời dự án — mở form sửa mà
        /// thiếu người đó là mất lựa chọn đang lưu.
        /// </summary>
        private static List<User> AssignableUsers(WorkTask task)
        {
            var users = ProjectMembers(task.ProjectId)
                .Select(m => m.User)
                .ToList();

            if (task.AssigneeUserId > 0 && users.All(u => u.Id != task.AssigneeUserId))
            {
                var current = Repository.Users.Find(task.AssigneeUserId);
                if (current != null) users.Add(current);
            }

            return users;
        }
    }
}
