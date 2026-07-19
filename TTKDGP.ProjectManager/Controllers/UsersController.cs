using System;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>Quản trị tài khoản đăng nhập. Chỉ Admin dùng được.</summary>
    [AppAuthorize(RequiredRole = Roles.Admin)]
    public class UsersController : BaseController
    {
        public ActionResult Index()
        {
            var users = Repository.Users.All()
                .OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return View(users);
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
            {
                return View(new UserEditViewModel { Role = Roles.Manager, IsActive = true });
            }

            var user = Repository.Users.Find(id.Value);
            if (user == null) return HttpNotFound();

            return View(new UserEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserEditViewModel model)
        {
            // Tạo mới thì bắt buộc có mật khẩu; sửa thì để trống nghĩa là giữ nguyên mật khẩu cũ.
            if (model.Id == 0 && string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu cho tài khoản mới.");
            }

            if (!Roles.All.Contains(model.Role))
            {
                ModelState.AddModelError("Role", "Phân quyền không hợp lệ.");
            }

            var duplicate = Repository.Users.FirstOrDefault(u =>
                u.Id != model.Id &&
                string.Equals(u.UserName, model.UserName, StringComparison.OrdinalIgnoreCase));
            if (duplicate != null)
            {
                ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại.");
            }

            if (!ModelState.IsValid) return View(model);

            if (model.Id == 0)
            {
                Repository.Users.Insert(new User
                {
                    UserName = model.UserName.Trim(),
                    FullName = model.FullName.Trim(),
                    Role = model.Role,
                    PasswordHash = PasswordHasher.Hash(model.Password),
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                });
                Notify("Đã tạo tài khoản \"" + model.UserName + "\".");
                return RedirectToAction("Index");
            }

            var user = Repository.Users.Find(model.Id);
            if (user == null) return HttpNotFound();

            // Không cho Admin tự hạ quyền hoặc tự khoá mình, tránh mất lối vào phần quản trị.
            if (user.Id == CurrentUser.UserId)
            {
                if (!string.Equals(model.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("Role", "Bạn không thể tự bỏ quyền Admin của chính mình.");
                }
                if (!model.IsActive)
                {
                    ModelState.AddModelError("IsActive", "Bạn không thể tự khoá tài khoản của chính mình.");
                }
                if (!ModelState.IsValid) return View(model);
            }

            user.UserName = model.UserName.Trim();
            user.FullName = model.FullName.Trim();
            user.Role = model.Role;
            user.IsActive = model.IsActive;
            if (!string.IsNullOrEmpty(model.Password))
            {
                user.PasswordHash = PasswordHasher.Hash(model.Password);
            }

            Repository.Users.Update(user);
            Notify("Đã cập nhật tài khoản \"" + user.UserName + "\".");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var user = Repository.Users.Find(id);
            if (user == null) return HttpNotFound();

            if (user.Id == CurrentUser.UserId)
            {
                NotifyError("Bạn không thể xoá tài khoản đang đăng nhập.");
                return RedirectToAction("Index");
            }

            // Luôn phải còn ít nhất một Admin đang hoạt động.
            if (user.IsAdmin)
            {
                var otherActiveAdmins = Repository.Users.All()
                    .Count(u => u.Id != id && u.IsActive && u.IsAdmin);
                if (otherActiveAdmins == 0)
                {
                    NotifyError("Không thể xoá Admin cuối cùng của hệ thống.");
                    return RedirectToAction("Index");
                }
            }

            Repository.Users.Delete(id);
            Notify("Đã xoá tài khoản \"" + user.UserName + "\".");
            return RedirectToAction("Index");
        }
    }
}
