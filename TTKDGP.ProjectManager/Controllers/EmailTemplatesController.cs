using System;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Quản trị mẫu email của thông báo cá nhân. Dùng chung nhóm quyền với màn Thông báo:
    /// notifications.view để xem, notifications.send để sửa mẫu và gửi thử.
    /// </summary>
    [AppAuthorize]
    public class EmailTemplatesController : BaseController
    {
        [AppAuthorize(Permission = "notifications.view")]
        public ActionResult Index()
        {
            // Tạo sẵn các dòng còn thiếu để danh sách luôn đủ bốn mẫu.
            EmailTemplateService.EnsureDefaults();

            var templates = EmailTemplateService.Definitions
                .Select(d => EmailTemplateService.Get(d.Code))
                .Where(t => t != null)
                .ToList();

            ViewBag.EmailReady = AppSettings.Email.IsConfigured;
            return View(templates);
        }

        [HttpGet]
        [AppAuthorize(Permission = "notifications.send")]
        public ActionResult Edit(string code)
        {
            EmailTemplateService.EnsureDefaults();

            var definition = EmailTemplateService.Definition(code);
            var template = EmailTemplateService.Get(code);
            if (definition == null || template == null) return HttpNotFound();

            ViewBag.Definition = definition;
            return View(template);
        }

        // Thân mail là HTML từ trình soạn thảo nên phải tắt request validation cho action này;
        // nội dung chỉ người có quyền quản trị thông báo mới sửa được.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [AppAuthorize(Permission = "notifications.send")]
        public ActionResult Edit(EmailTemplate model)
        {
            var definition = EmailTemplateService.Definition(model.Code);
            if (definition == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(model.Subject))
            {
                ModelState.AddModelError("Subject", "Vui lòng nhập tiêu đề mail.");
            }

            if (string.IsNullOrWhiteSpace(model.Body))
            {
                ModelState.AddModelError("Body", "Nội dung mail không được để trống.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Definition = definition;
                return View(model);
            }

            EmailTemplateService.EnsureDefaults();
            var current = Repository.EmailTemplates.FirstOrDefault(
                t => string.Equals(t.Code, model.Code, StringComparison.OrdinalIgnoreCase));
            if (current == null) return HttpNotFound();

            current.Subject = model.Subject.Trim();
            current.Body = model.Body;
            current.IsActive = model.IsActive;
            current.UpdatedAt = DateTime.Now;
            current.UpdatedBy = CurrentUser == null ? null : CurrentUser.FullName;
            Repository.EmailTemplates.Update(current);

            Notify(string.Format("Đã lưu mẫu \"{0}\".", current.Name));
            return RedirectToAction("Index");
        }

        /// <summary>Gửi mẫu ĐÃ LƯU với dữ liệu minh hoạ tới email của chính người bấm.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "notifications.send")]
        public ActionResult SendTest(string code)
        {
            var user = CurrentUser == null ? null : Repository.Users.Find(CurrentUser.UserId);

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                NotifyError("Tài khoản của bạn chưa khai email — vào Người dùng bổ sung email rồi gửi thử lại.");
                return RedirectToAction("Edit", new { code = code });
            }

            var result = EmailTemplateService.SendSample(code, user.Email.Trim(), user.FullName);

            if (result.Ok) Notify(string.Format("Đã gửi mail thử tới {0}.", user.Email.Trim()));
            else NotifyError("Không gửi được mail thử: " + result.Error);

            return RedirectToAction("Edit", new { code = code });
        }

        /// <summary>Đưa mẫu về nội dung mặc định của hệ thống.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "notifications.send")]
        public ActionResult Reset(string code)
        {
            var definition = EmailTemplateService.Definition(code);
            if (definition == null) return HttpNotFound();

            EmailTemplateService.EnsureDefaults();
            var current = Repository.EmailTemplates.FirstOrDefault(
                t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
            if (current == null) return HttpNotFound();

            current.Subject = definition.DefaultSubject;
            current.Body = definition.DefaultBody;
            current.UpdatedAt = DateTime.Now;
            current.UpdatedBy = CurrentUser == null ? null : CurrentUser.FullName;
            Repository.EmailTemplates.Update(current);

            Notify(string.Format("Đã đưa mẫu \"{0}\" về nội dung mặc định.", current.Name));
            return RedirectToAction("Edit", new { code = code });
        }
    }
}
