using System;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Quản lý việc "thành viên nào làm công việc gì trong dự án nào, trạng thái tham gia ra sao".
    /// Đây là nguồn dữ liệu cho màn hình tổng hợp ngoài FrontEnd.
    /// </summary>
    [AppAuthorize(AllowedRoles = Roles.Management)]
    public class AssignmentsController : BaseController
    {
        public ActionResult Index(string keyword, int? memberId, int? projectId)
        {
            var rows = Repository.BuildSummaryRows();

            if (memberId.HasValue && memberId.Value > 0)
            {
                rows = rows.Where(r => r.MemberId == memberId.Value).ToList();
            }

            if (projectId.HasValue && projectId.Value > 0)
            {
                rows = rows.Where(r => r.ProjectId == projectId.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                rows = rows.Where(r =>
                    Has(r.MemberName, k) || Has(r.ProjectName, k) ||
                    Has(r.Role, k) || Has(r.WorkContent, k)).ToList();
            }

            var model = new AssignmentListViewModel
            {
                Rows = rows
                    .OrderBy(r => r.MemberName, StringComparer.CurrentCulture)
                    .ThenBy(r => r.ProjectName, StringComparer.CurrentCulture)
                    .ToList(),
                Members = Repository.Members.All()
                    .OrderBy(m => m.FullName, StringComparer.CurrentCulture).ToList(),
                Projects = Repository.Projects.All()
                    .OrderBy(p => p.Name, StringComparer.CurrentCulture).ToList(),
                Keyword = keyword,
                MemberId = memberId,
                ProjectId = projectId
            };

            ViewBag.LogCounts = Repository.LogCountByAssignment();
            return View(model);
        }

        [HttpGet]
        public ActionResult Edit(int? id, int? projectId)
        {
            var assignment = id.HasValue
                ? Repository.Assignments.Find(id.Value)
                : new ProjectMember
                {
                    IsActive = true,
                    // Thêm phân công từ trong một dự án thì chọn sẵn dự án đó.
                    ProjectId = projectId.HasValue ? projectId.Value : 0,
                    WorkStatus = Repository.ActiveNames(Repository.WorkStatuses).FirstOrDefault(),
                    Role = Repository.ActiveNames(Repository.MemberRoles).FirstOrDefault()
                };

            if (assignment == null) return HttpNotFound();

            PopulateLists(assignment);
            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProjectMember model)
        {
            var project = Repository.Projects.Find(model.ProjectId);
            var member = Repository.Members.Find(model.MemberId);

            if (project == null) ModelState.AddModelError("ProjectId", "Dự án không tồn tại.");
            if (member == null) ModelState.AddModelError("MemberId", "Thành viên không tồn tại.");

            // Một thành viên chỉ nên có một dòng phân công cho mỗi dự án.
            if (project != null && member != null)
            {
                var duplicate = Repository.Assignments.FirstOrDefault(a =>
                    a.Id != model.Id && a.ProjectId == model.ProjectId && a.MemberId == model.MemberId);
                if (duplicate != null)
                {
                    ModelState.AddModelError("",
                        string.Format("\"{0}\" đã được phân công vào dự án \"{1}\". Hãy sửa dòng đó thay vì tạo mới.",
                            member.FullName, project.Name));
                }
            }

            if (!ModelState.IsValid)
            {
                PopulateLists(model);
                return View(model);
            }

            // Lưu kèm tên để dữ liệu vẫn đọc được nếu dự án/thành viên bị xoá về sau.
            model.ProjectName = project.Name;
            model.MemberName = member.FullName;

            if (model.Id == 0)
            {
                Repository.Assignments.Insert(model);
                Notify(string.Format("Đã phân công \"{0}\" vào dự án \"{1}\".", member.FullName, project.Name));
            }
            else
            {
                if (!Repository.Assignments.Update(model)) return HttpNotFound();
                Notify("Đã cập nhật phân công.");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var assignment = Repository.Assignments.Find(id);
            if (assignment == null) return HttpNotFound();

            // Xoá luôn nhật ký của phân công, tránh để lại dữ liệu mồ côi.
            var removedLogs = Repository.WorkLogs.DeleteWhere(w => w.AssignmentId == id);
            Repository.Assignments.Delete(id);

            Notify(removedLogs > 0
                ? string.Format("Đã gỡ \"{0}\" khỏi dự án \"{1}\" và xoá {2} tuần nhật ký.",
                    assignment.MemberName, assignment.ProjectName, removedLogs)
                : string.Format("Đã gỡ \"{0}\" khỏi dự án \"{1}\".",
                    assignment.MemberName, assignment.ProjectName));
            return RedirectToAction("Index");
        }

        /// <summary>Bật/tắt nhanh trạng thái tham gia ngay trên danh sách.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleActive(int id)
        {
            var assignment = Repository.Assignments.Find(id);
            if (assignment == null) return HttpNotFound();

            assignment.IsActive = !assignment.IsActive;
            Repository.Assignments.Update(assignment);

            Notify(string.Format("\"{0}\" — {1} dự án \"{2}\".",
                assignment.MemberName,
                assignment.IsActive ? "đang tham gia" : "đã ngừng tham gia",
                assignment.ProjectName));

            return RedirectToAction("Index");
        }

        private void PopulateLists(ProjectMember assignment)
        {
            var currentRole = assignment == null ? null : assignment.Role;
            var currentWorkStatus = assignment == null ? null : assignment.WorkStatus;

            ViewBag.Projects = Repository.Projects.All()
                .OrderBy(p => p.Name, StringComparer.CurrentCulture).ToList();
            // Giữ cả thành viên đã nghỉ, nếu không thì phân công cũ của họ sẽ mất lựa chọn khi mở ra sửa.
            ViewBag.Members = Repository.Members.All()
                .OrderBy(m => m.FullName, StringComparer.CurrentCulture).ToList();
            ViewBag.Roles = Repository.NamesForEdit(Repository.MemberRoles, currentRole);
            ViewBag.WorkStatuses = Repository.NamesForEdit(Repository.WorkStatuses, currentWorkStatus);
        }

        private static bool Has(string source, string keyword)
        {
            return !string.IsNullOrEmpty(source)
                   && source.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
