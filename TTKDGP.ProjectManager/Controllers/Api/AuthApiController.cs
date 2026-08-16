using System;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Models.Api;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>
    /// Dang nhap cho mobile — tra ve mot ApiToken (Bearer) thay vi Forms Auth cookie. Kiem tai
    /// khoan/mat khau Y HET AccountController.Login (cung Repository.Users + PasswordHasher) de
    /// hai duong dang nhap khong bao gio lech ket qua nhau.
    /// </summary>
    public class AuthApiController : BaseController
    {
        // 1 nam — app mobile khong co man dang nhap lai tu dong, giu token song lau de nguoi
        // dung khong bi vang ra dang nhap giua chung thuong xuyen.
        private const int TokenLifetimeDays = 365;

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Login(string username, string password)
        {
            var user = Repository.Users.FirstOrDefault(u =>
                string.Equals(u.UserName, username, StringComparison.OrdinalIgnoreCase));

            // Khong tiet lo tai khoan co ton tai hay khong — dung 1 cau bao loi chung cho ca hai
            // truong hop, giong AccountController.Login.
            if (user == null || !user.IsActive || !PasswordHasher.Verify(password, user.PasswordHash))
            {
                Response.StatusCode = 400;
                Response.TrySkipIisCustomErrors = true;
                return Json(new { error = "Tai khoan hoac mat khau khong dung." });
            }

            var token = new ApiToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(TokenLifetimeDays)
            };
            Repository.ApiTokens.Insert(token);

            return Json(new LoginResultDto
            {
                Token = token.Token,
                DisplayName = user.FullName,
                Role = user.Role
            });
        }

        /// <summary>
        /// Buoc 1 cua "Quen mat khau": gui ma OTP 6 so qua SMS toi so dien thoai nhap vao. Luon
        /// tra ve thong bao GIONG NHAU du so do co gan tai khoan hay khong — khong tiet lo so dien
        /// thoai co ton tai trong he thong, giong nguyen tac cua Login o tren.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult RequestPasswordResetOtp(string phone)
        {
            var result = PasswordResetService.RequestOtp(phone);
            if (!result.Ok)
            {
                Response.StatusCode = 400;
                Response.TrySkipIisCustomErrors = true;
                return Json(new { error = result.Message });
            }

            return Json(new { ok = true, message = result.Message });
        }

        /// <summary>
        /// Buoc 2: xac thuc ma OTP, sinh mat khau moi va gui qua SMS toi cung so dien thoai. KHONG
        /// tra mat khau trong JSON — chi bao thanh cong, mat khau CHI den qua kenh SMS.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ResetPasswordWithOtp(string phone, string code)
        {
            var result = PasswordResetService.VerifyAndReset(phone, code);
            if (!result.Ok)
            {
                Response.StatusCode = 400;
                Response.TrySkipIisCustomErrors = true;
                return Json(new { error = result.Error });
            }

            return Json(new { ok = true });
        }
    }
}
