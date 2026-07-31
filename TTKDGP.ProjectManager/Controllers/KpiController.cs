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
    /// Chấm KPI theo tháng cho từng thành viên trong tổ — xem <see cref="KpiService"/> về công thức.
    /// Ngoài màn hình còn mở bộ API JSON (/api/kpi...) dùng chung phiên đăng nhập.
    /// </summary>
    public class KpiController : BaseController
    {
        // ---------- Danh sách theo tháng ----------

        [AppAuthorize(Permission = "kpi.view")]
        public ActionResult Index(int? year, int? month, int? userId)
        {
            var y = year ?? DateTime.Today.Year;
            var m = month ?? DateTime.Today.Month;

            var rows = BuildRows(y, m, userId);

            ViewBag.Year = y;
            ViewBag.Month = m;
            ViewBag.UserId = userId ?? 0;
            ViewBag.Users = WorkService.ActiveUsers();
            ViewBag.StandardDays = KpiService.StandardWorkingDays(y, m);

            // Còn dòng tạm tính (Id = 0) nghĩa là tháng này chưa bấm "Tính KPI" cho đủ mọi người.
            ViewBag.HasPreview = rows.Any(r => r.Id == 0);

            return View(rows);
        }

        /// <summary>Tính KPI cho toàn bộ nhân viên trong tháng.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "kpi.generate")]
        public ActionResult Calculate(int year, int month)
        {
            var rows = KpiService.CalculateAll(year, month);
            Notify(string.Format("Đã tính KPI tháng {0:00}/{1} cho {2} nhân sự.", month, year, rows.Count));
            return RedirectToAction("Index", new { year = year, month = month });
        }

        /// <summary>Tính lại KPI của một nhân viên rồi quay về trang chi tiết.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "kpi.generate")]
        public ActionResult Recalculate(int year, int month, int userId)
        {
            var user = Repository.Users.Find(userId);
            if (user == null) return HttpNotFound();

            KpiService.CalculateUser(year, month, user);
            Notify(string.Format("Đã tính lại KPI của {0}.", user.FullName));
            return RedirectToAction("Detail", new { year = year, month = month, userId = userId });
        }

        /// <summary>
        /// Nạp lại số ngày nghỉ phép của một người từ các đơn ĐÃ DUYỆT rồi tính lại KPI.
        ///
        /// Trước đây đây là ô nhập tay. Nay số ngày nghỉ lấy thẳng từ màn Duyệt nghỉ phép nên
        /// hành động duy nhất còn lại là "tính lại cho khớp" — giữ endpoint để nút trên màn KPI
        /// và các liên kết cũ không gãy.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "kpi.generate")]
        public ActionResult Attendance(int year, int month, int userId, string back = null)
        {
            var user = Repository.Users.Find(userId);
            if (user == null) return HttpNotFound();

            // Lưu từ dòng trong danh sách thì quay về danh sách, từ trang chi tiết thì ở lại đó.
            var backToIndex = string.Equals(back, "index", StringComparison.OrdinalIgnoreCase);
            var backResult = backToIndex
                ? RedirectToAction("Index", new { year = year, month = month })
                : RedirectToAction("Detail", new { year = year, month = month, userId = userId });

            var row = KpiService.CalculateUser(year, month, user);

            Notify(string.Format("Đã tính lại KPI của {0} — ghi nhận {1:0.#} ngày nghỉ phép đã duyệt.",
                user.FullName, row.LeaveDays));

            return backResult;
        }

        // ---------- Chi tiết một người ----------

        [AppAuthorize(Permission = "kpi.view")]
        public ActionResult Detail(int year, int month, int userId)
        {
            var user = Repository.Users.Find(userId);
            if (user == null) return HttpNotFound();

            var row = Repository.KpiMonths.FirstOrDefault(
                k => k.Year == year && k.Month == month && k.UserId == userId);

            var tasks = KpiService.TasksOfUserInMonth(userId, year, month);

            // Chưa bấm "Tính KPI" thì vẫn xem trước được: tính trong bộ nhớ, không lưu gì xuống.
            var saved = row != null;
            if (row == null)
            {
                row = new KpiMonth { Year = year, Month = month, UserId = userId, UserFullName = user.FullName };
                KpiService.Fill(row, tasks);
            }

            ViewBag.IsSaved = saved;
            ViewBag.SupportTasks = tasks.Where(t => t.Kind == TaskKinds.Support)
                .OrderBy(t => t.DueDate).ToList();
            ViewBag.ExecuteTasks = tasks.Where(t => t.Kind == TaskKinds.Checklist)
                .OrderBy(t => t.DueDate).ToList();
            ViewBag.AssignedTasks = tasks.Where(t => t.Kind == TaskKinds.Standalone)
                .OrderBy(t => t.DueDate).ToList();

            return View(row);
        }

        // ---------- Xuất Excel ----------

        [AppAuthorize(Permission = "kpi.export")]
        public ActionResult Export(int year, int month, int? userId)
        {
            // Xuất đúng những gì đang thấy trên màn hình, kể cả dòng tạm tính.
            var rows = BuildRows(year, month, userId);

            var sheet = new SheetData(string.Format("KPI {0:00}-{1}", month, year),
                "STT", "Nhân sự", "Hỗ trợ", "Thực hiện", "Được giao",
                "KPI chất lượng", "Tỷ lệ ngày công (%)", "KPI cuối cùng", "Xếp loại");

            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                sheet.Add(i + 1, r.UserFullName, r.SupportPoint, r.ExecutePoint, r.AssignedPoint,
                    r.QualityPoint, r.AttendanceRate, r.FinalPoint, r.Rank);
            }

            var name = string.Format("KPI-thang-{0:00}-{1}{2}", month, year, SpreadsheetFile.Extension);
            return File(SpreadsheetFile.Build(sheet), SpreadsheetFile.ContentType, name);
        }

        // ---------- API JSON (routes /api/kpi... khai ở RouteConfig) ----------

        /// <summary>GET /api/kpi?year=&amp;month= — danh sách KPI theo tháng.</summary>
        [AppAuthorize(Permission = "kpi.view")]
        public ActionResult ApiList(int? year, int? month)
        {
            var y = year ?? DateTime.Today.Year;
            var m = month ?? DateTime.Today.Month;

            return Json(BuildRows(y, m, null).Select(ApiRow).ToList(), JsonRequestBehavior.AllowGet);
        }

        /// <summary>GET /api/kpi/{userId}?year=&amp;month= — chi tiết KPI một người.</summary>
        [AppAuthorize(Permission = "kpi.view")]
        public ActionResult ApiDetail(int userId, int? year, int? month)
        {
            var y = year ?? DateTime.Today.Year;
            var m = month ?? DateTime.Today.Month;

            var row = Repository.KpiMonths.FirstOrDefault(
                k => k.Year == y && k.Month == m && k.UserId == userId);
            if (row == null) return HttpNotFound();

            return Json(ApiRow(row), JsonRequestBehavior.AllowGet);
        }

        /// <summary>POST /api/kpi/calculate — tính KPI cho toàn bộ nhân viên trong tháng.</summary>
        [HttpPost]
        [AppAuthorize(Permission = "kpi.generate")]
        public ActionResult ApiCalculate(int year, int month)
        {
            var rows = KpiService.CalculateAll(year, month);
            return Json(new { ok = true, count = rows.Count });
        }

        /// <summary>POST /api/kpi/recalculate — tính lại KPI của một nhân viên.</summary>
        [HttpPost]
        [AppAuthorize(Permission = "kpi.generate")]
        public ActionResult ApiRecalculate(int year, int month, int userId)
        {
            var user = Repository.Users.Find(userId);
            if (user == null) return HttpNotFound();

            var row = KpiService.CalculateUser(year, month, user);
            return Json(new { ok = true, kpi = ApiRow(row) });
        }

        // ---------- Dùng chung ----------

        /// <summary>
        /// Các dòng KPI hiển thị của một tháng: dòng đã lưu, cộng thêm dòng TẠM TÍNH (Id = 0,
        /// tính trong bộ nhớ, không ghi gì xuống) cho tài khoản đang kích hoạt chưa được chốt —
        /// nhờ vậy mở màn hình là thấy ngay số liệu, không phải bấm "Tính KPI" trước.
        /// </summary>
        private static List<KpiMonth> BuildRows(int year, int month, int? userId)
        {
            var rows = Repository.KpiMonths.All()
                .Where(k => k.Year == year && k.Month == month
                            && (!userId.HasValue || userId.Value <= 0 || k.UserId == userId.Value))
                .ToList();

            var savedUserIds = new HashSet<int>(rows.Select(r => r.UserId));

            foreach (var user in WorkService.ActiveUsers())
            {
                if (userId.HasValue && userId.Value > 0 && user.Id != userId.Value) continue;
                if (savedUserIds.Contains(user.Id)) continue;

                var preview = new KpiMonth
                {
                    Year = year,
                    Month = month,
                    UserId = user.Id,
                    UserFullName = user.FullName
                };
                KpiService.Fill(preview, KpiService.TasksOfUserInMonth(user.Id, year, month));
                rows.Add(preview);
            }

            return rows
                .OrderByDescending(k => k.FinalPoint)
                .ThenBy(k => k.UserFullName, StringComparer.CurrentCulture)
                .ToList();
        }

        private static object ApiRow(KpiMonth r)
        {
            return new
            {
                userId = r.UserId,
                fullName = r.UserFullName,
                year = r.Year,
                month = r.Month,
                supportPoint = r.SupportPoint,
                executePoint = r.ExecutePoint,
                assignedPoint = r.AssignedPoint,
                qualityPoint = r.QualityPoint,
                standardDays = r.StandardDays,
                leaveDays = r.LeaveDays,
                requiredHours = r.RequiredHours,
                workingHours = r.WorkingHours,
                attendanceRate = r.AttendanceRate,
                finalPoint = r.FinalPoint,
                rank = r.Rank
            };
        }
    }
}
