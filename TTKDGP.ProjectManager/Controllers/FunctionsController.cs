using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>Một dòng chức năng trên màn hình, đã gộp danh mục gốc với phần cấu hình.</summary>
    public class FunctionRow
    {
        /// <summary>Mã đầy đủ, dạng "module.action".</summary>
        public string Code { get; set; }

        /// <summary>Tên gốc khai trong mã nguồn.</summary>
        public string DefaultName { get; set; }

        /// <summary>Tên đang hiển thị (đã sửa nếu có).</summary>
        public string DisplayName { get; set; }

        public bool IsEnabled { get; set; }
        public string Note { get; set; }

        /// <summary>Tên các nhóm quyền đang được cấp mã này.</summary>
        public List<string> Groups { get; set; }

        /// <summary>Số tài khoản có mã này qua nhóm của họ.</summary>
        public int UserCount { get; set; }

        /// <summary>Đã bị đổi tên so với mã nguồn chưa.</summary>
        public bool IsRenamed
        {
            get { return !string.Equals(DisplayName, DefaultName, StringComparison.Ordinal); }
        }
    }

    /// <summary>Một màn hình (module) cùng các chức năng của nó.</summary>
    public class FunctionModuleRow
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public List<FunctionRow> Functions { get; set; }

        /// <summary>Số chức năng đang tắt trong module này.</summary>
        public int DisabledCount { get { return Functions.Count(f => !f.IsEnabled); } }
    }

    /// <summary>
    /// Danh mục chức năng của phần mềm: đổi tên hiển thị và BẬT/TẮT từng chức năng.
    ///
    /// Không thêm/xoá được mã: mỗi mã gắn với một endpoint có thật trong mã nguồn
    /// (<c>[AppAuthorize(Permission = "...")]</c>), thêm một dòng ở đây mà không có endpoint nào
    /// dùng tới thì nó chẳng chặn hay mở được gì — chỉ là cái tên trong CSDL. Muốn có chức năng
    /// mới thì phải có màn hình mới. Xem <see cref="PermissionSettingService"/>.
    ///
    /// TẮT khác GỠ KHỎI NHÓM: tắt là đóng chức năng với tất cả mọi người kể cả tài khoản toàn
    /// quyền; gỡ khỏi nhóm chỉ là không cấp cho nhóm đó.
    /// </summary>
    [AppAuthorize]
    public class FunctionsController : BaseController
    {
        [AppAuthorize(Permission = "functions.view")]
        public ActionResult Index(string q, string category, string state)
        {
            var modules = BuildRows(q, category, state);

            ViewBag.Query = q;
            ViewBag.Category = category;
            ViewBag.State = state;
            ViewBag.Categories = Permissions.All.Select(m => m.Category).Distinct().ToList();
            ViewBag.CanEdit = Can(Permissions.Functions.Perm(Permissions.Edit));

            // Đếm trên TOÀN BỘ danh mục, không phải trên phần đang lọc: con số này để cảnh báo
            // "hệ thống đang có chức năng bị tắt", lọc mà đổi theo thì mất tác dụng cảnh báo.
            ViewBag.TotalCount = Permissions.AllCodes().Count();
            ViewBag.DisabledCount = PermissionSettingService.DisabledCodes().Count;

            return View(modules);
        }

        /// <summary>Hộp thoại sửa tên hiển thị / ghi chú của một chức năng.</summary>
        [HttpGet]
        [AppAuthorize(Permission = "functions.edit")]
        public ActionResult Edit(string code)
        {
            var trimmed = (code ?? string.Empty).Trim();

            var module = Permissions.All.FirstOrDefault(m =>
                m.Actions.Any(a => string.Equals(m.Perm(a.Code), trimmed, StringComparison.OrdinalIgnoreCase)));
            if (module == null) return HttpNotFound();

            var action = module.Actions.First(a =>
                string.Equals(module.Perm(a.Code), trimmed, StringComparison.OrdinalIgnoreCase));

            var setting = PermissionSettingService.Find(trimmed);

            ViewBag.ModuleName = module.Name;

            return PartialView(new FunctionRow
            {
                Code = module.Perm(action.Code),
                DefaultName = action.Name,
                DisplayName = PermissionSettingService.DisplayName(trimmed, action.Name),
                IsEnabled = setting == null || setting.IsEnabled,
                Note = setting == null ? null : setting.Note,
                Groups = new List<string>()
            });
        }

        /// <summary>Lưu tên hiển thị, trạng thái và ghi chú của MỘT chức năng.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "functions.edit")]
        public ActionResult Save(string code, string displayName, bool isEnabled, string note)
        {
            var guard = GuardSelfLockout(code, isEnabled);
            if (guard != null)
            {
                NotifyError(guard);
                return RedirectToAction("Index");
            }

            if (!PermissionSettingService.Save(code, displayName, isEnabled, note,
                    CurrentUser == null ? null : CurrentUser.FullName))
            {
                NotifyError("Mã chức năng không hợp lệ: " + code);
                return RedirectToAction("Index");
            }

            Notify(string.Format("Đã cập nhật chức năng \"{0}\" — {1}.",
                PermissionSettingService.DisplayName(code, code),
                isEnabled ? "đang bật" : "ĐÃ TẮT với mọi tài khoản"));

            return RedirectToAction("Index");
        }

        /// <summary>Bật/tắt hàng loạt theo các ô đang tích trên biểu mẫu.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "functions.edit")]
        public ActionResult SaveAll(string[] enabledCodes)
        {
            var enabled = enabledCodes ?? new string[0];

            // Không cho tự khoá mình ra khỏi chính màn này — sửa lại phải vào tận CSDL.
            var mine = Permissions.Functions.Perm(Permissions.View);
            var mineEdit = Permissions.Functions.Perm(Permissions.Edit);

            if (!enabled.Contains(mine, StringComparer.OrdinalIgnoreCase) ||
                !enabled.Contains(mineEdit, StringComparer.OrdinalIgnoreCase))
            {
                NotifyError("Không thể tắt chính hai chức năng của màn này "
                            + "(\"Chức năng hệ thống — Xem\" và \"Đổi tên / Bật tắt\"): "
                            + "tắt xong sẽ không ai mở lại được, phải sửa thẳng dưới cơ sở dữ liệu.");
                return RedirectToAction("Index");
            }

            var changed = PermissionSettingService.SaveEnabledSet(
                enabled, CurrentUser == null ? null : CurrentUser.FullName);

            Notify(changed == 0
                ? "Không có thay đổi nào."
                : string.Format("Đã cập nhật {0} chức năng.", changed));

            return RedirectToAction("Index");
        }

        /// <summary>Bỏ mọi tuỳ chỉnh, đưa danh mục về nguyên trạng mã nguồn (bật hết, tên gốc).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "functions.edit")]
        public ActionResult Reset()
        {
            var removed = PermissionSettingService.ResetAll();

            Notify(removed == 0
                ? "Danh mục chức năng vốn đã ở nguyên trạng."
                : string.Format("Đã bỏ {0} tuỳ chỉnh — mọi chức năng bật lại và dùng tên gốc.", removed));

            return RedirectToAction("Index");
        }

        // ---------- Trợ giúp ----------

        /// <summary>
        /// Chặn việc tự khoá mình ra ngoài: tắt "functions.view" hay "functions.edit" thì không
        /// còn đường vào màn này để bật lại. Trả về thông báo lỗi, hoặc null nếu không sao.
        /// </summary>
        private static string GuardSelfLockout(string code, bool isEnabled)
        {
            if (isEnabled) return null;

            var trimmed = (code ?? string.Empty).Trim();
            var isOwnScreen =
                string.Equals(trimmed, Permissions.Functions.Perm(Permissions.View), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, Permissions.Functions.Perm(Permissions.Edit), StringComparison.OrdinalIgnoreCase);

            return isOwnScreen
                ? "Không thể tắt chính chức năng của màn này — tắt xong sẽ không ai mở lại được."
                : null;
        }

        /// <summary>
        /// Dựng các dòng hiển thị: danh mục cứng trong mã nguồn, gộp với phần cấu hình đã lưu và
        /// thông tin nhóm quyền nào đang được cấp.
        /// </summary>
        private static List<FunctionModuleRow> BuildRows(string q, string category, string state)
        {
            var groupsByCode = GroupNamesByCode();
            var userCounts = UserCountsByCode();

            var keyword = (q ?? string.Empty).Trim();
            var result = new List<FunctionModuleRow>();

            foreach (var module in Permissions.All)
            {
                if (!string.IsNullOrWhiteSpace(category) &&
                    !string.Equals(module.Category, category, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                var functions = new List<FunctionRow>();

                foreach (var action in module.Actions)
                {
                    var code = module.Perm(action.Code);
                    var setting = PermissionSettingService.Find(code);
                    var enabled = setting == null || setting.IsEnabled;

                    if (state == "on" && !enabled) continue;
                    if (state == "off" && enabled) continue;

                    var display = PermissionSettingService.DisplayName(code, action.Name);

                    // Tìm theo mã, tên gốc lẫn tên đã sửa: người dùng nhớ mã ("kpi.view") hay nhớ
                    // chữ trên màn hình ("chấm KPI") đều phải ra.
                    if (keyword.Length > 0 &&
                        code.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) < 0 &&
                        display.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) < 0 &&
                        action.Name.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) < 0 &&
                        module.Name.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) < 0)
                    {
                        continue;
                    }

                    List<string> groups;
                    if (!groupsByCode.TryGetValue(code, out groups)) groups = new List<string>();

                    int users;
                    userCounts.TryGetValue(code, out users);

                    functions.Add(new FunctionRow
                    {
                        Code = code,
                        DefaultName = action.Name,
                        DisplayName = display,
                        IsEnabled = enabled,
                        Note = setting == null ? null : setting.Note,
                        Groups = groups,
                        UserCount = users
                    });
                }

                if (functions.Count == 0) continue;

                result.Add(new FunctionModuleRow
                {
                    Code = module.Code,
                    Name = module.Name,
                    Category = module.Category,
                    Functions = functions
                });
            }

            return result;
        }

        /// <summary>
        /// Tên các nhóm quyền đang cấp mỗi mã chức năng. Nhóm toàn quyền ("*") tính là có TẤT CẢ
        /// các mã — nếu không thì màn hình sẽ báo "chưa nhóm nào có quyền này" trong khi quản trị
        /// viên vẫn vào được, nhìn như số liệu sai.
        /// </summary>
        private static Dictionary<string, List<string>> GroupNamesByCode()
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            Action<string, string> add = (code, groupName) =>
            {
                List<string> names;
                if (!map.TryGetValue(code, out names))
                {
                    names = new List<string>();
                    map[code] = names;
                }
                if (!names.Contains(groupName)) names.Add(groupName);
            };

            try
            {
                foreach (var group in Repository.RoleGroups.All().Where(g => g.IsActive))
                {
                    var permissions = (group.Permissions ?? string.Empty).Trim();

                    if (permissions == "*")
                    {
                        foreach (var code in Permissions.AllCodes()) add(code, group.Name);
                        continue;
                    }

                    foreach (var code in permissions.Split(','))
                    {
                        var trimmed = code.Trim();
                        if (trimmed.Length > 0) add(trimmed, group.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                // SQL chập chờn: bày danh mục chức năng mà bỏ trống cột nhóm, còn hơn trang trắng.
                System.Diagnostics.Debug.WriteLine("Đọc nhóm quyền trượt: " + ex.Message);
            }

            return map;
        }

        /// <summary>Số tài khoản đang có mỗi mã chức năng, tính qua nhóm quyền của họ.</summary>
        private static Dictionary<string, int> UserCountsByCode()
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Gom theo NHÓM trước rồi mới nhân với số tài khoản: quét quyền cho từng tài khoản
                // là lặp lại cùng một phép tính cho mọi người cùng nhóm.
                var usersByGroup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var user in Repository.Users.All())
                {
                    foreach (var code in Models.Roles.Split(user.Role))
                    {
                        usersByGroup[code] = usersByGroup.ContainsKey(code) ? usersByGroup[code] + 1 : 1;
                    }
                }

                foreach (var group in Repository.RoleGroups.All().Where(g => g.IsActive))
                {
                    int users;
                    if (!usersByGroup.TryGetValue(group.Code ?? string.Empty, out users) || users == 0) continue;

                    var permissions = (group.Permissions ?? string.Empty).Trim();
                    var codes = permissions == "*"
                        ? Permissions.AllCodes()
                        : permissions.Split(',').Select(c => c.Trim()).Where(c => c.Length > 0);

                    foreach (var code in codes)
                    {
                        counts[code] = counts.ContainsKey(code) ? counts[code] + users : users;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Đếm tài khoản theo chức năng trượt: " + ex.Message);
            }

            return counts;
        }
    }
}
