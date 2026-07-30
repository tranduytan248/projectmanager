using System;
using System.Collections.Generic;
using System.IO;
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
    /// Dự án trong bộ quản lý công việc, kèm màn phân công theo giai đoạn. Người tham gia dự án
    /// là NGƯỜI DÙNG của hệ thống (bảng Users), không qua danh sách nhân sự riêng.
    ///
    /// Thêm/sửa/xoá dự án và chỉ định PM là việc của Quản lý Tổ. Phân công người tham gia thì cả
    /// PM của chính dự án đó cũng làm được (kiểm bằng CanEditProject).
    /// </summary>
    [AppAuthorize]
    public class WorkProjectsController : BaseController
    {
        [AppAuthorize(Permission = "wprojects.view")]
        public ActionResult Index(string keyword, string phase, string state, string projectType,
            int? pmUserId, int page = 1)
        {
            var items = Repository.WorkProjects.All();

            if (!string.IsNullOrWhiteSpace(phase)) items = items.Where(p => p.Phase == phase).ToList();
            if (!string.IsNullOrWhiteSpace(state)) items = items.Where(p => p.State == state).ToList();
            if (!string.IsNullOrWhiteSpace(projectType))
            {
                items = items.Where(p => p.ProjectType == projectType).ToList();
            }
            if (pmUserId.HasValue && pmUserId.Value > 0)
            {
                items = items.Where(p => p.PmUserId == pmUserId.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                items = items.Where(p => Has(p.Name, k) || Has(p.Code, k)
                                      || Has(p.Customer, k) || Has(p.PmName, k)).ToList();
            }

            // Nạp một lượt rồi ghép, không đếm lại cho từng dòng.
            var tasks = WorkService.AllTasks();
            var stats = WorkService.StatsByProject(tasks);
            var assignments = Repository.WorkAssignments.All();

            ViewBag.Stats = stats;
            ViewBag.MemberCounts = assignments
                .Where(a => a.IsActive)
                .GroupBy(a => a.ProjectId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.Keyword = keyword;
            ViewBag.Phase = phase;
            ViewBag.State = state;
            ViewBag.ProjectType = projectType;
            ViewBag.PmUserId = pmUserId;
            ViewBag.UserOptions = WorkService.ActiveUsers();

            // Loại dự án lấy từ danh mục dùng chung (Danh mục → Loại dự án), không phải danh sách cứng.
            ViewBag.ProjectTypes = Repository.ActiveNames(Repository.ProjectTypes);

            return View(PagedList<WorkProject>.From(items
                .OrderByDescending(p => p.IsOpen)
                .ThenBy(p => p.Name, StringComparer.CurrentCulture)
                .ToList(), page, WorkService.PageSize));
        }

        /// <summary>
        /// Xem toàn bộ thông tin một dự án ở một chỗ: thông tin chung, nhân sự theo từng giai đoạn
        /// tham gia, tiến độ checklist và tình hình báo cáo tuần — khỏi phải mở lần lượt từng màn.
        /// </summary>
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Details(int id)
        {
            var project = Repository.WorkProjects.Find(id);
            if (project == null || !CanViewProject(id)) return HttpNotFound();

            var tasks = WorkService.TasksOfProject(id);
            var checklist = tasks.Where(t => t.Kind == TaskKinds.Checklist).ToList();
            var support = tasks.Where(t => t.Kind == TaskKinds.Support).ToList();

            var year = WeekHelper.CurrentYear;
            var week = WeekHelper.CurrentWeek;

            var model = new ProjectDetailsViewModel
            {
                Project = project,
                CanEdit = CanEditProject(id),
                Assignments = WorkService.AssignmentsOfProject(id),
                ChecklistStat = Stat(checklist),
                OverdueTasks = tasks.Where(t => t.IsOverdue)
                    .OrderBy(t => t.DueDate)
                    .Take(5)
                    .ToList(),
                SupportThisWeek = support.Count(t => t.Year == year && t.Week == week),
                Year = year,
                Week = week,
                CurrentReport = WorkService.FindReport(id, year, week),
                RecentReports = Repository.WorkWeekReports.All()
                    .Where(r => r.ProjectId == id)
                    .OrderByDescending(r => r.Year).ThenByDescending(r => r.Week)
                    .Take(5)
                    .ToList(),
                Files = Repository.WorkProjectFiles.All()
                    .Where(f => f.ProjectId == id)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList()
            };

            return View(model);
        }

        private static TaskStat Stat(List<WorkTask> items)
        {
            return new TaskStat
            {
                Total = items.Count,
                Done = items.Count(t => t.State == TaskStates.Done),
                Overdue = items.Count(t => t.IsOverdue)
            };
        }

        [HttpGet]
        [AppAuthorize(Permission = "wprojects.create,wprojects.edit")]
        public ActionResult Edit(int? id)
        {
            var project = id.HasValue
                ? Repository.WorkProjects.Find(id.Value)
                : new WorkProject
                {
                    ProjectType = Repository.ActiveNames(Repository.ProjectTypes).FirstOrDefault(),
                    Phase = ProjectPhases.Implement,
                    State = ProjectStates.DefaultFor(ProjectPhases.Implement),
                    StartDate = DateTime.Today
                };

            if (project == null) return HttpNotFound();

            PopulateEditLists(project);

            // Mở từ hộp thoại thì chỉ trả về phần form; mở thẳng bằng đường dẫn thì trả cả trang.
            return Request.IsAjaxRequest() ? (ActionResult)PartialView("_EditForm", project) : View(project);
        }

        /// <summary>
        /// Giữ lại giá trị đang có kể cả khi nó đã bị gỡ khỏi danh mục — mở dự án cũ ra sửa mà mất
        /// lựa chọn thì lưu lại sẽ vô tình xoá mất loại dự án.
        /// </summary>
        private void PopulateEditLists(WorkProject project)
        {
            ViewBag.UserOptions = WorkService.ActiveUsers();
            ViewBag.ProjectTypes = Repository.NamesForEdit(
                Repository.ProjectTypes, project == null ? null : project.ProjectType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wprojects.create,wprojects.edit")]
        public ActionResult Edit(WorkProject model, IEnumerable<HttpPostedFileBase> files)
        {
            if (model.StartDate.HasValue && model.EndDate.HasValue
                && model.EndDate.Value.Date < model.StartDate.Value.Date)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            if (string.IsNullOrWhiteSpace(model.Phase)) model.Phase = ProjectPhases.Implement;

            // Kiểm ở máy chủ chứ không chỉ ở giao diện: ô trạng thái được lọc lại bằng JavaScript
            // theo giai đoạn, mà JavaScript thì sửa được — gửi thẳng biểu mẫu vẫn lọt trạng thái
            // của giai đoạn khác, làm dữ liệu lệch mà không ai biết.
            if (!ProjectStates.BelongsTo(model.State, model.Phase))
            {
                ModelState.AddModelError("State", string.Format(
                    "Trạng thái \"{0}\" không thuộc giai đoạn {1}.",
                    ProjectStates.Display(model.State), ProjectPhases.Display(model.Phase)));
            }

            // Để trống mã thì tự sinh từ tên dự án — đỡ phải nghĩ ra mã, và bảo đảm dự án nào cũng
            // có mã để đối chiếu về sau.
            if (string.IsNullOrWhiteSpace(model.Code) && !string.IsNullOrWhiteSpace(model.Name))
            {
                model.Code = WorkService.GenerateProjectCode(
                    model.Name, WorkService.ExistingProjectCodes(model.Id));
            }

            if (!string.IsNullOrWhiteSpace(model.Code))
            {
                var dup = Repository.WorkProjects.FirstOrDefault(p =>
                    p.Id != model.Id && string.Equals(p.Code, model.Code.Trim(),
                        StringComparison.CurrentCultureIgnoreCase));
                if (dup != null) ModelState.AddModelError("Code", "Mã dự án đã được dùng.");
            }

            if (!ModelState.IsValid)
            {
                PopulateEditLists(model);

                // Trả lại chính form kèm lỗi để hộp thoại hiện tại chỗ, không mất dữ liệu đã nhập.
                return Request.IsAjaxRequest()
                    ? (ActionResult)PartialView("_EditForm", model)
                    : View(model);
            }

            model.Name = model.Name.Trim();
            if (!string.IsNullOrWhiteSpace(model.Code)) model.Code = model.Code.Trim();
            model.PmName = WorkService.UserFullName(model.PmUserId);

            // Mô tả đến từ trình soạn thảo WYSIWYG (HTML) — lọc về tập thẻ an toàn TRƯỚC khi lưu,
            // vì chỗ hiển thị dùng Html.Raw.
            model.Description = HtmlSanitizer.Clean(model.Description);
            TrimDeployInfo(model);

            var now = DateTime.Now;

            if (model.Id == 0)
            {
                model.CreatedAt = now;
                var saved = Repository.WorkProjects.Insert(model);
                SaveProjectFiles(saved.Id, files);

                Notify(string.Format("Đã thêm dự án \"{0}\". Bước tiếp theo: phân công nhân sự.", saved.Name));
                return RedirectToAction("Members", new { id = saved.Id });
            }

            var current = Repository.WorkProjects.Find(model.Id);
            if (current == null) return HttpNotFound();

            model.CreatedAt = current.CreatedAt;
            model.UpdatedAt = now;
            Repository.WorkProjects.Update(model);
            SaveProjectFiles(model.Id, files);

            // Tên PM lưu kèm ở phân công/đầu việc không tự đổi theo, nhưng tên dự án thì nên đồng bộ
            // để danh sách "Công việc của tôi" không hiện tên cũ.
            if (current.Name != model.Name)
            {
                Repository.WorkTasks.UpdateWhere(t => t.ProjectId == model.Id, t => t.ProjectName = model.Name);
                Repository.WorkAssignments.UpdateWhere(a => a.ProjectId == model.Id, a => a.ProjectName = model.Name);
            }

            Notify(string.Format("Đã cập nhật dự án \"{0}\".", model.Name));
            return Saved("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wprojects.delete")]
        public ActionResult Delete(int id)
        {
            var project = Repository.WorkProjects.Find(id);
            if (project == null) return HttpNotFound();

            // Dự án đã phát sinh công việc thì KHÔNG xoá — phiếu chấm KPI của các kỳ trước còn trỏ
            // vào những đầu việc này. Chuyển trạng thái sang Huỷ là đủ để nó biến khỏi danh sách.
            var taskCount = Repository.WorkTasks.All().Count(t => t.ProjectId == id);
            if (taskCount > 0)
            {
                NotifyError(string.Format(
                    "Không xoá được \"{0}\" vì đã có {1} đầu việc. Hãy chuyển trạng thái sang Huỷ.",
                    project.Name, taskCount));
                return RedirectToAction("Index");
            }

            Repository.WorkAssignments.DeleteWhere(a => a.ProjectId == id);

            // Xoá cả tài liệu đính kèm: bản ghi lẫn file trên đĩa, không để file mồ côi.
            foreach (var f in Repository.WorkProjectFiles.All().Where(f => f.ProjectId == id))
            {
                CommentAttachments.Delete(f.StoredName);
            }
            Repository.WorkProjectFiles.DeleteWhere(f => f.ProjectId == id);

            Repository.WorkProjects.Delete(id);

            Notify(string.Format("Đã xoá dự án \"{0}\".", project.Name));
            return RedirectToAction("Index");
        }

        // ---------- Tài liệu đính kèm của dự án ----------

        /// <summary>Cắt khoảng trắng thừa của các trường thông tin triển khai trước khi lưu.</summary>
        private static void TrimDeployInfo(WorkProject model)
        {
            model.GithubLink = TrimOrNull(model.GithubLink);
            model.SvnLink = TrimOrNull(model.SvnLink);
            model.FtpAccount = TrimOrNull(model.FtpAccount);
            model.FtpPassword = TrimOrNull(model.FtpPassword);
            model.DbType = TrimOrNull(model.DbType);
            model.DbServer = TrimOrNull(model.DbServer);
            model.DbUsername = TrimOrNull(model.DbUsername);
            model.DbPassword = TrimOrNull(model.DbPassword);
        }

        private static string TrimOrNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Lưu các file gửi kèm form vào kho đính kèm chung. File bị từ chối (quá cỡ, sai đuôi)
        /// không chặn việc lưu dự án — chỉ báo lại để người dùng gửi lại file đó.
        /// </summary>
        private void SaveProjectFiles(int projectId, IEnumerable<HttpPostedFileBase> files)
        {
            if (files == null) return;

            var rejected = new List<string>();
            foreach (var file in files)
            {
                if (file == null || file.ContentLength <= 0) continue;

                string stored, name, error;
                long size;
                if (!CommentAttachments.TrySaveFile(file, out stored, out name, out size, out error))
                {
                    rejected.Add(string.Format("\"{0}\" ({1})", Path.GetFileName(file.FileName), error));
                    continue;
                }
                if (stored == null) continue;

                Repository.WorkProjectFiles.Insert(new WorkProjectFile
                {
                    ProjectId = projectId,
                    StoredName = stored,
                    OriginalName = name,
                    Size = size,
                    UploadedByUserId = CurrentUserId,
                    UploadedByName = CurrentUser == null ? null : CurrentUser.FullName,
                    CreatedAt = DateTime.Now
                });
            }

            if (rejected.Count > 0)
            {
                NotifyError("Không nhận được file: " + string.Join("; ", rejected));
            }
        }

        /// <summary>Thêm tài liệu từ trang chi tiết dự án.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult UploadFiles(int id, IEnumerable<HttpPostedFileBase> files)
        {
            var project = Repository.WorkProjects.Find(id);
            if (project == null || !CanEditProject(id)) return HttpNotFound();

            var before = Repository.WorkProjectFiles.All().Count(f => f.ProjectId == id);
            SaveProjectFiles(id, files);
            var added = Repository.WorkProjectFiles.All().Count(f => f.ProjectId == id) - before;

            if (added > 0) Notify(string.Format("Đã thêm {0} file vào dự án.", added));
            return RedirectToAction("Details", new { id = id });
        }

        /// <summary>Tải một tài liệu của dự án. Ai xem được dự án thì tải được.</summary>
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult DownloadFile(int id, int fileId)
        {
            var record = Repository.WorkProjectFiles.Find(fileId);
            if (record == null || record.ProjectId != id || !CanViewProject(id)) return HttpNotFound();

            var path = CommentAttachments.FullPath(record.StoredName);
            if (path == null) return HttpNotFound();

            // Luôn trả kiểu tải-về chung chung: trình duyệt tải file chứ không thực thi/nhúng.
            return File(path, "application/octet-stream",
                string.IsNullOrWhiteSpace(record.OriginalName) ? "tai-lieu" : record.OriginalName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult DeleteFile(int id, int fileId)
        {
            var record = Repository.WorkProjectFiles.Find(fileId);
            if (record == null || record.ProjectId != id || !CanEditProject(id)) return HttpNotFound();

            CommentAttachments.Delete(record.StoredName);
            Repository.WorkProjectFiles.Delete(fileId);

            Notify(string.Format("Đã xoá file \"{0}\".", record.OriginalName));
            return RedirectToAction("Details", new { id = id });
        }

        // ---------- Sinh mã dự án ----------

        /// <summary>Sinh thử một mã từ tên dự án, cho nút "Tạo mã" trên form gọi.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wprojects.create,wprojects.edit")]
        public JsonResult SuggestCode(string name, int id = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { ok = false, message = "Hãy nhập tên dự án trước." });
            }

            var code = WorkService.GenerateProjectCode(name, WorkService.ExistingProjectCodes(id));
            return Json(new { ok = true, code = code });
        }

        /// <summary>Sinh mã cho những dự án chưa có mã — dùng sau khi chuyển dữ liệu từ bộ cũ.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wprojects.edit")]
        public ActionResult FillMissingCodes()
        {
            var taken = WorkService.ExistingProjectCodes();
            var filled = 0;

            foreach (var project in Repository.WorkProjects.All()
                         .Where(p => string.IsNullOrWhiteSpace(p.Code)))
            {
                var code = WorkService.GenerateProjectCode(project.Name, taken);
                taken.Add(code);

                project.Code = code;
                project.UpdatedAt = DateTime.Now;
                Repository.WorkProjects.Update(project);
                filled++;
            }

            Notify(filled == 0
                ? "Mọi dự án đều đã có mã."
                : string.Format("Đã sinh mã cho {0} dự án.", filled));

            return RedirectToAction("Index");
        }

        // ---------- Lấy dự án từ bộ cũ ----------

        /// <summary>
        /// Xem trước việc chuyển dự án từ bảng Projects cũ sang bảng WorkProjects mới.
        /// Chỉ ĐỌC bộ cũ, không sửa gì bên đó. Chưa chuyển nhân sự tham gia.
        /// </summary>
        [HttpGet]
        [AppAuthorize(Permission = "wprojects.create")]
        public ActionResult ImportLegacy()
        {
            return View(BuildLegacyPreview());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wprojects.create")]
        public ActionResult ImportLegacyConfirm()
        {
            var model = BuildLegacyPreview();
            var now = DateTime.Now;
            var added = 0;

            foreach (var row in model.Rows.Where(r => r.Outcome == LegacyProjectRow.Add))
            {
                row.Prepared.CreatedAt = now;
                Repository.WorkProjects.Insert(row.Prepared);
                added++;
            }

            // Dựng lại danh sách sau khi ghi thay vì tin vào TempData: phiên có thể hết hạn, và
            // dựng lại thì con số báo về chắc chắn khớp với thứ vừa ghi.
            Notify(string.Format(
                "Đã chuyển {0} dự án từ bộ cũ sang. Chưa chuyển nhân sự tham gia — vào từng dự án "
                + "bấm \"Nhân sự dự án\" để phân công.", added));

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Dựng danh sách xem trước. Gọi lại được nhiều lần và cho kết quả như nhau, nên bấm nhầm
        /// hai lần cũng không nhân đôi dữ liệu — dự án đã có tên trùng sẽ bị bỏ qua.
        /// </summary>
        private LegacyImportViewModel BuildLegacyPreview()
        {
            var model = new LegacyImportViewModel();

            var existingNames = new HashSet<string>(
                Repository.WorkProjects.All().Select(p => (p.Name ?? string.Empty).Trim()),
                StringComparer.CurrentCultureIgnoreCase);

            // Nạp một lượt để nối PM cũ sang tài khoản người dùng theo họ tên.
            var legacyMembers = Repository.Members.All().ToDictionary(m => m.Id);
            var usersByName = new Dictionary<string, User>(StringComparer.CurrentCultureIgnoreCase);
            foreach (var u in Repository.Users.All().Where(u => u.IsActive))
            {
                var key = (u.FullName ?? string.Empty).Trim();
                if (key.Length > 0 && !usersByName.ContainsKey(key)) usersByName[key] = u;
            }

            foreach (var source in Repository.Projects.All()
                         .OrderBy(p => p.Name, StringComparer.CurrentCulture))
            {
                var row = new LegacyProjectRow { Source = source };
                var name = (source.Name ?? string.Empty).Trim();

                if (name.Length == 0)
                {
                    row.Outcome = LegacyProjectRow.Skip;
                    row.Message = "Dự án cũ không có tên.";
                    model.Rows.Add(row);
                    continue;
                }

                if (existingNames.Contains(name))
                {
                    row.Outcome = LegacyProjectRow.Skip;
                    row.Message = "Bộ mới đã có dự án cùng tên.";
                    model.Rows.Add(row);
                    continue;
                }

                // Đánh dấu ngay để hai dự án cũ trùng tên nhau cũng chỉ vào một lần.
                existingNames.Add(name);

                string phase, state;
                row.StatusGuessed = !MapStatus(source.CurrentStatus, out phase, out state);

                Member legacyPm = null;
                if (source.PmMemberId > 0) legacyMembers.TryGetValue(source.PmMemberId, out legacyPm);
                row.LegacyPmName = legacyPm == null ? null : legacyPm.FullName;

                User pmUser = null;
                if (legacyPm != null && !string.IsNullOrWhiteSpace(legacyPm.FullName))
                {
                    usersByName.TryGetValue(legacyPm.FullName.Trim(), out pmUser);
                }
                row.PmMatched = pmUser != null;

                row.Prepared = new WorkProject
                {
                    Name = name,
                    Customer = source.Customer,
                    ProjectType = source.ProjectType,
                    Phase = phase,
                    State = state,
                    PmUserId = pmUser == null ? 0 : pmUser.Id,
                    PmName = pmUser == null ? null : pmUser.FullName,
                    Description = BuildDescription(source)
                };

                row.Outcome = LegacyProjectRow.Add;

                var notes = new List<string>();
                if (row.StatusGuessed)
                {
                    notes.Add(string.Format("Trạng thái cũ \"{0}\" chưa có trong bảng ánh xạ, tạm đặt {1}.",
                        string.IsNullOrWhiteSpace(source.CurrentStatus) ? "(trống)" : source.CurrentStatus,
                        ProjectStates.Display(state)));
                }
                if (!string.IsNullOrWhiteSpace(row.LegacyPmName) && !row.PmMatched)
                {
                    notes.Add(string.Format("Chưa tìm thấy người dùng nào tên \"{0}\" — để trống PM.",
                        row.LegacyPmName));
                }
                row.Message = string.Join(" ", notes);

                model.Rows.Add(row);
            }

            return model;
        }

        /// <summary>
        /// Ánh xạ trạng thái của bộ cũ (một danh mục chữ tự do) sang cặp giai đoạn + trạng thái
        /// của bộ mới. Trả về false khi không khớp mục nào — lúc đó dùng giá trị đoán và màn xem
        /// trước sẽ đánh dấu để người dùng tự sửa lại sau.
        /// </summary>
        private static bool MapStatus(string legacyStatus, out string phase, out string state)
        {
            phase = ProjectPhases.Implement;
            state = ProjectStates.Implementing;

            var s = (legacyStatus ?? string.Empty).Trim();
            if (s.Length == 0) return false;

            if (Same(s, "Đang thực hiện")) { state = ProjectStates.Implementing; return true; }
            if (Same(s, "Đang thử nghiệm")) { state = ProjectStates.Testing; return true; }
            if (Same(s, "Đang chờ")) { state = ProjectStates.Preparing; return true; }
            if (Same(s, "Chuẩn bị")) { state = ProjectStates.Preparing; return true; }

            // Dữ liệu cũ có dự án chưa kịp cập nhật trạng thái; coi như chưa bắt đầu triển khai.
            if (Same(s, "Chưa có thông tin")) { state = ProjectStates.Preparing; return true; }
            if (Same(s, "Tạm dừng")) { state = ProjectStates.ImplementPaused; return true; }
            if (Same(s, "Hoàn thành")) { state = ProjectStates.Accepted; return true; }
            if (Same(s, "Đã nghiệm thu")) { state = ProjectStates.Accepted; return true; }
            if (Same(s, "Huỷ") || Same(s, "Hủy")) { state = ProjectStates.Cancelled; return true; }

            if (Same(s, "Đang hỗ trợ"))
            {
                phase = ProjectPhases.Support;
                state = ProjectStates.Supporting;
                return true;
            }

            if (Same(s, "Kết thúc hỗ trợ"))
            {
                phase = ProjectPhases.Support;
                state = ProjectStates.SupportEnded;
                return true;
            }

            return false;
        }

        private static bool Same(string a, string b)
        {
            return string.Equals(a, b, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gom các trường của bộ cũ không có chỗ tương ứng ở bộ mới vào phần Mô tả, thay vì bỏ đi:
        /// ghi chú, trạng thái hợp đồng, doanh thu dự kiến và các đường dẫn Redmine/Git/SVN.
        /// </summary>
        private static string BuildDescription(Project source)
        {
            var lines = new List<string>();

            if (!string.IsNullOrWhiteSpace(source.Note)) lines.Add(source.Note.Trim());
            if (!string.IsNullOrWhiteSpace(source.ContractStatus)) lines.Add("Hợp đồng: " + source.ContractStatus.Trim());
            if (!string.IsNullOrWhiteSpace(source.ExpectedRevenue)) lines.Add("Doanh thu dự kiến: " + source.ExpectedRevenue.Trim());
            if (!string.IsNullOrWhiteSpace(source.Redmine)) lines.Add("Redmine / Google Sheet: " + source.Redmine.Trim());
            if (!string.IsNullOrWhiteSpace(source.Github)) lines.Add("Github: " + source.Github.Trim());
            if (!string.IsNullOrWhiteSpace(source.Svn)) lines.Add("SVN: " + source.Svn.Trim());
            if (!string.IsNullOrWhiteSpace(source.GeneralReport)) lines.Add("Báo cáo CV chung: " + source.GeneralReport.Trim());

            return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
        }

        // ---------- Phân công theo giai đoạn ----------

        [HttpGet]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult Members(int id, int page = 1)
        {
            var project = Repository.WorkProjects.Find(id);
            if (project == null || !CanViewProject(id)) return HttpNotFound();

            ViewBag.Project = project;
            ViewBag.CanEdit = CanEditProject(id);
            ViewBag.UserOptions = WorkService.ActiveUsers();
            ViewBag.PmName = WorkService.UserFullName(project.PmUserId);

            // Vai trò lấy từ danh mục dùng chung (Danh mục → Vai trò), không nhập tự do — để
            // danh sách vai trò của cả Tổ thống nhất và lọc/thống kê về sau còn gom được.
            ViewBag.Roles = Repository.ActiveNames(Repository.MemberRoles);

            var all = WorkService.AssignmentsOfProject(id);
            ViewBag.ActiveCount = all.Count(a => a.IsActive);
            ViewBag.TotalCount = all.Count;

            return View(PagedList<WorkAssignment>.From(all, page, WorkService.PageSize));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult AddMember(int id, int userId, string role, string phase,
            bool isPm, DateTime? joinedAt, string note)
        {
            if (!CanEditProject(id)) return HttpNotFound();

            var project = Repository.WorkProjects.Find(id);
            var person = Repository.Users.Find(userId);
            if (project == null || person == null) return HttpNotFound();

            // Hai giai đoạn cùng mở của một người trong một dự án thì không biết tính theo cái nào.
            var open = WorkService.OpenAssignment(id, userId);
            if (open != null)
            {
                NotifyError(string.Format(
                    "\"{0}\" đang tham gia dự án từ {1}. Hãy kết thúc giai đoạn đó trước khi phân công lại.",
                    person.FullName, open.JoinedAt.ToString("dd/MM/yyyy")));
                return RedirectToAction("Members", new { id = id });
            }

            var from = joinedAt ?? DateTime.Today;
            var replaced = isPm ? EndCurrentPm(id, from) : null;

            Repository.WorkAssignments.Insert(new WorkAssignment
            {
                ProjectId = id,
                ProjectName = project.Name,
                UserId = userId,
                UserFullName = person.FullName,
                Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim(),
                IsPm = isPm,
                Phase = string.IsNullOrWhiteSpace(phase) ? AssignmentPhases.Both : phase,
                JoinedAt = from,
                Note = note,
                CreatedAt = DateTime.Now
            });

            WorkService.SyncCurrentPm(id);

            // Báo cho người vừa được thêm (trừ khi tự thêm chính mình).
            if (userId != CurrentUserId) NotificationService.ProjectAdded(userId, project);

            Notify(replaced != null
                ? string.Format("Đã thêm \"{0}\" làm PM từ {1}; \"{2}\" thôi làm PM từ ngày đó.",
                    person.FullName, from.ToString("dd/MM/yyyy"), replaced.UserFullName)
                : string.Format("Đã thêm \"{0}\" vào dự án từ {1}{2}.",
                    person.FullName, from.ToString("dd/MM/yyyy"), isPm ? " với vai trò PM" : string.Empty));

            return Saved("Members", new { id = id });
        }

        /// <summary>Chuyển vai trò PM sang một người đang tham gia dự án.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wprojects.edit")]
        public ActionResult SetPm(int id, int assignmentId, DateTime? from)
        {
            if (!CanEditProject(id)) return HttpNotFound();

            var target = Repository.WorkAssignments.Find(assignmentId);
            if (target == null || target.ProjectId != id) return HttpNotFound();

            if (target.LeftAt.HasValue)
            {
                NotifyError("Người này đã rời dự án, không đặt làm PM được.");
                return RedirectToAction("Members", new { id = id });
            }

            var since = from ?? DateTime.Today;
            var replaced = EndCurrentPm(id, since, assignmentId);

            target.IsPm = true;
            Repository.WorkAssignments.Update(target);
            WorkService.SyncCurrentPm(id);

            Notify(replaced != null
                ? string.Format("\"{0}\" làm PM từ {1}, thay cho \"{2}\".",
                    target.UserFullName, since.ToString("dd/MM/yyyy"), replaced.UserFullName)
                : string.Format("\"{0}\" làm PM từ {1}.", target.UserFullName, since.ToString("dd/MM/yyyy")));

            return RedirectToAction("Members", new { id = id });
        }

        /// <summary>
        /// Kết thúc vai trò PM của người đang giữ.
        ///
        /// Không kết thúc luôn cả dòng tham gia — PM bàn giao vai trò nhưng thường vẫn ở lại dự án.
        /// Muốn họ rời hẳn thì bấm "Kết thúc" riêng.
        /// </summary>
        private WorkAssignment EndCurrentPm(int projectId, DateTime from, int excludeAssignmentId = 0)
        {
            var current = WorkService.CurrentPmAssignment(projectId);
            if (current == null || current.Id == excludeAssignmentId) return null;

            current.IsPm = false;
            Repository.WorkAssignments.Update(current);
            return current;
        }

        /// <summary>Form thêm một giai đoạn tham gia, mở trong hộp thoại.</summary>
        [HttpGet]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult AddMemberForm(int id)
        {
            if (!CanEditProject(id)) return HttpNotFound();

            var project = Repository.WorkProjects.Find(id);
            if (project == null) return HttpNotFound();

            ViewBag.UserOptions = WorkService.ActiveUsers();
            ViewBag.Roles = Repository.ActiveNames(Repository.MemberRoles);

            return PartialView("_AddMemberForm", project);
        }

        /// <summary>Form nhỏ chọn ngày rời, mở trong hộp thoại từ menu thao tác của từng dòng.</summary>
        [HttpGet]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult EndMemberForm(int id, int assignmentId)
        {
            if (!CanEditProject(id)) return HttpNotFound();

            var assignment = Repository.WorkAssignments.Find(assignmentId);
            if (assignment == null || assignment.ProjectId != id) return HttpNotFound();

            return PartialView("_EndMemberForm", assignment);
        }

        /// <summary>Kết thúc một giai đoạn tham gia. Muốn quay lại thì thêm giai đoạn mới.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult EndMember(int id, int assignmentId, DateTime? leftAt)
        {
            if (!CanEditProject(id)) return HttpNotFound();

            var assignment = Repository.WorkAssignments.Find(assignmentId);
            if (assignment == null || assignment.ProjectId != id) return HttpNotFound();

            var date = leftAt ?? DateTime.Today;
            if (date.Date < assignment.JoinedAt.Date)
            {
                if (Request.IsAjaxRequest())
                {
                    ModelState.AddModelError("leftAt", "Ngày rời dự án không được sớm hơn ngày tham gia.");
                    return PartialView("_EndMemberForm", assignment);
                }

                NotifyError("Ngày rời dự án không được sớm hơn ngày tham gia.");
                return RedirectToAction("Members", new { id = id });
            }

            assignment.LeftAt = date;
            Repository.WorkAssignments.Update(assignment);

            // Người vừa rời có thể đang là PM — phải tính lại, nếu không dự án sẽ vẫn coi họ là PM
            // và họ tiếp tục sửa được dự án dù đã bàn giao.
            WorkService.SyncCurrentPm(id);

            // Báo cho người vừa được rút khỏi dự án (trừ khi tự rút chính mình).
            if (assignment.UserId != CurrentUserId)
            {
                NotificationService.ProjectRemoved(assignment.UserId, assignment.ProjectName, id);
            }

            // Việc đang giao cho người vừa rút ra không tự chuyển sang ai — chỉ cảnh báo để PM biết
            // mà xử lý, chứ tự ý đổi người thực hiện sẽ làm sai điểm chất lượng của cả hai bên.
            var pending = Repository.WorkTasks.All().Count(t =>
                t.ProjectId == id && t.AssigneeUserId == assignment.UserId
                && !TaskStates.IsClosed(t.State));

            Notify(pending > 0
                ? string.Format("\"{0}\" đã rời dự án từ {1}. Còn {2} đầu việc đang mở giao cho người này — nhớ chuyển lại.",
                    assignment.UserFullName, date.ToString("dd/MM/yyyy"), pending)
                : string.Format("\"{0}\" đã rời dự án từ {1}.",
                    assignment.UserFullName, date.ToString("dd/MM/yyyy")));

            return Saved("Members", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "wtasks.view")]
        public ActionResult RemoveMember(int id, int assignmentId)
        {
            if (!CanEditProject(id)) return HttpNotFound();

            var assignment = Repository.WorkAssignments.Find(assignmentId);
            if (assignment == null || assignment.ProjectId != id) return HttpNotFound();

            // Xoá một giai đoạn ĐANG MỞ đồng nghĩa rút người đó khỏi dự án — báo cho họ biết.
            var wasActive = assignment.IsActive;

            Repository.WorkAssignments.Delete(assignmentId);
            WorkService.SyncCurrentPm(id);

            if (wasActive && assignment.UserId != CurrentUserId)
            {
                NotificationService.ProjectRemoved(assignment.UserId, assignment.ProjectName, id);
            }

            Notify(string.Format("Đã xoá giai đoạn tham gia của \"{0}\".", assignment.UserFullName));
            return RedirectToAction("Members", new { id = id });
        }

        private static bool Has(string source, string keyword)
        {
            return !string.IsNullOrEmpty(source)
                   && source.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
