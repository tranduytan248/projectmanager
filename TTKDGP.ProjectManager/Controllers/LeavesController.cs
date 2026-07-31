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
    /// Đăng ký và duyệt nghỉ phép.
    ///
    /// Màn chia làm hai nửa theo hai quyền khác nhau:
    ///   • My/Edit/Cancel — quyền leaves.view, AI CŨNG CÓ, chỉ đụng được vào đơn của chính mình.
    ///   • Approve/SetState — quyền leaves.approve, chỉ Quản lý Tổ.
    ///
    /// Không gộp một màn chung vì như thế người dùng thường sẽ thấy đơn của cả Tổ. Mọi action ở
    /// nửa đầu đều kiểm lại UserId của đơn so với người đăng nhập: thiếu lớp đó thì đổi id trên
    /// thanh địa chỉ là sửa được đơn người khác.
    /// </summary>
    [AppAuthorize]
    public class LeavesController : BaseController
    {
        /// <summary>Số ngày nghỉ tối đa cho một đơn — chặn nhầm lẫn kiểu chọn nhầm năm.</summary>
        private const int MaxDaysPerRequest = 60;

        // ================= Phần của cá nhân =================

        /// <summary>Đơn nghỉ phép của chính mình.</summary>
        [AppAuthorize(Permission = "leaves.view")]
        public ActionResult My(int year = 0, string state = null, int page = 1)
        {
            var items = LeaveService.OfUser(CurrentUserId).AsEnumerable();

            if (year > 0) items = items.Where(l => l.FromDate.Year == year || l.ToDate.Year == year);
            if (!string.IsNullOrWhiteSpace(state)) items = items.Where(l => l.State == state);

            var list = items.ToList();
            var all = LeaveService.OfUser(CurrentUserId);

            ViewBag.Year = year;
            ViewBag.State = state;
            ViewBag.Years = all
                .Select(l => l.FromDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            // Số ngày đã duyệt của NĂM đang lọc (mặc định năm nay) — con số người dùng cần nhất.
            var statYear = year > 0 ? year : DateTime.Today.Year;
            ViewBag.StatYear = statYear;
            ViewBag.ApprovedDaysInYear = all
                .Where(l => l.IsApproved)
                .Sum(l => l.DaysInRange(new DateTime(statYear, 1, 1), new DateTime(statYear, 12, 31)));
            ViewBag.PendingCount = all.Count(l => l.State == LeaveStates.Pending);

            return View(PagedList<LeaveRequest>.From(list, page, WorkService.PageSize));
        }

        [HttpGet]
        [AppAuthorize(Permission = "leaves.view")]
        public ActionResult Edit(int? id)
        {
            LeaveRequest leave;

            if (id.HasValue)
            {
                leave = Repository.LeaveRequests.Find(id.Value);

                // Đơn của người khác thì coi như không tồn tại — 404 chứ không phải 403.
                if (leave == null || leave.UserId != CurrentUserId) return HttpNotFound();

                if (!LeaveStates.IsEditable(leave.State))
                {
                    NotifyError("Đơn đã được xử lý nên không sửa được nữa.");
                    return RedirectToAction("My");
                }
            }
            else
            {
                leave = new LeaveRequest
                {
                    Kind = LeaveKinds.Annual,
                    FromDate = DateTime.Today,
                    ToDate = DateTime.Today,
                    State = LeaveStates.Pending
                };
            }

            return Request.IsAjaxRequest()
                ? (ActionResult)PartialView("_EditForm", leave)
                : View(leave);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "leaves.view")]
        public ActionResult Edit(LeaveRequest model)
        {
            var isNew = model.Id <= 0;
            LeaveRequest current = null;

            if (!isNew)
            {
                current = Repository.LeaveRequests.Find(model.Id);
                if (current == null || current.UserId != CurrentUserId) return HttpNotFound();

                if (!LeaveStates.IsEditable(current.State))
                {
                    NotifyError("Đơn đã được xử lý nên không sửa được nữa.");
                    return RedirectToAction("My");
                }
            }

            Validate(model);

            if (!ModelState.IsValid)
            {
                return Request.IsAjaxRequest()
                    ? (ActionResult)PartialView("_EditForm", model)
                    : View(model);
            }

            if (isNew)
            {
                var leave = new LeaveRequest
                {
                    UserId = CurrentUserId,
                    UserFullName = CurrentUser == null ? null : CurrentUser.FullName,
                    State = LeaveStates.Pending,
                    CreatedAt = DateTime.Now
                };

                CopyInput(model, leave);
                Repository.LeaveRequests.Insert(leave);

                NotifyApprovers(leave);
                Notify(string.Format("Đã gửi đơn nghỉ {0:0.#} ngày, đang chờ duyệt.", leave.Days));
            }
            else
            {
                CopyInput(model, current);
                current.UpdatedAt = DateTime.Now;
                Repository.LeaveRequests.Update(current);

                Notify("Đã cập nhật đơn nghỉ phép.");
            }

            return Saved("My");
        }

        /// <summary>Thu hồi đơn của chính mình khi chưa được duyệt.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "leaves.view")]
        public ActionResult Cancel(int id)
        {
            var leave = Repository.LeaveRequests.Find(id);
            if (leave == null || leave.UserId != CurrentUserId) return HttpNotFound();

            if (!LeaveStates.IsEditable(leave.State))
            {
                NotifyError("Đơn đã được xử lý nên không thu hồi được nữa.");
                return RedirectToAction("My");
            }

            leave.State = LeaveStates.Cancelled;
            leave.UpdatedAt = DateTime.Now;
            Repository.LeaveRequests.Update(leave);

            Notify("Đã thu hồi đơn nghỉ phép.");
            return RedirectToAction("My");
        }

        // ================= Phần của Quản lý Tổ =================

        /// <summary>
        /// Danh sách đơn của toàn Tổ để duyệt. Mặc định chỉ hiện đơn CHỜ DUYỆT — đó là việc cần
        /// làm; đơn đã xử lý xem lại bằng bộ lọc trạng thái.
        /// </summary>
        [AppAuthorize(Permission = "leaves.approve")]
        public ActionResult Approve(string q = null, int userId = 0, string state = null,
            int year = 0, int month = 0, int page = 1)
        {
            // Chưa chọn gì thì mặc định là đơn chờ duyệt.
            if (state == null) state = LeaveStates.Pending;

            var all = Repository.LeaveRequests.All();
            var items = all.AsEnumerable();

            if (state.Length > 0) items = items.Where(l => l.State == state);
            if (userId > 0) items = items.Where(l => l.UserId == userId);

            if (year > 0 && month > 0)
            {
                var from = new DateTime(year, month, 1);
                var to = from.AddMonths(1).AddDays(-1);
                items = items.Where(l => l.OverlapsRange(from, to));
            }
            else if (year > 0)
            {
                items = items.Where(l => l.FromDate.Year == year || l.ToDate.Year == year);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                items = items.Where(l =>
                    (l.UserFullName ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || (l.Reason ?? string.Empty).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0);
            }

            var list = items
                .OrderBy(l => l.State == LeaveStates.Pending ? 0 : 1)
                .ThenByDescending(l => l.FromDate)
                .ThenByDescending(l => l.Id)
                .ToList();

            ViewBag.Query = q;
            ViewBag.UserId = userId;
            ViewBag.State = state;
            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.Users = WorkService.ActiveUsers();
            ViewBag.PendingCount = all.Count(l => l.State == LeaveStates.Pending);
            ViewBag.Years = all
                .Select(l => l.FromDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            return View(PagedList<LeaveRequest>.From(list, page, WorkService.PageSize));
        }

        /// <summary>
        /// Duyệt hoặc từ chối một đơn.
        ///
        /// Duyệt xong KHÔNG tính lại KPI ngay: bảng KPI là ảnh chụp tại lần bấm "Tính KPI" gần
        /// nhất, và số ngày nghỉ được đọc lại từ đây mỗi lần tính. Xem KpiService.Fill.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "leaves.approve")]
        public ActionResult SetState(int id, string state, string note)
        {
            var leave = Repository.LeaveRequests.Find(id);
            if (leave == null) return HttpNotFound();

            if (state != LeaveStates.Approved && state != LeaveStates.Rejected)
            {
                NotifyError("Trạng thái không hợp lệ.");
                return BackToList("Approve");
            }

            if (state == LeaveStates.Rejected && string.IsNullOrWhiteSpace(note))
            {
                NotifyError("Vui lòng nhập lý do từ chối để người đăng ký nắm được.");
                return BackToList("Approve");
            }

            leave.State = state;
            leave.ApprovedByUserId = CurrentUserId;
            leave.ApprovedByName = CurrentUser == null ? null : CurrentUser.FullName;
            leave.ApprovedAt = DateTime.Now;
            leave.ApproverNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            leave.UpdatedAt = DateTime.Now;
            Repository.LeaveRequests.Update(leave);

            NotifyRequester(leave);

            Notify(state == LeaveStates.Approved
                ? string.Format("Đã duyệt đơn nghỉ {0:0.#} ngày của {1}.", leave.Days, leave.UserFullName)
                : string.Format("Đã từ chối đơn nghỉ phép của {0}.", leave.UserFullName));

            return BackToList("Approve");
        }

        // ================= Dùng chung =================

        /// <summary>
        /// Kiểm dữ liệu nhập của một đơn. Gom vào một chỗ để màn thêm và màn sửa không lệch luật.
        /// </summary>
        private void Validate(LeaveRequest model)
        {
            if (model.FromDate == default(DateTime))
            {
                ModelState.AddModelError("FromDate", "Vui lòng chọn ngày bắt đầu nghỉ.");
            }

            if (model.ToDate == default(DateTime))
            {
                ModelState.AddModelError("ToDate", "Vui lòng chọn ngày kết thúc nghỉ.");
            }

            if (model.FromDate != default(DateTime) && model.ToDate != default(DateTime))
            {
                if (model.ToDate.Date < model.FromDate.Date)
                {
                    ModelState.AddModelError("ToDate", "Ngày kết thúc phải từ ngày bắt đầu trở đi.");
                }
                else
                {
                    var span = (model.ToDate.Date - model.FromDate.Date).TotalDays + 1;
                    if (span > MaxDaysPerRequest)
                    {
                        ModelState.AddModelError("ToDate",
                            string.Format("Mỗi đơn tối đa {0} ngày. Nghỉ dài hơn thì tách thành nhiều đơn.",
                                MaxDaysPerRequest));
                    }

                    // Nửa ngày chỉ có nghĩa khi đợt nghỉ gói trong đúng một ngày — nhiều ngày mà
                    // đánh dấu nửa ngày thì không rõ nửa ngày nào.
                    if (model.IsHalfDay && model.ToDate.Date != model.FromDate.Date)
                    {
                        ModelState.AddModelError("IsHalfDay",
                            "Nghỉ nửa ngày chỉ áp dụng khi ngày bắt đầu và kết thúc là cùng một ngày.");
                    }

                    // Nghỉ trọn Thứ 7 + Chủ nhật thì không có ngày công nào bị trừ — nhận đơn kiểu
                    // này chỉ làm rối danh sách duyệt.
                    if (ModelState.IsValid && model.Days <= 0)
                    {
                        ModelState.AddModelError("FromDate",
                            "Khoảng ngày đã chọn không có ngày làm việc nào (chỉ rơi vào Thứ 7 hoặc Chủ nhật).");
                    }

                    if (ModelState.IsValid)
                    {
                        var overlap = LeaveService.FindOverlap(
                            CurrentUserId, model.FromDate, model.ToDate, model.Id);

                        if (overlap != null)
                        {
                            ModelState.AddModelError("FromDate", string.Format(
                                "Bạn đã có đơn {0} từ {1:dd/MM/yyyy} đến {2:dd/MM/yyyy} trùng khoảng ngày này.",
                                LeaveStates.Display(overlap.State).ToLowerInvariant(),
                                overlap.FromDate, overlap.ToDate));
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Kind) && !LeaveKinds.All.Contains(model.Kind))
            {
                ModelState.AddModelError("Kind", "Loại nghỉ không hợp lệ.");
            }
        }

        /// <summary>Chép phần người dùng nhập được sang bản ghi thật, bỏ qua mọi trường trạng thái.</summary>
        private static void CopyInput(LeaveRequest from, LeaveRequest to)
        {
            to.Kind = string.IsNullOrWhiteSpace(from.Kind) ? LeaveKinds.Annual : from.Kind;
            to.FromDate = from.FromDate.Date;
            to.ToDate = from.ToDate.Date;
            to.IsHalfDay = from.IsHalfDay;
            to.HalfDaySession = from.IsHalfDay ? from.HalfDaySession : null;
            to.Reason = string.IsNullOrWhiteSpace(from.Reason) ? null : from.Reason.Trim();
        }

        /// <summary>Báo cho những người có quyền duyệt là có đơn mới.</summary>
        private static void NotifyApprovers(LeaveRequest leave)
        {
            try
            {
                var message = string.Format("{0} xin nghỉ {1:0.#} ngày ({2:dd/MM} – {3:dd/MM/yyyy}).",
                    leave.UserFullName, leave.Days, leave.FromDate, leave.ToDate);

                foreach (var user in WorkService.ActiveUsers())
                {
                    if (user.Id == leave.UserId) continue;
                    if (!Permissions.UserHas(user.Role, Permissions.Leaves.Perm("approve"))) continue;

                    NotificationService.Add(user.Id, "leave.request", message);
                }
            }
            catch (Exception)
            {
                // Gửi thông báo hỏng thì đơn vẫn phải được lưu — người duyệt còn vào màn duyệt xem được.
            }
        }

        /// <summary>Báo cho người xin nghỉ biết kết quả duyệt.</summary>
        private static void NotifyRequester(LeaveRequest leave)
        {
            try
            {
                var message = leave.IsApproved
                    ? string.Format("Đơn nghỉ {0:dd/MM} – {1:dd/MM/yyyy} đã được duyệt.",
                        leave.FromDate, leave.ToDate)
                    : string.Format("Đơn nghỉ {0:dd/MM} – {1:dd/MM/yyyy} bị từ chối: {2}",
                        leave.FromDate, leave.ToDate, leave.ApproverNote);

                NotificationService.Add(leave.UserId, "leave.result", message);
            }
            catch (Exception)
            {
                // Như trên: thông báo hỏng không được làm hỏng thao tác duyệt.
            }
        }
    }
}
