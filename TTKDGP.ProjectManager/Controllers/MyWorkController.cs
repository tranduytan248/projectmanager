using System;
using System.Collections.Generic;
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
    /// Màn cá nhân của bộ quản lý công việc: việc được giao, dự án đang tham gia, báo cáo tiến độ
    /// và trao đổi với PM. Việc gán thẳng theo tài khoản đăng nhập nên ai đăng nhập được là thấy
    /// ngay việc của mình, không cần gắn hồ sơ nhân sự nào.
    /// </summary>
    [AppAuthorize]
    public class MyWorkController : BaseController
    {
        /// <summary>
        /// Toàn bộ công việc của cá nhân trong MỘT danh sách, lọc được theo dự án — dùng cả để nhìn
        /// nhanh việc đang mở lẫn lục lại "tôi đã làm gì ở dự án X".
        /// </summary>
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Tasks(int projectId = 0, string state = null, string kind = null,
            string q = null, bool showClosed = false, int page = 1, int year = 0, int month = 0)
        {
            var mine = WorkService.TasksOfUser(CurrentUserId);

            // Danh sách dự án để đổ bộ lọc lấy từ CHÍNH việc của người này, không lấy toàn bộ dự án
            // của Tổ — lọc theo một dự án mình không có việc nào thì chỉ ra danh sách rỗng.
            var projectOptions = mine
                .Where(t => t.ProjectId > 0)
                .GroupBy(t => t.ProjectId)
                .Select(g => new ProjectOption
                {
                    Id = g.Key,
                    Name = g.Select(t => t.ProjectName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                           ?? ("Dự án #" + g.Key),
                    Count = g.Count()
                })
                .OrderBy(o => o.Name, StringComparer.CurrentCulture)
                .ToList();

            var standaloneCount = mine.Count(t => t.ProjectId <= 0);

            var items = mine.AsEnumerable();

            if (!showClosed) items = items.Where(t => !TaskStates.IsClosed(t.State));

            // projectId âm là quy ước cho "việc ngoài dự án" — số 0 đã mang nghĩa "tất cả".
            if (projectId > 0) items = items.Where(t => t.ProjectId == projectId);
            else if (projectId < 0) items = items.Where(t => t.ProjectId <= 0);

            if (!string.IsNullOrWhiteSpace(state)) items = items.Where(t => t.State == state);
            if (!string.IsNullOrWhiteSpace(kind)) items = items.Where(t => t.Kind == kind);

            // Lọc theo tháng: cả hai ô đều phải chọn thì mới lọc — chọn mỗi năm hay mỗi tháng thì
            // chưa xác định được kỳ nào. Dùng chung phép xét tháng với bộ chấm KPI và màn Tổng quan.
            var byMonth = year > 0 && month >= 1 && month <= 12;
            if (byMonth) items = items.Where(t => KpiService.TaskInMonth(t, year, month));

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                items = items.Where(t =>
                    (t.Title ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || (t.Code ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0);
            }

            var list = WorkService.Sort(items);

            var model = new MyTasksViewModel
            {
                UserFullName = CurrentUser.FullName,
                ProjectId = projectId,
                State = state,
                Kind = kind,
                Query = q,
                ShowClosed = showClosed,
                Year = year,
                Month = month,
                Projects = projectOptions,
                StandaloneCount = standaloneCount,
                OverdueCount = list.Count(t => t.IsOverdue),
                DoneCount = list.Count(t => t.State == TaskStates.Done),
                Tasks = PagedList<WorkTask>.From(list, page, WorkService.PageSize)
            };

            return View(model);
        }

        /// <summary>
        /// Dự án tôi đang/đã tham gia hoặc đang làm PM.
        ///
        /// Cần màn riêng vì thành viên dự án và PM KHÔNG có quyền wprojects.view (đó là quyền Quản
        /// lý Tổ, cho xem toàn bộ dự án của Tổ). Không có màn này thì PM không có đường vào
        /// checklist của chính dự án mình.
        /// </summary>
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Projects(string q = null, string phase = null, string state = null,
            string role = null, bool showClosed = false, int page = 1)
        {
            var userId = CurrentUserId;
            var year = WeekHelper.CurrentYear;
            var week = WeekHelper.CurrentWeek;

            // Nạp một lượt rồi ghép trong bộ nhớ, không gọi Find() trong vòng lặp.
            var projects = Repository.WorkProjects.All();
            var myAssignments = Repository.WorkAssignments.All().Where(a => a.UserId == userId).ToList();
            var tasks = WorkService.AllTasks();
            var stats = WorkService.StatsByProject(tasks);
            var reports = Repository.WorkWeekReports.All()
                .Where(r => r.Year == year && r.Week == week).ToList();

            var rows = new List<MyProjectRow>();

            foreach (var project in projects)
            {
                var isPm = project.PmUserId == userId;
                var terms = myAssignments.Where(a => a.ProjectId == project.Id).ToList();
                if (!isPm && terms.Count == 0) continue;

                TaskStat stat;
                stats.TryGetValue(project.Id, out stat);

                var open = terms.FirstOrDefault(t => t.IsActive);

                rows.Add(new MyProjectRow
                {
                    Project = project,
                    IsPm = isPm,
                    IsActiveMember = open != null,
                    Role = open != null ? open.Role : terms.Select(t => t.Role).FirstOrDefault(),
                    JoinedAt = terms.Count > 0 ? terms.Min(t => t.JoinedAt) : (DateTime?)null,
                    LeftAt = open != null || terms.Count == 0 ? null : terms.Max(t => t.LeftAt),
                    Stat = stat ?? new TaskStat(),
                    MyOpenCount = tasks.Count(t => t.ProjectId == project.Id
                        && t.AssigneeUserId == userId && !TaskStates.IsClosed(t.State)),
                    MyOverdueCount = tasks.Count(t => t.ProjectId == project.Id
                        && t.AssigneeUserId == userId && t.IsOverdue),
                    Report = reports.FirstOrDefault(r => r.ProjectId == project.Id)
                });
            }

            // Sắp xếp TRƯỚC khi lọc để các ô đếm và danh sách trạng thái luôn theo cùng một thứ tự,
            // dù người dùng đang lọc kiểu gì.
            rows = rows
                .OrderByDescending(r => r.IsPm)
                .ThenByDescending(r => r.IsActiveMember)
                .ThenBy(r => r.Project.Name, StringComparer.CurrentCulture)
                .ToList();

            // Danh sách trạng thái để đổ ô lọc lấy từ CHÍNH dự án của người này — lọc theo trạng
            // thái mình không có dự án nào thì chỉ ra danh sách rỗng. Cùng lối nghĩ với ô lọc dự án
            // ở màn Công việc của tôi.
            var stateOptions = rows
                .Select(r => r.Project.State)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => Array.IndexOf(ProjectStates.All, s))
                .ToList();

            var roleOptions = rows
                .Select(r => r.Role)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(s => s, StringComparer.CurrentCulture)
                .ToList();

            var items = rows.AsEnumerable();

            // Mặc định giấu dự án đã đóng (huỷ / kết thúc hỗ trợ) — danh sách dài dần theo năm
            // tháng, người dùng gần như luôn chỉ quan tâm dự án còn chạy.
            if (!showClosed) items = items.Where(r => r.Project.IsOpen);

            if (!string.IsNullOrWhiteSpace(phase)) items = items.Where(r => r.Project.Phase == phase);
            if (!string.IsNullOrWhiteSpace(state)) items = items.Where(r => r.Project.State == state);

            // "pm" là quy ước cho "dự án tôi làm PM" — vai trò tự do không bao giờ nhận giá trị này.
            if (!string.IsNullOrWhiteSpace(role))
            {
                items = role == MyProjectsViewModel.RolePm
                    ? items.Where(r => r.IsPm)
                    : items.Where(r => string.Equals(r.Role, role, StringComparison.CurrentCultureIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                items = items.Where(r =>
                    (r.Project.Name ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || (r.Project.Code ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || (r.Project.Customer ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || (r.Project.PmName ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0);
            }

            var list = items.ToList();

            ViewBag.Year = year;
            ViewBag.Week = week;

            return View(new MyProjectsViewModel
            {
                Query = q,
                Phase = phase,
                State = state,
                Role = role,
                ShowClosed = showClosed,
                States = stateOptions,
                Roles = roleOptions,
                // Các con số tổng đếm trên TOÀN BỘ dự án của người này, không theo bộ lọc — để
                // người dùng còn biết mình đang giấu mất bao nhiêu.
                TotalCount = rows.Count,
                PmCount = rows.Count(r => r.IsPm),
                ClosedCount = rows.Count(r => !r.Project.IsOpen),
                Rows = PagedList<MyProjectRow>.From(list, page, WorkService.PageSize)
            });
        }

        // ---------- Chi tiết một đầu việc ----------

        [HttpGet]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Detail(int id)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            ViewBag.States = TaskStates.All;
            ViewBag.CanReport = CanEditTask(task);
            ViewBag.Comments = WorkService.CommentsOfTask(id);

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Report(int id, string state, int progress, string note)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null) return HttpNotFound();

            // Chỉ người được giao và người quán xuyến dự án mới cập nhật được tiến độ.
            // So thẳng AssigneeUserId với CurrentUserId là hở: việc CHƯA GIAO có AssigneeUserId
            // bằng 0, mà khách chưa đăng nhập cũng mang số 0 — hai số 0 khớp nhau thì ai cũng
            // báo cáo được việc chưa giao. CanEditTask đã chặn trường hợp đó.
            if (!CanEditTask(task)) return HttpNotFound();

            if (!TaskStates.All.Contains(state))
            {
                NotifyError("Trạng thái không hợp lệ.");
                return RedirectToAction("Detail", new { id = id });
            }

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

            // Nói thẳng ảnh hưởng tới điểm để người dùng biết ngay, khỏi chờ tới cuối tháng.
            if (task.State == TaskStates.Done)
            {
                Notify(task.IsOnTime
                    ? string.Format("Đã ghi nhận hoàn thành \"{0}\" — đúng hạn.", task.Title)
                    : string.Format("Đã ghi nhận hoàn thành \"{0}\" — TRỄ HẠN {1} ngày.",
                        task.Title, task.LateDays));
            }
            else
            {
                Notify(string.Format("Đã cập nhật tiến độ \"{0}\" — {1}%.", task.Title, task.Progress));
            }

            return RedirectToAction("Detail", new { id = id });
        }

        /// <summary>
        /// Tải file đính kèm của một ĐẦU VIỆC (file giao kèm lúc giao việc riêng). Ai xem được
        /// việc thì tải được; chưa đăng nhập sẽ bị đưa qua màn đăng nhập trước.
        /// </summary>
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Attachment(int id)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task) || !task.HasAttachment) return HttpNotFound();

            var path = CommentAttachments.FullPath(task.AttachmentFile);
            if (path == null) return HttpNotFound();

            // Luôn trả kiểu tải-về chung chung: trình duyệt tải file chứ không thực thi/nhúng.
            return File(path, "application/octet-stream",
                string.IsNullOrWhiteSpace(task.AttachmentName) ? "tep-dinh-kem" : task.AttachmentName);
        }

        // ---------- Trao đổi ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Comment(int id, string content, HttpPostedFileBase file)
        {
            var task = Repository.WorkTasks.Find(id);
            if (task == null || !CanSeeTask(task)) return HttpNotFound();

            var hasText = !string.IsNullOrWhiteSpace(content);
            var hasFile = file != null && file.ContentLength > 0;

            if (!hasText && !hasFile)
            {
                NotifyError("Hãy nhập nội dung hoặc chọn file đính kèm.");
                return RedirectToAction("Detail", new { id = id });
            }

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
                NotifyError(error);
                return RedirectToAction("Detail", new { id = id });
            }

            Repository.WorkComments.Insert(comment);

            // Báo cho những người bị nhắc tên bằng @ trong nội dung (web + email).
            NotificationService.Mentions(task, comment, CurrentUserId,
                CurrentUser == null ? null : CurrentUser.FullName);

            Notify("Đã gửi trao đổi.");
            return RedirectToAction("Detail", new { id = id });
        }

        /// <summary>Thu hồi nội dung. Không xoá cứng để mạch hội thoại không đứt quãng.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult DeleteComment(int id, int commentId)
        {
            var comment = Repository.WorkComments.Find(commentId);
            if (comment == null || comment.TaskId != id) return HttpNotFound();

            var task = Repository.WorkTasks.Find(id);
            if (task == null) return HttpNotFound();

            if (comment.UserId != CurrentUserId && !CanEditProject(task.ProjectId))
            {
                return HttpNotFound();
            }

            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.Now;
            Repository.WorkComments.Update(comment);

            Notify("Đã thu hồi nội dung trao đổi.");
            return RedirectToAction("Detail", new { id = id });
        }

    }

    /// <summary>Một dự án trong ô lọc, kèm số việc của cá nhân ở dự án đó.</summary>
    public class ProjectOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class MyTasksViewModel
    {
        public string UserFullName { get; set; }

        // ----- Điều kiện lọc đang áp dụng, giữ lại để dựng lại form và các liên kết phân trang -----
        public int ProjectId { get; set; }
        public string State { get; set; }
        public string Kind { get; set; }
        public string Query { get; set; }
        public bool ShowClosed { get; set; }

        /// <summary>Kỳ đang lọc; 0 nghĩa là không lọc theo tháng.</summary>
        public int Year { get; set; }
        public int Month { get; set; }

        public List<ProjectOption> Projects { get; set; }

        /// <summary>Số việc ngoài dự án — quyết định có hiện mục lọc riêng cho nó không.</summary>
        public int StandaloneCount { get; set; }

        public int OverdueCount { get; set; }
        public int DoneCount { get; set; }

        public PagedList<WorkTask> Tasks { get; set; }

        /// <summary>Có điều kiện lọc nào đang bật không — để hiện nút bỏ lọc.</summary>
        public bool HasFilter
        {
            get
            {
                return ProjectId != 0 || ShowClosed
                       || (Year > 0 && Month > 0)
                       || !string.IsNullOrWhiteSpace(State)
                       || !string.IsNullOrWhiteSpace(Kind)
                       || !string.IsNullOrWhiteSpace(Query);
            }
        }

        public MyTasksViewModel()
        {
            Projects = new List<ProjectOption>();
            Tasks = new PagedList<WorkTask>();
        }
    }

    /// <summary>
    /// Màn "Dự án của tôi" kèm bộ lọc. Tách thành view model thay vì nhét vào ViewBag để các liên
    /// kết phân trang dựng lại được đúng điều kiện lọc đang áp dụng.
    /// </summary>
    public class MyProjectsViewModel
    {
        /// <summary>Giá trị quy ước của ô lọc vai trò cho "dự án tôi làm PM".</summary>
        public const string RolePm = "pm";

        // ----- Điều kiện lọc đang áp dụng -----
        public string Query { get; set; }
        public string Phase { get; set; }
        public string State { get; set; }
        public string Role { get; set; }
        public bool ShowClosed { get; set; }

        /// <summary>Các trạng thái có thật trong dự án của người này — nguồn của ô lọc trạng thái.</summary>
        public List<string> States { get; set; }

        /// <summary>Các vai trò có thật trong dự án của người này.</summary>
        public List<string> Roles { get; set; }

        // ----- Các con số đếm trên TOÀN BỘ dự án, không theo bộ lọc -----
        public int TotalCount { get; set; }
        public int PmCount { get; set; }
        public int ClosedCount { get; set; }

        public PagedList<MyProjectRow> Rows { get; set; }

        /// <summary>Có điều kiện lọc nào đang bật không — để hiện nút bỏ lọc.</summary>
        public bool HasFilter
        {
            get
            {
                return ShowClosed
                       || !string.IsNullOrWhiteSpace(Query)
                       || !string.IsNullOrWhiteSpace(Phase)
                       || !string.IsNullOrWhiteSpace(State)
                       || !string.IsNullOrWhiteSpace(Role);
            }
        }

        public MyProjectsViewModel()
        {
            States = new List<string>();
            Roles = new List<string>();
            Rows = new PagedList<MyProjectRow>();
        }
    }

    public class MyProjectRow
    {
        public WorkProject Project { get; set; }
        public bool IsPm { get; set; }
        public bool IsActiveMember { get; set; }
        public string Role { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }

        public TaskStat Stat { get; set; }
        public int MyOpenCount { get; set; }
        public int MyOverdueCount { get; set; }

        public WorkWeekReport Report { get; set; }
    }
}
