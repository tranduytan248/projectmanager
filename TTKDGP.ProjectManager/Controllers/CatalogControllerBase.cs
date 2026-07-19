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
    /// Khung dùng chung cho các màn hình danh mục (loại dự án, trạng thái dự án,
    /// trạng thái tham gia, vai trò). Lớp con chỉ cần khai báo kho dữ liệu, cách hiển thị,
    /// nơi giá trị được sử dụng và cách đổi tên hàng loạt.
    /// </summary>
    [AppAuthorize]
    public abstract class CatalogControllerBase<T> : BaseController where T : CatalogItem, new()
    {
        protected abstract JsonStore<T> Store { get; }
        protected abstract CatalogConfig Config { get; }

        /// <summary>Số bản ghi nghiệp vụ đang dùng mỗi giá trị, khoá theo tên giá trị.</summary>
        protected abstract Dictionary<string, int> Usage();

        /// <summary>Đổi tên giá trị trên mọi bản ghi đang dùng. Trả về số bản ghi đã đổi.</summary>
        protected abstract int RenameUsage(string oldName, string newName);

        public ActionResult Index()
        {
            var model = new CatalogListViewModel
            {
                Config = Config,
                Usage = Usage(),
                Items = Store.All()
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name, StringComparer.CurrentCulture)
                    .Cast<CatalogItem>()
                    .ToList()
            };

            return View("CatalogIndex", model);
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
            {
                var existing = Store.All();
                return View("CatalogEdit", new CatalogEditViewModel
                {
                    Config = Config,
                    IsActive = true,
                    SortOrder = existing.Count == 0 ? 10 : existing.Max(x => x.SortOrder) + 10,
                    Priority = existing.Count == 0 ? 1 : existing.Max(x => x.Priority) + 1
                });
            }

            var item = Store.Find(id.Value);
            if (item == null) return HttpNotFound();

            return View("CatalogEdit", new CatalogEditViewModel
            {
                Config = Config,
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Priority = item.Priority,
                IsClosed = item.IsClosed,
                IsActive = item.IsActive,
                SortOrder = item.SortOrder
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CatalogEditViewModel model)
        {
            model.Config = Config;

            var duplicate = Store.FirstOrDefault(x =>
                x.Id != model.Id &&
                string.Equals(x.Name, model.Name, StringComparison.CurrentCultureIgnoreCase));
            if (duplicate != null)
            {
                ModelState.AddModelError("Name", "Đã có mục trùng tên trong danh mục.");
            }

            if (!ModelState.IsValid) return View("CatalogEdit", model);

            var name = model.Name.Trim();

            if (model.Id == 0)
            {
                var item = new T
                {
                    Name = name,
                    Description = model.Description,
                    Priority = Config.ShowPriority ? model.Priority : 0,
                    IsClosed = Config.ShowExcludeFlag && model.IsClosed,
                    IsActive = model.IsActive,
                    SortOrder = model.SortOrder
                };
                Store.Insert(item);
                Notify(string.Format("Đã thêm {0} \"{1}\".", Config.ItemLabel, name));
                return RedirectToAction("Index");
            }

            var current = Store.Find(model.Id);
            if (current == null) return HttpNotFound();

            var oldName = current.Name;
            current.Name = name;
            current.Description = model.Description;
            if (Config.ShowPriority) current.Priority = model.Priority;
            if (Config.ShowExcludeFlag) current.IsClosed = model.IsClosed;
            current.IsActive = model.IsActive;
            current.SortOrder = model.SortOrder;
            Store.Update(current);

            // Bản ghi nghiệp vụ tham chiếu bằng tên, nên đổi tên phải kéo theo chúng.
            var renamed = RenameUsage(oldName, name);

            Notify(renamed > 0
                ? string.Format("Đã cập nhật {0} và đổi tên trên {1} bản ghi liên quan.", Config.ItemLabel, renamed)
                : string.Format("Đã cập nhật {0} \"{1}\".", Config.ItemLabel, name));

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var item = Store.Find(id);
            if (item == null) return HttpNotFound();

            // Còn bản ghi đang dùng thì không cho xoá, tránh để lại giá trị trỏ vào danh mục không tồn tại.
            int inUse;
            if (Usage().TryGetValue(item.Name, out inUse) && inUse > 0)
            {
                NotifyError(string.Format(
                    "Không thể xoá {0} \"{1}\" vì đang có {2} bản ghi sử dụng. "
                    + "Hãy chuyển các bản ghi đó sang giá trị khác, hoặc bỏ đánh dấu \"Đang sử dụng\" để ẩn khỏi danh sách chọn.",
                    Config.ItemLabel, item.Name, inUse));
                return RedirectToAction("Index");
            }

            Store.Delete(id);
            Notify(string.Format("Đã xoá {0} \"{1}\".", Config.ItemLabel, item.Name));
            return RedirectToAction("Index");
        }
    }
}
