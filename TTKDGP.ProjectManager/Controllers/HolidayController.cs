using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Models.Work;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Quản lý danh mục Ngày nghỉ lễ (Lễ cố định hàng năm và Nghỉ bù / Tự khai báo).
    /// </summary>
    public class HolidayController : BaseController
    {
        [AppAuthorize(Permission = "holiday.view")]
        public ActionResult Index(int? year)
        {
            var selectedYear = year ?? DateTime.Now.Year;
            var allHolidays = HolidayService.All();
            var holidaysForYear = HolidayService.ForYear(selectedYear);

            // Thống kê số ngày nghỉ lễ theo từng tháng trong năm được chọn
            var monthlyHolidays = new Dictionary<int, List<Holiday>>();
            for (int m = 1; m <= 12; m++)
            {
                monthlyHolidays[m] = HolidayService.InMonth(selectedYear, m);
            }

            // Danh sách các năm để lọc
            var years = allHolidays
                .Where(h => h.Year.HasValue)
                .Select(h => h.Year.Value)
                .Concat(new[] { DateTime.Now.Year - 1, DateTime.Now.Year, DateTime.Now.Year + 1 })
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            ViewBag.SelectedYear = selectedYear;
            ViewBag.Years = years;
            ViewBag.MonthlyHolidays = monthlyHolidays;
            ViewBag.AllHolidays = allHolidays;

            return View(holidaysForYear);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "holiday.create")]
        public ActionResult Create(Holiday model, string specificDateStr)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Vui lòng nhập tên ngày nghỉ lễ.";
                return RedirectToAction("Index", new { year = model.Year ?? DateTime.Now.Year });
            }

            if (model.Kind == HolidayKind.Compensatory || model.Kind == HolidayKind.Custom)
            {
                DateTime parsedDate;
                if (!string.IsNullOrWhiteSpace(specificDateStr) &&
                    DateTime.TryParseExact(specificDateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out parsedDate))
                {
                    model.SpecificDate = parsedDate;
                    model.Day = parsedDate.Day;
                    model.Month = parsedDate.Month;
                    model.Year = parsedDate.Year;
                }
                else if (!model.SpecificDate.HasValue)
                {
                    TempData["Error"] = "Vui lòng chọn ngày nghỉ cụ thể.";
                    return RedirectToAction("Index", new { year = model.Year ?? DateTime.Now.Year });
                }
            }
            else
            {
                // Cố định hàng năm
                if (!model.Day.HasValue || !model.Month.HasValue || model.Day < 1 || model.Day > 31 || model.Month < 1 || model.Month > 12)
                {
                    TempData["Error"] = "Vui lòng chọn ngày và tháng hợp lệ.";
                    return RedirectToAction("Index", new { year = model.Year ?? DateTime.Now.Year });
                }
            }

            HolidayService.Save(model);
            TempData["Success"] = string.Format("Đã thêm ngày nghỉ lễ \"{0}\".", model.Name);

            return RedirectToAction("Index", new { year = model.Year ?? DateTime.Now.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "holiday.edit")]
        public ActionResult Edit(Holiday model, string specificDateStr)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Vui lòng nhập tên ngày nghỉ lễ.";
                return RedirectToAction("Index", new { year = model.Year ?? DateTime.Now.Year });
            }

            var existing = HolidayService.Find(model.Id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy ngày nghỉ lễ cần sửa.";
                return RedirectToAction("Index", new { year = model.Year ?? DateTime.Now.Year });
            }

            existing.Name = model.Name;
            existing.Kind = model.Kind;
            existing.Note = model.Note;
            existing.IsActive = model.IsActive;

            if (model.Kind == HolidayKind.Compensatory || model.Kind == HolidayKind.Custom)
            {
                DateTime parsedDate;
                if (!string.IsNullOrWhiteSpace(specificDateStr) &&
                    DateTime.TryParseExact(specificDateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out parsedDate))
                {
                    existing.SpecificDate = parsedDate;
                    existing.Day = parsedDate.Day;
                    existing.Month = parsedDate.Month;
                    existing.Year = parsedDate.Year;
                }
                else if (model.SpecificDate.HasValue)
                {
                    existing.SpecificDate = model.SpecificDate;
                    existing.Day = model.SpecificDate.Value.Day;
                    existing.Month = model.SpecificDate.Value.Month;
                    existing.Year = model.SpecificDate.Value.Year;
                }
            }
            else
            {
                existing.Day = model.Day;
                existing.Month = model.Month;
                existing.Year = null; // Áp dụng mọi năm
                existing.SpecificDate = null;
            }

            HolidayService.Save(existing);
            TempData["Success"] = string.Format("Đã cập nhật ngày nghỉ lễ \"{0}\".", existing.Name);

            return RedirectToAction("Index", new { year = existing.Year ?? DateTime.Now.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "holiday.delete")]
        public ActionResult Delete(int id, int? returnYear)
        {
            var item = HolidayService.Find(id);
            if (item != null)
            {
                HolidayService.Delete(id);
                TempData["Success"] = string.Format("Đã xoá ngày nghỉ lễ \"{0}\".", item.Name);
            }

            return RedirectToAction("Index", new { year = returnYear ?? DateTime.Now.Year });
        }

        [HttpPost]
        [AppAuthorize(Permission = "holiday.edit")]
        public ActionResult ToggleActive(int id)
        {
            var newState = HolidayService.ToggleActive(id);
            return Json(new { success = true, isActive = newState });
        }
    }
}
