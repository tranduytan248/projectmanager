using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>Quản trị tài khoản đăng nhập.</summary>
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
                SelectedRoles = Roles.Split(user.Role),
                MemberId = user.MemberId,
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

            // Nhóm có quyền tự báo cáo công việc thì tài khoản phải gắn nhân sự — để biết báo cáo
            // cho phân công nào. Các nhóm khác thì để trống cũng được.
            var needsMember = Permissions.UserHas(roleValue, Permissions.MyReports.Perm("report"));
            if (needsMember && model.MemberId <= 0)
            {
                ModelState.AddModelError("MemberId",
                    "Nhóm có quyền báo cáo công việc phải chọn nhân sự tương ứng.");
            }

            if (model.MemberId > 0 && Repository.Members.Find(model.MemberId) == null)
            {
                ModelState.AddModelError("MemberId", "Nhân sự không tồn tại.");
            }

            // Một nhân sự chỉ nên gắn với một tài khoản báo cáo, tránh hai người cùng ghi một chỗ.
            if (needsMember && model.MemberId > 0)
            {
                var taken = Repository.Users.FirstOrDefault(u =>
                    u.Id != model.Id &&
                    u.MemberId == model.MemberId &&
                    Permissions.UserHas(u.Role, Permissions.MyReports.Perm("report")));
                if (taken != null)
                {
                    ModelState.AddModelError("MemberId",
                        "Nhân sự này đã gắn với tài khoản \"" + taken.UserName + "\".");
                }
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

            if (!ModelState.IsValid)
            {
                PopulateLists();
                return View(model);
            }

            // Giữ liên kết nhân sự cho mọi quyền: tài khoản Quản lý/Quản trị có gắn nhân sự thì
            // cũng dùng được màn "Báo cáo của tôi" cho phần việc của chính họ.
            if (model.Id == 0)
            {
                Repository.Users.Insert(new User
                {
                    UserName = model.UserName.Trim(),
                    FullName = model.FullName.Trim(),
                    Email = email,
                    Role = roleValue,
                    MemberId = model.MemberId,
                    PasswordHash = PasswordHasher.Hash(model.Password),
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
            user.Role = roleValue;
            user.MemberId = model.MemberId;
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

        // ---------- Mở tài khoản hàng loạt cho nhân sự ----------

        /// <summary>Đuôi email cơ quan; tên đăng nhập là phần đứng trước đuôi này.</summary>
        private const string EmailDomain = "@vnpt.vn";

        /// <summary>Mật khẩu gợi ý sẵn trên form, người tạo đổi lại được trước khi bấm.</summary>
        private const string SuggestedPassword = "Ncpt@2026";

        /// <summary>
        /// Xem trước danh sách tài khoản sắp mở cho nhân sự chưa có, kèm lý do với những
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

            if (string.IsNullOrEmpty(model.Password) || model.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Mật khẩu khởi tạo tối thiểu 6 ký tự.");
                return View(model);
            }

            if (model.ToCreate.Count == 0)
            {
                Notify("Mọi nhân sự đang làm việc đều đã có tài khoản, không có gì để tạo.");
                return RedirectToAction("Index");
            }

            foreach (var row in model.ToCreate)
            {
                Repository.Users.Insert(new User
                {
                    UserName = row.UserName,
                    FullName = row.MemberName,
                    Email = row.Email,
                    Role = Roles.Reporter,
                    MemberId = row.MemberId,
                    PasswordHash = PasswordHasher.Hash(model.Password),
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }

            Notify(string.Format("Đã mở {0} tài khoản Báo cáo công việc với mật khẩu khởi tạo vừa nhập.",
                model.ToCreate.Count));
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Đối chiếu nhân sự đang làm việc với tài khoản hiện có để biết ai còn thiếu.
        /// Mốc so sánh là TÊN ĐĂNG NHẬP suy từ email (bỏ đuôi <see cref="EmailDomain"/>):
        /// có tài khoản trùng tên đó thì coi như nhân sự đã có tài khoản, không thì mở mới.
        /// </summary>
        private UserProvisionViewModel BuildProvisionModel()
        {
            var model = new UserProvisionViewModel();

            var users = Repository.Users.All();

            var members = Repository.Members.All()
                .Where(m => m.IsActive)
                .OrderBy(m => m.FullName, StringComparer.CurrentCulture)
                .ToList();

            foreach (var m in members)
            {
                var email = (m.Email ?? string.Empty).Trim();

                if (email.Length == 0)
                {
                    AddSkip(model, m, "Chưa có email nên không suy ra được tên đăng nhập.");
                    continue;
                }

                if (!email.EndsWith(EmailDomain, StringComparison.OrdinalIgnoreCase))
                {
                    AddSkip(model, m, "Email không thuộc miền " + EmailDomain + ".");
                    continue;
                }

                var userName = email.Substring(0, email.Length - EmailDomain.Length).Trim();

                if (userName.Length < 3)
                {
                    AddSkip(model, m, "Tên đăng nhập suy ra từ email quá ngắn.");
                    continue;
                }

                // Đã có tài khoản mang đúng tên đăng nhập này — nhân sự coi như đã có tài khoản.
                var sameName = users.FirstOrDefault(u =>
                    string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));
                if (sameName != null)
                {
                    continue;
                }

                // Nhân sự đã gắn với một tài khoản mang tên khác. Không mở thêm, mà nêu ra để
                // quản trị tự quyết — mở nữa là một người hai tài khoản.
                var linked = users.FirstOrDefault(u => u.MemberId == m.Id);
                if (linked != null)
                {
                    AddSkip(model, m, "Đã gắn với tài khoản \"" + linked.UserName
                        + "\" (khác tên đăng nhập suy từ email).");
                    continue;
                }

                var sameEmail = users.FirstOrDefault(u =>
                    !string.IsNullOrWhiteSpace(u.Email) &&
                    string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                if (sameEmail != null)
                {
                    AddSkip(model, m, "Email đang dùng cho tài khoản \"" + sameEmail.UserName + "\".");
                    continue;
                }

                model.ToCreate.Add(new UserProvisionRow
                {
                    MemberId = m.Id,
                    MemberName = m.FullName,
                    Email = email,
                    UserName = userName
                });
            }

            model.UnlinkedUsers = users
                .Where(u => u.MemberId <= 0)
                .OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return model;
        }

        private static void AddSkip(UserProvisionViewModel model, Member member, string reason)
        {
            model.Skipped.Add(new UserProvisionSkip
            {
                MemberName = member.FullName,
                Email = member.Email,
                Reason = reason
            });
        }

        /// <summary>Nguồn cho form tài khoản: danh sách nhân sự và danh sách nhóm quyền để chọn.</summary>
        private void PopulateLists()
        {
            ViewBag.MemberOptions = Repository.Members.All()
                .Where(m => m.IsActive)
                .OrderBy(m => m.FullName, StringComparer.CurrentCulture)
                .ToList();

            ViewBag.RoleGroupOptions = Repository.RoleGroups.All()
                .Where(g => g.IsActive)
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name, StringComparer.CurrentCulture)
                .ToList();
        }
    }
}
