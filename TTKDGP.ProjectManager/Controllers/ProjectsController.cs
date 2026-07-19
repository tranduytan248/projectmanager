using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    [AppAuthorize]
    public class ProjectsController : BaseController
    {
        public ActionResult Index(string keyword, string status, string projectType, int? pmMemberId)
        {
            var projects = Repository.Projects.All();

            // Danh sách PM lấy từ toàn bộ dự án trước khi lọc, để chọn một PM không làm mất
            // các lựa chọn còn lại trong dropdown.
            var allMembers = Repository.Members.All().ToDictionary(m => m.Id, m => m.FullName);
            var pmOptions = projects
                .Select(p => p.PmMemberId)
                .Where(id => id > 0 && allMembers.ContainsKey(id))
                .Distinct()
                .Select(id => new Member { Id = id, FullName = allMembers[id] })
                .OrderBy(m => m.FullName, StringComparer.CurrentCulture)
                .ToList();

            if (pmMemberId.HasValue && pmMemberId.Value > 0)
            {
                projects = projects.Where(p => p.PmMemberId == pmMemberId.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                projects = projects
                    .Where(p => string.Equals(p.CurrentStatus, status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(projectType))
            {
                projects = projects
                    .Where(p => string.Equals(p.ProjectType, projectType, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
            }

            var memberNames = allMembers;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                projects = projects.Where(p =>
                    Has(p.Name, k) || Has(p.Customer, k) || Has(p.ProjectType, k) ||
                    Has(NameOf(memberNames, p.PmMemberId), k) ||
                    p.ParticipantIds.Any(id => Has(NameOf(memberNames, id), k))).ToList();
            }

            // Số thành viên đang tham gia mỗi dự án, để hiển thị ở cột "Thành viên".
            var activeCounts = Repository.Assignments.All()
                .Where(a => a.IsActive)
                .GroupBy(a => a.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.MemberId).Distinct().Count());

            ViewBag.MemberNames = memberNames;
            ViewBag.ActiveCounts = activeCounts;
            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.ProjectType = projectType;
            ViewBag.ProjectTypes = Repository.ActiveNames(Repository.ProjectTypes);
            ViewBag.Statuses = Repository.ActiveNames(Repository.ProjectStatuses);
            ViewBag.PmMemberId = pmMemberId;
            ViewBag.PmOptions = pmOptions;

            return View(projects.OrderBy(p => p.Name, StringComparer.CurrentCulture).ToList());
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            var project = id.HasValue ? Repository.Projects.Find(id.Value) : new Project { CurrentStatus = "Đang thực hiện" };
            if (project == null) return HttpNotFound();

            PopulateLists(project);
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Project model)
        {
            // Không chọn ai thì trình duyệt không gửi trường nào cả.
            if (model.ParticipantIds == null) model.ParticipantIds = new List<int>();

            var validIds = new HashSet<int>(Repository.Members.All().Select(m => m.Id));

            if (model.PmMemberId > 0 && !validIds.Contains(model.PmMemberId))
            {
                ModelState.AddModelError("PmMemberId", "PM phụ trách không tồn tại.");
            }

            // Bỏ id lạ và id trùng, giữ đúng thứ tự người dùng chọn.
            model.ParticipantIds = model.ParticipantIds
                .Where(validIds.Contains)
                .Distinct()
                .ToList();

            if (!ModelState.IsValid)
            {
                PopulateLists(model);
                return View(model);
            }

            if (model.Id == 0)
            {
                Repository.Projects.Insert(model);
                Notify("Đã thêm dự án \"" + model.Name + "\".");
            }
            else
            {
                if (!Repository.Projects.Update(model)) return HttpNotFound();

                // Tên dự án đã lưu kèm trong phân công phải khớp lại sau khi đổi tên.
                Repository.SyncDenormalizedNames(model.Id, null);
                Notify("Đã cập nhật dự án \"" + model.Name + "\".");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var project = Repository.Projects.Find(id);
            if (project == null) return HttpNotFound();

            // Xoá luôn phân công và nhật ký thuộc dự án, tránh để lại dữ liệu mồ côi.
            var removedLogs = Repository.WorkLogs.DeleteWhere(w => w.ProjectId == id);
            var removedAssignments = Repository.Assignments.DeleteWhere(a => a.ProjectId == id);
            Repository.Projects.Delete(id);

            Notify(removedAssignments > 0
                ? string.Format("Đã xoá dự án \"{0}\", {1} phân công và {2} tuần nhật ký liên quan.",
                    project.Name, removedAssignments, removedLogs)
                : string.Format("Đã xoá dự án \"{0}\".", project.Name));

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Đổ danh sách loại dự án và trạng thái cho dropdown. Giá trị hiện tại của dự án luôn được
        /// giữ trong danh sách kể cả khi đã bị ẩn khỏi danh mục, tránh mở ra sửa lại làm mất giá trị cũ.
        /// </summary>
        private void PopulateLists(Project project)
        {
            ViewBag.ProjectTypes = Repository.NamesForEdit(Repository.ProjectTypes, project.ProjectType);
            ViewBag.ProjectStatuses = Repository.NamesForEdit(Repository.ProjectStatuses, project.CurrentStatus);

            // Giữ cả thành viên đã nghỉ, nếu không thì dự án cũ do họ phụ trách sẽ mất lựa chọn khi mở ra sửa.
            ViewBag.Members = Repository.Members.All()
                .OrderBy(m => m.FullName, StringComparer.CurrentCulture)
                .ToList();
        }

        private static string NameOf(Dictionary<int, string> names, int id)
        {
            string n;
            return id > 0 && names.TryGetValue(id, out n) ? n : null;
        }

        private static bool Has(string source, string keyword)
        {
            return !string.IsNullOrEmpty(source)
                   && source.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
