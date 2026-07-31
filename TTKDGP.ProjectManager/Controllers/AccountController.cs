using System;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    public class AccountController : BaseController
    {
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (CurrentUser != null) return HomeFor(CurrentUser.Role);

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        /// <summary>
        /// Màn hình mở đầu sau khi đăng nhập là "Dự án của tôi" — nơi ai cũng thấy dự án mình
        /// tham gia. Tài khoản không có quyền xem công việc (ví dụ Báo cáo công việc) không vào
        /// được màn đó nên đưa thẳng về màn báo cáo của họ.
        /// </summary>
        private ActionResult HomeFor(string role)
        {
            return Permissions.UserHas(role, Permissions.WorkTasks.Perm(Permissions.View))
                ? RedirectToAction("Projects", "MyWork")
                : RedirectToAction("Index", "MyReports");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = Repository.Users.FirstOrDefault(u =>
                string.Equals(u.UserName, model.UserName, StringComparison.OrdinalIgnoreCase));

            // Không tiết lộ tài khoản có tồn tại hay không.
            if (user == null || !user.IsActive || !PasswordHasher.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            AuthHelper.SignIn(HttpContext, user, model.RememberMe);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return HomeFor(user.Role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            AuthHelper.SignOut();
            return RedirectToAction("Index", "Home");
        }

        // Đặt tên method khác "Profile" vì Controller đã có sẵn thành viên trùng tên,
        // ActionName giữ URL vẫn là /Account/Profile.
        [HttpGet]
        [AppAuthorize]
        [ActionName("Profile")]
        public ActionResult ShowProfile()
        {
            var user = Repository.Users.Find(CurrentUser.UserId);
            if (user == null) return HttpNotFound();

            return View(BuildProfilePage(user));
        }

        /// <summary>Người dùng tự sửa thông tin cá nhân của mình.</summary>
        [HttpPost]
        [AppAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile([Bind(Prefix = "Profile")] ProfileViewModel model)
        {
            var user = Repository.Users.Find(CurrentUser.UserId);
            if (user == null) return HttpNotFound();

            if (!ModelState.IsValid)
            {
                var page = BuildProfilePage(user);
                page.Profile = model;
                return View("Profile", page);
            }

            user.FullName = model.FullName.Trim();
            Repository.Users.Update(user);

            // Họ tên hiển thị được đọc lại từ dữ liệu mỗi request nên không cần cấp lại vé đăng nhập.
            Notify("Đã cập nhật thông tin cá nhân.");
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [AppAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordViewModel model)
        {
            var user = Repository.Users.Find(CurrentUser.UserId);
            if (user == null) return HttpNotFound();

            if (ModelState.IsValid && !PasswordHasher.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("ChangePassword.CurrentPassword", "Mật khẩu hiện tại không đúng.");
            }

            if (!ModelState.IsValid)
            {
                var page = BuildProfilePage(user);
                page.ChangePassword = model;
                return View("Profile", page);
            }

            user.PasswordHash = PasswordHasher.Hash(model.NewPassword);
            Repository.Users.Update(user);

            Notify("Đã đổi mật khẩu thành công.");
            return RedirectToAction("Profile");
        }

        private static ProfilePageViewModel BuildProfilePage(User user)
        {
            return new ProfilePageViewModel
            {
                UserName = user.UserName,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                Profile = new ProfileViewModel { FullName = user.FullName },
                ChangePassword = new ChangePasswordViewModel()
            };
        }
    }
}
