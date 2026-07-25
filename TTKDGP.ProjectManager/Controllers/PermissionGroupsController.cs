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
    /// Cấu hình nhóm quyền: mỗi nhóm nắm một tập chức năng (module.action). Menu và quyền
    /// truy cập của tài khoản suy ra từ nhóm. Chỉ nhóm có chức năng "roles" mới vào được đây.
    /// </summary>
    [AppAuthorize]
    public class PermissionGroupsController : BaseController
    {
        [AppAuthorize(Permission = "roles.view")]
        public ActionResult Index()
        {
            var groups = Repository.RoleGroups.All()
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name, StringComparer.CurrentCulture)
                .ToList();

            ViewBag.UserCounts = UserCountsByCode();
            return View(groups);
        }

        [HttpGet]
        [AppAuthorize(Permission = "roles.view")]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
            {
                return View(new RoleGroupEditViewModel());
            }

            var group = Repository.RoleGroups.Find(id.Value);
            if (group == null) return HttpNotFound();

            var isAll = string.Equals((group.Permissions ?? string.Empty).Trim(), "*", StringComparison.Ordinal);

            return View(new RoleGroupEditViewModel
            {
                Id = group.Id,
                Code = group.Code,
                Name = group.Name,
                Description = group.Description,
                IsActive = group.IsActive,
                GrantAll = isAll,
                SelectedPermissions = isAll ? new string[0] : SplitPermissions(group.Permissions),
                UserCount = CountUsers(group.Code)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "roles.edit")]
        public ActionResult Edit(RoleGroupEditViewModel model)
        {
            var code = (model.Code ?? string.Empty).Trim();

            // Mã nhóm không được trùng.
            var duplicate = Repository.RoleGroups.FirstOrDefault(g =>
                g.Id != model.Id && string.Equals(g.Code, code, StringComparison.OrdinalIgnoreCase));
            if (duplicate != null)
            {
                ModelState.AddModelError("Code", "Mã nhóm đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                model.UserCount = CountUsers(code);
                return View(model);
            }

            // Chỉ giữ các mã chức năng hợp lệ (bỏ mã lạ do sửa tay), trừ khi cấp toàn quyền.
            var permissions = model.GrantAll
                ? "*"
                : string.Join(",", (model.SelectedPermissions ?? new string[0])
                    .Select(p => (p ?? string.Empty).Trim())
                    .Where(p => p.Length > 0 && ValidCodes().Contains(p))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            if (model.IsNew)
            {
                Repository.RoleGroups.Insert(new RoleGroup
                {
                    Code = code,
                    Name = model.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                    Permissions = permissions,
                    IsActive = model.IsActive,
                    SortOrder = NextSortOrder()
                });
                Notify("Đã tạo nhóm quyền \"" + model.Name + "\".");
                return RedirectToAction("Index");
            }

            var group = Repository.RoleGroups.Find(model.Id);
            if (group == null) return HttpNotFound();

            // Tự bảo vệ: người đang đăng nhập không được tự cắt quyền vào màn cấu hình nhóm
            // của chính nhóm mình đang dùng, tránh khoá mình ra ngoài.
            var editingOwnGroup = CurrentUser != null
                && Models.Roles.Has(CurrentUser.Role, group.Code);
            var stillHasRoles = permissions == "*"
                || SplitPermissions(permissions).Contains(Permissions.Roles.Perm(Permissions.Edit),
                    StringComparer.OrdinalIgnoreCase);
            if (editingOwnGroup && !stillHasRoles)
            {
                ModelState.AddModelError("", "Không thể tự bỏ quyền cấu hình nhóm quyền của nhóm bạn đang dùng.");
                model.UserCount = CountUsers(code);
                return View(model);
            }

            // Giữ nguyên Code khi sửa (tài khoản đang tham chiếu tới), chỉ đổi phần còn lại.
            group.Name = model.Name.Trim();
            group.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            group.Permissions = permissions;
            group.IsActive = model.IsActive;

            Repository.RoleGroups.Update(group);
            Notify("Đã cập nhật nhóm quyền \"" + group.Name + "\".");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "roles.delete")]
        public ActionResult Delete(int id)
        {
            var group = Repository.RoleGroups.Find(id);
            if (group == null) return HttpNotFound();

            // Không xoá nhóm còn tài khoản đang dùng, tránh biến họ thành không nhóm.
            var users = CountUsers(group.Code);
            if (users > 0)
            {
                NotifyError("Không xoá được: còn " + users + " tài khoản đang dùng nhóm này. "
                            + "Hãy chuyển các tài khoản đó sang nhóm khác trước.");
                return RedirectToAction("Index");
            }

            Repository.RoleGroups.Delete(id);
            Notify("Đã xoá nhóm quyền \"" + group.Name + "\".");
            return RedirectToAction("Index");
        }

        // ---------- Trợ giúp ----------

        private static string[] SplitPermissions(string permissions)
        {
            if (string.IsNullOrWhiteSpace(permissions)) return new string[0];
            return permissions.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();
        }

        private static HashSet<string> ValidCodes()
        {
            return new HashSet<string>(Permissions.AllCodes(), StringComparer.OrdinalIgnoreCase);
        }

        private static int CountUsers(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return 0;
            return Repository.Users.All().Count(u => Models.Roles.Has(u.Role, code));
        }

        private static Dictionary<string, int> UserCountsByCode()
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var u in Repository.Users.All())
            {
                foreach (var code in Models.Roles.Split(u.Role))
                {
                    counts[code] = counts.ContainsKey(code) ? counts[code] + 1 : 1;
                }
            }
            return counts;
        }

        private static int NextSortOrder()
        {
            var groups = Repository.RoleGroups.All();
            return groups.Any() ? groups.Max(g => g.SortOrder) + 1 : 1;
        }
    }
}
