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
    /// "Đăng ký nghỉ phép" của mobile — tương đương LeavesController.My/Edit/Cancel bên web, dùng
    /// lại NGUYÊN VẸN logic/luật đã có (LeaveService, LeaveRequest.Days, cùng bộ kiểm dữ liệu
    /// trong LeavesController.Validate), chỉ đổi cách trả kết quả (JSON thay vì redirect/
    /// PartialView) để mobile tự cập nhật màn hình.
    /// </summary>
    [ApiAuthorize]
    public class LeavesApiController : BaseController
    {
        /// <summary>Khớp LeavesController.MaxDaysPerRequest.</summary>
        private const int MaxDaysPerRequest = 60;

        [HttpGet]
        public ActionResult Index(int year = 0, string state = null)
        {
            var all = LeaveService.OfUser(CurrentUserId);
            var items = all.AsEnumerable();

            if (year > 0) items = items.Where(l => l.FromDate.Year == year || l.ToDate.Year == year);
            if (!string.IsNullOrWhiteSpace(state)) items = items.Where(l => l.State == state);

            // Nam thong ke: nam dang loc, hoac nam hien tai neu xem "Tat ca" — y het LeavesController.My.
            var statYear = year > 0 ? year : DateTime.Today.Year;

            var dto = new LeavesListDto
            {
                StatYear = statYear,
                ApprovedDaysInYear = all
                    .Where(l => l.IsApproved)
                    .Sum(l => l.DaysInRange(new DateTime(statYear, 1, 1), new DateTime(statYear, 12, 31))),
                PendingCount = all.Count(l => l.State == LeaveStates.Pending),
                Years = all.Select(l => l.FromDate.Year).Distinct().OrderByDescending(y => y).ToList(),
                Items = items.Select(ApiMappers.ToDetailDto).ToList()
            };

            return Json(dto, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Create(string kind, DateTime? fromDate, DateTime? toDate,
            bool isHalfDay, string halfDaySession, string reason)
        {
            var error = ValidateLeave(kind, fromDate, toDate, isHalfDay, 0);
            if (error != null) return BadRequest(error);

            var leave = new LeaveRequest
            {
                UserId = CurrentUserId,
                UserFullName = CurrentUser == null ? null : CurrentUser.FullName,
                State = LeaveStates.Pending,
                CreatedAt = DateTime.Now
            };
            CopyInput(leave, kind, fromDate.Value, toDate.Value, isHalfDay, halfDaySession, reason);

            var saved = Repository.LeaveRequests.Insert(leave);
            NotifyApprovers(saved);

            return Json(ApiMappers.ToDetailDto(saved));
        }

        [HttpPost]
        public ActionResult Update(int id, string kind, DateTime? fromDate, DateTime? toDate,
            bool isHalfDay, string halfDaySession, string reason)
        {
            var current = Repository.LeaveRequests.Find(id);
            if (current == null || current.UserId != CurrentUserId) return HttpNotFound();

            if (!LeaveStates.IsEditable(current.State))
            {
                return BadRequest("Đơn đã được xử lý nên không sửa được nữa.");
            }

            var error = ValidateLeave(kind, fromDate, toDate, isHalfDay, id);
            if (error != null) return BadRequest(error);

            CopyInput(current, kind, fromDate.Value, toDate.Value, isHalfDay, halfDaySession, reason);
            current.UpdatedAt = DateTime.Now;
            Repository.LeaveRequests.Update(current);

            return Json(ApiMappers.ToDetailDto(current));
        }

        /// <summary>Thu hồi đơn của chính mình khi chưa được duyệt — y hệt LeavesController.Cancel.</summary>
        [HttpPost]
        public ActionResult Cancel(int id)
        {
            var leave = Repository.LeaveRequests.Find(id);
            if (leave == null || leave.UserId != CurrentUserId) return HttpNotFound();

            if (!LeaveStates.IsEditable(leave.State))
            {
                return BadRequest("Đơn đã được xử lý nên không thu hồi được nữa.");
            }

            leave.State = LeaveStates.Cancelled;
            leave.UpdatedAt = DateTime.Now;
            Repository.LeaveRequests.Update(leave);

            return Json(ApiMappers.ToDetailDto(leave));
        }

        /// <summary>Chép phần người dùng nhập được sang bản ghi thật — y hệt LeavesController.CopyInput.</summary>
        private static void CopyInput(LeaveRequest to, string kind, DateTime fromDate, DateTime toDate,
            bool isHalfDay, string halfDaySession, string reason)
        {
            to.Kind = string.IsNullOrWhiteSpace(kind) ? LeaveKinds.Annual : kind;
            to.FromDate = fromDate.Date;
            to.ToDate = toDate.Date;
            to.IsHalfDay = isHalfDay;
            to.HalfDaySession = isHalfDay ? halfDaySession : null;
            to.Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        }

        /// <summary>
        /// Kiểm dữ liệu một đơn — y hệt thứ tự kiểm ở LeavesController.Validate, chỉ đổi cách báo
        /// lỗi: trả về THÔNG BÁO ĐẦU TIÊN gặp phải (dạng BadRequest) thay vì gom cả ModelState,
        /// khớp quy ước các API khác trong dự án (MyProjectsApiController, ChecklistApiController).
        /// </summary>
        private string ValidateLeave(string kind, DateTime? fromDate, DateTime? toDate, bool isHalfDay,
            int excludeId)
        {
            if (!fromDate.HasValue) return "Vui lòng chọn ngày bắt đầu nghỉ.";
            if (!toDate.HasValue) return "Vui lòng chọn ngày kết thúc nghỉ.";

            if (toDate.Value.Date < fromDate.Value.Date)
            {
                return "Ngày kết thúc phải từ ngày bắt đầu trở đi.";
            }

            var span = (toDate.Value.Date - fromDate.Value.Date).TotalDays + 1;
            if (span > MaxDaysPerRequest)
            {
                return string.Format("Mỗi đơn tối đa {0} ngày. Nghỉ dài hơn thì tách thành nhiều đơn.",
                    MaxDaysPerRequest);
            }

            if (isHalfDay && toDate.Value.Date != fromDate.Value.Date)
            {
                return "Nghỉ nửa ngày chỉ áp dụng khi ngày bắt đầu và kết thúc là cùng một ngày.";
            }

            // Muon mot LeaveRequest tam de tai dung dung logic tinh Days (bo Thu 7/Chu nhat, nua
            // ngay = 0.5) — khong phai truy DB, chi can 3 field da co tren tay.
            var probe = new LeaveRequest { FromDate = fromDate.Value, ToDate = toDate.Value, IsHalfDay = isHalfDay };
            if (probe.Days <= 0)
            {
                return "Khoảng ngày đã chọn không có ngày làm việc nào (chỉ rơi vào Thứ 7 hoặc Chủ nhật).";
            }

            var overlap = LeaveService.FindOverlap(CurrentUserId, fromDate.Value, toDate.Value, excludeId);
            if (overlap != null)
            {
                return string.Format(
                    "Bạn đã có đơn {0} từ {1:dd/MM/yyyy} đến {2:dd/MM/yyyy} trùng khoảng ngày này.",
                    LeaveStates.Display(overlap.State).ToLowerInvariant(), overlap.FromDate, overlap.ToDate);
            }

            if (!string.IsNullOrWhiteSpace(kind) && !LeaveKinds.All.Contains(kind))
            {
                return "Loại nghỉ không hợp lệ.";
            }

            return null;
        }

        /// <summary>Báo cho những người có quyền duyệt là có đơn mới — y hệt LeavesController.NotifyApprovers.</summary>
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

                    NotificationService.Add(user.Id, NotificationTypes.LeaveRequested, message);
                }
            }
            catch (Exception)
            {
                // Gui thong bao hong thi don van phai duoc luu — nguoi duyet con vao man duyet xem duoc.
            }
        }

        // ================= Phần Duyệt nghỉ phép (Quản lý Tổ) =================

        private bool CanApproveLeaves => Can(Permissions.Leaves.Perm("approve")) || IsTeamManager || Can("*");

        [HttpGet]
        public ActionResult Approvals(string q = null, int userId = 0, string state = null,
            int year = 0, int month = 0)
        {
            if (!CanApproveLeaves) return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ mới có quyền duyệt nghỉ phép.");

            // Mặc định là đơn chờ duyệt nếu chưa chọn trạng thái (state == null)
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

            var members = WorkService.ActiveUsers()
                .Select(u => new AssigneeOptionDto { UserId = u.Id, FullName = u.FullName })
                .ToList();

            var years = all
                .Select(l => l.FromDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            var result = new LeaveApprovalsDataDto
            {
                TotalCount = all.Count,
                PendingCount = all.Count(l => l.State == LeaveStates.Pending),
                ApprovedCount = all.Count(l => l.State == LeaveStates.Approved),
                RejectedCount = all.Count(l => l.State == LeaveStates.Rejected),
                Years = years,
                Members = members,
                Items = list.Select(ApiMappers.ToDetailDto).ToList()
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SetState(int id, string state, string note)
        {
            if (!CanApproveLeaves) return new HttpStatusCodeResult(403, "Chỉ Quản lý Tổ mới có quyền duyệt nghỉ phép.");

            var leave = Repository.LeaveRequests.Find(id);
            if (leave == null) return HttpNotFound();

            if (state != LeaveStates.Approved && state != LeaveStates.Rejected)
            {
                return BadRequest("Trạng thái không hợp lệ.");
            }

            if (state == LeaveStates.Rejected && string.IsNullOrWhiteSpace(note))
            {
                return BadRequest("Vui lòng nhập lý do từ chối để người đăng ký nắm được.");
            }

            leave.State = state;
            leave.ApprovedByUserId = CurrentUserId;
            leave.ApprovedByName = CurrentUser == null ? null : CurrentUser.FullName;
            leave.ApprovedAt = DateTime.Now;
            leave.ApproverNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            leave.UpdatedAt = DateTime.Now;
            Repository.LeaveRequests.Update(leave);

            NotifyRequester(leave);

            return Json(ApiMappers.ToDetailDto(leave));
        }

        /// <summary>Báo cho người xin nghỉ biết kết quả duyệt — y hệt LeavesController.NotifyRequester.</summary>
        private static void NotifyRequester(LeaveRequest leave)
        {
            try
            {
                var message = leave.IsApproved
                    ? string.Format("Đơn nghỉ {0:dd/MM} – {1:dd/MM/yyyy} đã được duyệt.",
                        leave.FromDate, leave.ToDate)
                    : string.Format("Đơn nghỉ {0:dd/MM} – {1:dd/MM/yyyy} bị từ chối: {2}",
                        leave.FromDate, leave.ToDate, leave.ApproverNote);

                NotificationService.Add(leave.UserId, NotificationTypes.LeaveResult, message);
            }
            catch (Exception)
            {
                // Thong bao hong khong lam hong thao tac duyet.
            }
        }
    }
}
