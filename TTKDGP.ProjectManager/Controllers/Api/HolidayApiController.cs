using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models.Api;
using TTKDGP.ProjectManager.Models.Work;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers.Api
{
    /// <summary>
    /// API Tra cứu và quản lý Ngày nghỉ lễ cho ứng dụng di động BrewTask.
    /// </summary>
    [ApiAuthorize]
    public class HolidayApiController : BaseController
    {
        [HttpGet]
        public ActionResult Index(int? year)
        {
            var selectedYear = year ?? DateTime.Now.Year;
            var list = HolidayService.ForYear(selectedYear);

            var dtos = list.Select(h => new HolidayDto
            {
                Id = h.Id,
                Name = h.Name,
                Kind = (int)h.Kind,
                KindName = h.KindDisplay,
                Day = h.Day,
                Month = h.Month,
                SpecificDate = h.SpecificDate,
                Year = h.Year,
                Note = h.Note,
                IsActive = h.IsActive,
                DisplayDate = h.DisplayDate
            }).ToList();

            var result = new HolidayListDto
            {
                Year = selectedYear,
                TotalCount = dtos.Count,
                Holidays = dtos
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Create(HolidayDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên ngày nghỉ lễ." });
            }

            var model = new Holiday
            {
                Name = req.Name.Trim(),
                Kind = (HolidayKind)req.Kind,
                Day = req.Day,
                Month = req.Month,
                SpecificDate = req.SpecificDate,
                Year = req.Year,
                Note = req.Note,
                IsActive = req.IsActive,
                CreatedAt = DateTime.Now
            };

            HolidayService.Save(model);

            return Json(new { success = true, id = model.Id, message = "Thêm ngày nghỉ lễ thành công." });
        }

        [HttpPost]
        public ActionResult Update(HolidayDto req)
        {
            if (req == null || req.Id <= 0 || string.IsNullOrWhiteSpace(req.Name))
            {
                return Json(new { success = false, message = "Thông tin ngày nghỉ lễ không hợp lệ." });
            }

            var existing = HolidayService.Find(req.Id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Không tìm thấy ngày nghỉ lễ cần sửa." });
            }

            existing.Name = req.Name.Trim();
            existing.Kind = (HolidayKind)req.Kind;
            existing.Day = req.Day;
            existing.Month = req.Month;
            existing.SpecificDate = req.SpecificDate;
            existing.Year = req.Year;
            existing.Note = req.Note;
            existing.IsActive = req.IsActive;

            HolidayService.Save(existing);

            return Json(new { success = true, id = existing.Id, message = "Cập nhật ngày nghỉ lễ thành công." });
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            var existing = HolidayService.Find(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Không tìm thấy ngày nghỉ lễ cần xoá." });
            }

            HolidayService.Delete(id);
            return Json(new { success = true, message = "Xoá ngày nghỉ lễ thành công." });
        }
    }
}
