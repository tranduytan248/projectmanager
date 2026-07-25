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
    public class MembersController : BaseController
    {
        [AppAuthorize(Permission = "members.view")]
        public ActionResult Index(string keyword)
        {
            var members = Repository.Members.All();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                members = members
                    .Where(m => !string.IsNullOrEmpty(m.FullName)
                                && m.FullName.IndexOf(k, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    .ToList();
            }

            var assignments = Repository.Assignments.All().Where(a => a.IsActive).ToList();

            ViewBag.ProjectCounts = assignments
                .GroupBy(a => a.MemberId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.ProjectId).Distinct().Count());

            // Đếm dự án theo trạng thái của chính dự án, chỉ tính phân công còn hiệu lực.
            var statusByProject = Repository.Projects.All()
                .ToDictionary(p => p.Id, p => p.CurrentStatus ?? string.Empty);

            ViewBag.DoingCounts = CountByProjectStatus(assignments, statusByProject, "Đang thực hiện");
            ViewBag.SupportCounts = CountByProjectStatus(assignments, statusByProject, "Đang hỗ trợ");

            ViewBag.Keyword = keyword;

            return View(members.OrderBy(m => m.FullName, StringComparer.CurrentCulture).ToList());
        }

        /// <summary>Số dự án riêng biệt của mỗi thành viên đang ở một trạng thái dự án cụ thể.</summary>
        private static Dictionary<int, int> CountByProjectStatus(
            List<ProjectMember> assignments, Dictionary<int, string> statusByProject, string status)
        {
            return assignments
                .Where(a =>
                {
                    string s;
                    return statusByProject.TryGetValue(a.ProjectId, out s)
                           && string.Equals(s, status, StringComparison.CurrentCultureIgnoreCase);
                })
                .GroupBy(a => a.MemberId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.ProjectId).Distinct().Count());
        }

        [HttpGet]
        [AppAuthorize(Permission = "members.create,members.edit")]
        public ActionResult Edit(int? id)
        {
            var member = id.HasValue ? Repository.Members.Find(id.Value) : new Member { IsActive = true };
            if (member == null) return HttpNotFound();

            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "members.create,members.edit")]
        public ActionResult Edit(Member model)
        {
            if (!ModelState.IsValid) return View(model);

            var duplicate = Repository.Members.FirstOrDefault(m =>
                m.Id != model.Id &&
                string.Equals(m.FullName, model.FullName, StringComparison.CurrentCultureIgnoreCase));
            if (duplicate != null)
            {
                ModelState.AddModelError("FullName", "Đã có thành viên trùng họ tên.");
                return View(model);
            }

            // Email không bắt buộc, nhưng đã nhập thì mỗi người một địa chỉ riêng
            // để mail nhắc không gửi nhầm sang người khác.
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            if (model.Email != null)
            {
                var duplicateEmail = Repository.Members.FirstOrDefault(m =>
                    m.Id != model.Id &&
                    string.Equals(m.Email, model.Email, StringComparison.OrdinalIgnoreCase));
                if (duplicateEmail != null)
                {
                    ModelState.AddModelError("Email",
                        "Email này đang dùng cho thành viên \"" + duplicateEmail.FullName + "\".");
                    return View(model);
                }
            }

            if (model.Id == 0)
            {
                Repository.Members.Insert(model);
                Notify("Đã thêm thành viên \"" + model.FullName + "\".");
            }
            else
            {
                if (!Repository.Members.Update(model)) return HttpNotFound();

                Repository.SyncDenormalizedNames(null, model.Id);
                Notify("Đã cập nhật thành viên \"" + model.FullName + "\".");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "members.delete")]
        public ActionResult Delete(int id)
        {
            var member = Repository.Members.Find(id);
            if (member == null) return HttpNotFound();

            var assignmentCount = Repository.Assignments.All().Count(a => a.MemberId == id);
            if (assignmentCount > 0)
            {
                // Giữ lại lịch sử tham gia: yêu cầu gỡ phân công trước khi xoá thành viên.
                NotifyError(string.Format(
                    "Không thể xoá \"{0}\" vì còn {1} phân công. Hãy gỡ các phân công đó trước.",
                    member.FullName, assignmentCount));
                return RedirectToAction("Index");
            }

            Repository.Members.Delete(id);
            Notify("Đã xoá thành viên \"" + member.FullName + "\".");
            return RedirectToAction("Index");
        }
    }
}
