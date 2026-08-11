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
    /// <summary>
    /// Quản trị tài khoản đăng nhập. Quản lý ĐỘC LẬP: tài khoản không gắn với hồ sơ thành viên
    /// hay nhân sự nào — công việc và dự án của bộ quản lý công việc gán thẳng theo tài khoản.
    /// </summary>
    [AppAuthorize]
    public class UsersController : BaseController
    {
        [AppAuthorize(Permission = "users.view")]
        public ActionResult Index()
        {
            var users = Repository.Users.All()
                .OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return View(users);
        }

        [HttpGet]
        [AppAuthorize(Permission = "users.create,users.edit")]
        public ActionResult Edit(int? id)
        {
            PopulateLists();

            if (!id.HasValue)
            {
                return View(new UserEditViewModel
                {
                    SelectedRoles = new[] { Roles.Manager },
                    IsActive = true
                });
            }

            var user = Repository.Users.Find(id.Value);
            if (user == null) return HttpNotFound();

            return View(new UserEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                SelectedRoles = Roles.Split(user.Role),
                IsTeamManager = user.IsTeamManager,
                IsActive = user.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "users.create,users.edit")]
        public ActionResult Edit(UserEditViewModel model)
        {
            // Tạo mới thì bắt buộc có mật khẩu; sửa thì để trống nghĩa là giữ nguyên mật khẩu cũ.
            if (model.Id == 0 && string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu cho tài khoản mới.");
            }

            // Gộp các nhóm được chọn thành chuỗi lưu trữ; Join đã bỏ trùng lặp và khoảng trắng.
            var roleValue = Roles.Join(model.SelectedRoles);

            if (string.IsNullOrEmpty(roleValue))
            {
                ModelState.AddModelError("SelectedRoles", "Vui lòng chọn ít nhất một nhóm quyền.");
            }
            else if (Roles.Split(roleValue).Any(code =>
                Repository.RoleGroups.FirstOrDefault(g => string.Equals(g.Code, code, StringComparison.OrdinalIgnoreCase)) == null))
            {
                ModelState.AddModelError("SelectedRoles", "Có nhóm quyền không tồn tại.");
            }

            var duplicate = Repository.Users.FirstOrDefault(u =>
                u.Id != model.Id &&
                string.Equals(u.UserName, model.UserName, StringComparison.OrdinalIgnoreCase));
            if (duplicate != null)
            {
                ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại.");
            }

            // Email là tuỳ chọn, nhưng nếu có nhập thì không được trùng tài khoản khác.
            var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            if (email != null)
            {
                var duplicateEmail = Repository.Users.FirstOrDefault(u =>
                    u.Id != model.Id &&
                    string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                if (duplicateEmail != null)
                {
                    ModelState.AddModelError("Email", "Email đã được dùng cho tài khoản khác.");
                }
            }

            // Số điện thoại cũng tuỳ chọn. Có nhập thì bắt phải đúng dạng ngay tại đây bằng chính
            // bộ chuẩn hoá của tổng đài — sai dạng mà để lọt thì tới lúc gửi SMS mới vỡ, khi đó
            // không còn ai ngồi đó mà sửa.
            var phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            if (phone != null)
            {
                List<string> invalid;
                var normalized = SmsClient.NormalizePhones(phone, out invalid);

                if (normalized.Count != 1 || invalid.Count > 0)
                {
                    ModelState.AddModelError("Phone",
                        "Số điện thoại không hợp lệ. Nhập một số dạng 09xxxxxxxx hoặc 84xxxxxxxxx.");
                }
            }

            if (!ModelState.IsValid)
            {
                PopulateLists();
                return View(model);
            }

            if (model.Id == 0)
            {
                Repository.Users.Insert(new User
                {
                    UserName = model.UserName.Trim(),
                    FullName = model.FullName.Trim(),
                    Email = email,
                    Phone = phone,
                    Role = roleValue,
                    PasswordHash = PasswordHasher.Hash(model.Password),
                    IsTeamManager = model.IsTeamManager,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                });
                Notify("Đã tạo tài khoản \"" + model.UserName + "\".");
                return RedirectToAction("Index");
            }

            var user = Repository.Users.Find(model.Id);
            if (user == null) return HttpNotFound();

            // Không cho tự cắt quyền quản lý người dùng hoặc tự khoá mình, tránh mất lối vào.
            if (user.Id == CurrentUser.UserId)
            {
                if (!Permissions.UserHas(roleValue, Permissions.Users.Perm(Permissions.Edit)))
                {
                    ModelState.AddModelError("SelectedRoles",
                        "Bạn không thể tự bỏ quyền quản lý người dùng của chính mình.");
                }
                if (!model.IsActive)
                {
                    ModelState.AddModelError("IsActive", "Bạn không thể tự khoá tài khoản của chính mình.");
                }
                if (!ModelState.IsValid)
                {
                    PopulateLists();
                    return View(model);
                }
            }

            user.UserName = model.UserName.Trim();
            user.FullName = model.FullName.Trim();
            user.Email = email;
            user.Phone = phone;
            user.Role = roleValue;
            user.IsTeamManager = model.IsTeamManager;
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
        [AppAuthorize(Permission = "users.delete")]
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

        // ---------- Đổ số điện thoại từ HRM sang bảng Người dùng ----------

        /// <summary>
        /// Xem trước việc lấy số điện thoại bên nhân sự HRM đổ sang tài khoản, ghép theo email.
        /// Chưa ghi gì — bấm xác nhận mới ghi.
        /// </summary>
        [HttpGet]
        [AppAuthorize(Permission = "users.provision")]
        public ActionResult SyncPhones()
        {
            return View(UserPhoneSyncService.Build());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("SyncPhones")]
        [AppAuthorize(Permission = "users.provision")]
        public ActionResult SyncPhonesConfirmed()
        {
            var updated = UserPhoneSyncService.Apply();

            Notify(updated == 0
                ? "Không có số nào cần cập nhật."
                : string.Format("Đã cập nhật số điện thoại cho {0} tài khoản.", updated));

            return RedirectToAction("Index");
        }

        // ---------- Mở tài khoản hàng loạt từ HRM ----------

        /// <summary>Đuôi email cơ quan; tên đăng nhập là phần đứng trước đuôi này.</summary>
        private const string EmailDomain = "@vnpt.vn";

        /// <summary>Mật khẩu gợi ý sẵn trên form, người tạo đổi lại được trước khi bấm.</summary>
        private const string SuggestedPassword = "Vnpt@2026";

        /// <summary>Trần số nhân sự HRM nạp lên để lọc. Toàn tỉnh khoảng 800 người nên 5000 là dư.</summary>
        private const int MaxHrmFetch = 5000;

        /// <summary>
        /// Xem trước danh sách tài khoản sắp mở cho nhân sự HRM chưa có, kèm lý do với những
        /// người bị bỏ qua. Chưa ghi gì vào dữ liệu.
        /// </summary>
        [HttpGet]
        [AppAuthorize(Permission = "users.provision")]
        public ActionResult Provision()
        {
            var model = BuildProvisionModel();
            model.Password = SuggestedPassword;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Provision")]
        [AppAuthorize(Permission = "users.provision")]
        public ActionResult ProvisionConfirmed(UserProvisionViewModel form)
        {
            // Dựng lại danh sách từ dữ liệu hiện tại chứ không tin danh sách gửi lên, phòng khi
            // người khác vừa thêm tài khoản trong lúc màn xem trước còn mở.
            var model = BuildProvisionModel();
            model.Password = form == null ? null : form.Password;
            model.ResetExisting = form != null && form.ResetExisting;

            if (string.IsNullOrEmpty(model.Password) || model.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Mật khẩu khởi tạo tối thiểu 6 ký tự.");
                return View(model);
            }

            if (!model.HasWork)
            {
                Notify("Không có nhân sự nào để mở tài khoản.");
                return RedirectToAction("Index");
            }

            var now = DateTime.Now;
            var created = 0;
            var reset = 0;
            var phoneUpdated = 0;

            foreach (var row in model.ToCreate)
            {
                Repository.Users.Insert(new User
                {
                    UserName = row.UserName,
                    FullName = row.FullName,
                    Email = row.Email,
                    Phone = row.Phone,
                    Role = Roles.Reporter,
                    PasswordHash = PasswordHasher.Hash(model.Password),
                    IsActive = true,
                    CreatedAt = now
                });

                created++;
            }

            foreach (var row in model.Existing)
            {
                var user = Repository.Users.Find(row.UserId);
                if (user == null) continue;

                var changed = false;

                // Tài khoản cũ có thể chưa điền email nên chỉ ghép được theo tên đăng nhập. Bù vào
                // để lần sau còn đối chiếu được cả hai đường.
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    user.Email = row.Email;
                    changed = true;
                }

                // HRM là nguồn của số điện thoại nên số bên đó thắng. Nhưng HRM bỏ trống thì giữ
                // nguyên số đang có — nhiều người được điền tay vì HRM chưa khai, xoá đi là mất.
                if (row.PhoneWillUpdate)
                {
                    user.Phone = row.Phone;
                    changed = true;
                    phoneUpdated++;
                }

                if (model.ResetExisting)
                {
                    user.PasswordHash = PasswordHasher.Hash(model.Password);
                    changed = true;
                    reset++;
                }

                if (changed) Repository.Users.Update(user);
            }

            var parts = new List<string>();
            if (created > 0) parts.Add(string.Format("mở {0} tài khoản", created));
            if (phoneUpdated > 0) parts.Add(string.Format("cập nhật số điện thoại {0} tài khoản", phoneUpdated));
            if (reset > 0) parts.Add(string.Format("đặt lại mật khẩu {0} tài khoản đã có", reset));

            Notify(parts.Count == 0
                ? "Không có thay đổi nào."
                : "Đã " + string.Join(", ", parts) + ".");
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Đối chiếu nhân sự HRM (đơn vị khai ở Work:HrmWorkplace và toàn bộ đơn vị con) với tài
        /// khoản hiện có. KHÔNG gắn tài khoản với hồ sơ nào — chỉ mở tài khoản còn thiếu.
        ///
        /// Mốc so sánh là TÊN ĐĂNG NHẬP suy từ email cơ quan (bỏ đuôi <see cref="EmailDomain"/>) —
        /// đúng bằng tài khoản HRM của người đó. Có tài khoản trùng tên thì dùng lại chứ không mở
        /// thêm; chưa có thì mở mới.
        /// </summary>
        private UserProvisionViewModel BuildProvisionModel()
        {
            var model = new UserProvisionViewModel();

            var users = Repository.Users.All();
            var matchedUserIds = new HashSet<int>();

            List<HrWorkplace> subtree;
            var root = FindWorkplaceSubtree(AppSettings.Work.HrmWorkplace, out subtree);

            if (root == null)
            {
                // KHÔNG lùi về hiện tất cả — như vậy sẽ lặng lẽ mở tài khoản cho cả tỉnh. Báo rõ
                // để người dùng biết mà sửa cấu hình hoặc đồng bộ lại cây đơn vị.
                model.UnitError = string.Format(
                    "Không tìm thấy đơn vị \"{0}\" trong dữ liệu HRM. Kiểm lại khoá Work:HrmWorkplace "
                    + "trong Web.config, hoặc đồng bộ lại danh sách đơn vị ở màn HRM.",
                    AppSettings.Work.HrmWorkplace);
                model.UnlinkedUsers = users
                    .OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return model;
            }

            model.UnitName = string.IsNullOrWhiteSpace(root.WpCode)
                ? root.WpName
                : root.WpName + " (" + root.WpCode + ")";

            var ids = new HashSet<string>(subtree.Select(w => w.WpId), StringComparer.OrdinalIgnoreCase);
            var employees = HrEmployeeStore.Page(1, MaxHrmFetch, null).Items
                .Where(e => !string.IsNullOrWhiteSpace(e.WorkplaceId) && ids.Contains(e.WorkplaceId))
                .OrderBy(e => e.FullName, StringComparer.CurrentCulture)
                .ToList();

            foreach (var e in employees)
            {
                var email = (e.Email ?? string.Empty).Trim();

                if (email.Length == 0)
                {
                    AddSkip(model, e.FullName, email, "Chưa có email nên không suy ra được tài khoản HRM.");
                    continue;
                }

                if (!email.EndsWith(EmailDomain, StringComparison.OrdinalIgnoreCase))
                {
                    AddSkip(model, e.FullName, email, "Email không thuộc miền " + EmailDomain + ".");
                    continue;
                }

                var userName = email.Substring(0, email.Length - EmailDomain.Length).Trim();

                if (userName.Length < 3)
                {
                    AddSkip(model, e.FullName, email, "Tài khoản HRM suy ra từ email quá ngắn.");
                    continue;
                }

                var row = new UserProvisionRow
                {
                    EmployeeCode = e.Code,
                    FullName = e.FullName,
                    Email = email,
                    Phone = (e.PhoneNumber ?? string.Empty).Trim(),
                    UserName = userName
                };

                // Khớp theo tên đăng nhập trước; một số tài khoản cũ chưa điền email nên không thể
                // chỉ dựa vào email để nhận ra.
                var account = users.FirstOrDefault(u =>
                    string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase))
                    ?? users.FirstOrDefault(u =>
                        !string.IsNullOrWhiteSpace(u.Email)
                        && string.Equals(u.Email.Trim(), email, StringComparison.OrdinalIgnoreCase));

                if (account == null)
                {
                    model.ToCreate.Add(row);
                    continue;
                }

                row.UserId = account.Id;
                row.CurrentPhone = (account.Phone ?? string.Empty).Trim();

                // Chỉ tính là "sẽ cập nhật" khi HRM thật sự có số và số đó khác số đang lưu.
                // HRM bỏ trống thì giữ nguyên số cũ — không lấy khoảng trắng đè lên dữ liệu tốt.
                row.PhoneWillUpdate = row.Phone.Length > 0
                    && !string.Equals(row.Phone, row.CurrentPhone, StringComparison.Ordinal);

                matchedUserIds.Add(account.Id);
                model.Existing.Add(row);
            }

            model.UnlinkedUsers = users
                .Where(u => !matchedUserIds.Contains(u.Id))
                .OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return model;
        }

        private static void AddSkip(UserProvisionViewModel model, string fullName, string email, string reason)
        {
            model.Skipped.Add(new UserProvisionSkip
            {
                FullName = fullName,
                Email = email,
                Reason = reason
            });
        }

        /// <summary>
        /// Tìm đơn vị HRM theo tên (hoặc mã) rồi gom toàn bộ đơn vị con của nó.
        /// Trả về null nếu không có đơn vị nào khớp.
        /// </summary>
        private static HrWorkplace FindWorkplaceSubtree(string nameOrCode, out List<HrWorkplace> subtree)
        {
            subtree = new List<HrWorkplace>();
            if (string.IsNullOrWhiteSpace(nameOrCode)) return null;

            var all = HrWorkplaceStore.All();
            var needle = nameOrCode.Trim();

            var root = all.FirstOrDefault(w =>
                string.Equals(w.WpName, needle, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(w.WpCode, needle, StringComparison.OrdinalIgnoreCase));

            if (root == null) return null;

            // Đi xuống theo WpParent. Có canh chừng đã duyệt để dữ liệu lỗi tạo vòng lặp cũng không treo.
            var byParent = all.Where(w => !string.IsNullOrWhiteSpace(w.WpParent))
                .GroupBy(w => w.WpParent, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<HrWorkplace>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (string.IsNullOrWhiteSpace(current.WpId) || !seen.Add(current.WpId)) continue;

                subtree.Add(current);

                List<HrWorkplace> children;
                if (!byParent.TryGetValue(current.WpId, out children)) continue;
                foreach (var child in children) queue.Enqueue(child);
            }

            return root;
        }

        /// <summary>Nguồn cho form tài khoản: danh sách nhóm quyền để chọn.</summary>
        private void PopulateLists()
        {
            ViewBag.RoleGroupOptions = Repository.RoleGroups.All()
                .Where(g => g.IsActive)
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name, StringComparer.CurrentCulture)
                .ToList();
        }
    }
}
