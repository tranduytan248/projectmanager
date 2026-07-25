using System;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Quản trị các hệ thống bên ngoài được phép tích hợp: mã, tên, khoá bí mật (sinh ngẫu nhiên,
    /// dùng để ký checksum) và trạng thái hoạt động. Chỉ Admin dùng vì khoá là thông tin nhạy cảm.
    /// </summary>
    [AppAuthorize]
    public class IntegrationSystemsController : BaseController
    {
        [AppAuthorize(Permission = "integrations.view")]
        public ActionResult Index(string keyword)
        {
            var items = Repository.IntegrationSystems.All();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                items = items.Where(x =>
                    Has(x.Code, k) || Has(x.Name, k)).ToList();
            }

            ViewBag.Keyword = keyword;

            return View(items
                .OrderBy(x => x.Code, StringComparer.CurrentCulture)
                .ToList());
        }

        [HttpGet]
        [AppAuthorize(Permission = "integrations.create,integrations.edit")]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
            {
                // Khoá sinh sẵn ở màn thêm mới, đưa vào ô ẩn để hiện cho người dùng và lưu khi bấm Thêm.
                return View(new IntegrationSystem
                {
                    IsActive = true,
                    PrivateKey = IntegrationKey.Generate()
                });
            }

            var item = Repository.IntegrationSystems.Find(id.Value);
            if (item == null) return HttpNotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "integrations.create,integrations.edit")]
        public ActionResult Edit(IntegrationSystem model)
        {
            var code = (model.Code ?? string.Empty).Trim();

            // Mã là khoá nghiệp vụ, không cho trùng (bỏ qua chính bản ghi đang sửa).
            var duplicate = Repository.IntegrationSystems.FirstOrDefault(x =>
                x.Id != model.Id &&
                string.Equals(x.Code, code, StringComparison.CurrentCultureIgnoreCase));
            if (duplicate != null)
            {
                ModelState.AddModelError("Code", "Đã có hệ thống khác dùng mã này.");
            }

            if (!ModelState.IsValid) return View(model);

            var now = DateTime.Now;

            if (model.Id == 0)
            {
                // Khoá lấy từ ô ẩn nếu hợp lệ, không thì sinh mới — luôn có khoá khi tạo.
                var key = IntegrationKey.LooksValid(model.PrivateKey)
                    ? model.PrivateKey
                    : IntegrationKey.Generate();

                Repository.IntegrationSystems.Insert(new IntegrationSystem
                {
                    Code = code,
                    Name = model.Name.Trim(),
                    PrivateKey = key,
                    IsActive = model.IsActive,
                    CreatedAt = now,
                    UpdatedAt = now
                });

                Notify(string.Format("Đã thêm hệ thống tích hợp \"{0}\".", code));
                return RedirectToAction("Index");
            }

            var current = Repository.IntegrationSystems.Find(model.Id);
            if (current == null) return HttpNotFound();

            // KHÔNG lấy khoá từ form khi sửa: giữ nguyên khoá đang có, muốn đổi thì bấm "Tạo lại khoá".
            current.Code = code;
            current.Name = model.Name.Trim();
            current.IsActive = model.IsActive;
            current.UpdatedAt = now;
            Repository.IntegrationSystems.Update(current);

            Notify(string.Format("Đã cập nhật hệ thống tích hợp \"{0}\".", code));
            return RedirectToAction("Index");
        }

        /// <summary>Sinh lại khoá bí mật cho một hệ thống. Khoá cũ hết hiệu lực ngay sau khi lưu.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "integrations.edit")]
        public ActionResult Regenerate(int id)
        {
            var item = Repository.IntegrationSystems.Find(id);
            if (item == null) return HttpNotFound();

            item.PrivateKey = IntegrationKey.Generate();
            item.UpdatedAt = DateTime.Now;
            Repository.IntegrationSystems.Update(item);

            Notify(string.Format(
                "Đã tạo lại khoá bí mật cho \"{0}\". Nhớ cấp khoá mới cho hệ thống đối tác; khoá cũ không còn dùng được.",
                item.Code));
            return RedirectToAction("Edit", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "integrations.delete")]
        public ActionResult Delete(int id)
        {
            var item = Repository.IntegrationSystems.Find(id);
            if (item == null) return HttpNotFound();

            Repository.IntegrationSystems.Delete(id);
            Notify(string.Format("Đã xoá hệ thống tích hợp \"{0}\".", item.Code));
            return RedirectToAction("Index");
        }

        private static bool Has(string source, string keyword)
        {
            return !string.IsNullOrEmpty(source)
                   && source.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
