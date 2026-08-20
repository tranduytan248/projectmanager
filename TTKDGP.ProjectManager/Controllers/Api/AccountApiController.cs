using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Models.Api;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>
    /// Man "Thong tin ca nhan" cua mobile — tuong duong AccountController.ShowProfile/
    /// UpdateProfile/ChangePassword ben web, nhung tach rieng khoi AuthApiController (controller
    /// do chi lo dang nhap, khong can [ApiAuthorize]).
    /// </summary>
    [ApiAuthorize]
    public class AccountApiController : BaseController
    {
        [HttpGet]
        public ActionResult Me()
        {
            var user = Repository.Users.Find(CurrentUserId);
            if (user == null) return HttpNotFound();

            return Json(ToDto(user), JsonRequestBehavior.AllowGet);
        }

        /// <summary>Y het AccountController.UpdateProfile: chi sua duoc Ho ten.</summary>
        [HttpPost]
        public ActionResult UpdateProfile(string fullName)
        {
            var user = Repository.Users.Find(CurrentUserId);
            if (user == null) return HttpNotFound();

            var trimmed = (fullName ?? string.Empty).Trim();
            if (trimmed.Length == 0) return BadRequest("Vui lòng nhập họ tên.");
            if (trimmed.Length > 150) return BadRequest("Họ tên tối đa 150 ký tự.");

            user.FullName = trimmed;
            Repository.Users.Update(user);

            return Json(ToDto(user));
        }

        /// <summary>Y het AccountController.ChangePassword: phai dung mat khau hien tai, mat khau
        /// moi toi thieu 6 ky tu va khop voi xac nhan.</summary>
        [HttpPost]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = Repository.Users.Find(CurrentUserId);
            if (user == null) return HttpNotFound();

            if (string.IsNullOrEmpty(currentPassword)) return BadRequest("Vui lòng nhập mật khẩu hiện tại.");
            if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
            {
                return BadRequest("Mật khẩu hiện tại không đúng.");
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                return BadRequest("Mật khẩu mới tối thiểu 6 ký tự.");
            }
            if (newPassword != confirmPassword) return BadRequest("Xác nhận mật khẩu không khớp.");

            user.PasswordHash = PasswordHasher.Hash(newPassword);
            Repository.Users.Update(user);

            return Json(new { ok = true });
        }

        private static ProfileDto ToDto(User user)
        {
            return new ProfileDto
            {
                UserName = user.UserName,
                FullName = user.FullName,
                RoleDisplay = Roles.Display(user.Role),
                IsTeamManager = user.IsTeamManager,
                IsAdmin = user.IsAdmin,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
