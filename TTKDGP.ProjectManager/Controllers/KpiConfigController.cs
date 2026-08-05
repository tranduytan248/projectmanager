using System;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;
using TTKDGP.ProjectManager.Services;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Sửa công thức chấm KPI: trọng số các nhóm việc, mức trừ báo cáo trễ, ngày công và ngưỡng
    /// xếp loại. Xem <see cref="KpiConfigService"/> về cách lưu và phương án dự phòng.
    ///
    /// Sửa ở đây KHÔNG tự chấm lại điểm đã lưu: dòng KPI là ảnh chụp tại lần tính gần nhất. Muốn
    /// áp dụng cho tháng đã chốt thì vào màn KPI bấm "Tính &amp; lưu" lại — cố ý làm vậy để một cú
    /// đổi trọng số không âm thầm viết lại điểm của mọi tháng trong quá khứ.
    /// </summary>
    public class KpiConfigController : BaseController
    {
        [AppAuthorize(Permission = "kpi.config")]
        public ActionResult Index()
        {
            FillViewBag();
            return View(KpiConfigService.Current);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "kpi.config")]
        public ActionResult Index(KpiConfig form)
        {
            var error = Validate(form);
            if (error != null)
            {
                NotifyError(error);
                FillViewBag();
                return View(form);
            }

            var saved = KpiConfigService.Save(form, CurrentUser == null ? null : CurrentUser.FullName);

            Notify(string.Format(
                "Đã lưu công thức KPI: hỗ trợ tối đa {0:0.##}, thực hiện tối đa {1:0.##}, " +
                "một ngày công {2:0.##} giờ. Vào màn KPI theo tháng bấm \"Tính & lưu\" để áp dụng.",
                saved.SupportMaxPoint, saved.ExecuteMaxPoint, saved.HoursPerDay));

            return RedirectToAction("Index");
        }

        /// <summary>Đưa toàn bộ tham số về mặc định của hệ thống.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AppAuthorize(Permission = "kpi.config")]
        public ActionResult Reset()
        {
            KpiConfigService.ResetToDefaults(CurrentUser == null ? null : CurrentUser.FullName);
            Notify("Đã khôi phục công thức KPI về mặc định của hệ thống.");

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Kiểm các giá trị người dùng nhập. Trả về thông báo lỗi, hoặc null nếu hợp lệ.
        ///
        /// Kiểm ở đây chứ không chỉ dựa vào thuộc tính min/max của ô nhập: người dùng gửi thẳng
        /// biểu mẫu là qua hết. Riêng phần chặn giá trị vô lý ở tầng dưới (KpiConfigService)
        /// là lưới an toàn cho dữ liệu cũ, không thay cho lớp này — nó âm thầm sửa giá trị, còn
        /// ở đây phải nói cho người nhập biết họ sai chỗ nào.
        /// </summary>
        private static string Validate(KpiConfig f)
        {
            if (f.SupportMaxPoint < 0 || f.ExecuteMaxPoint < 0)
            {
                return "Điểm tối đa của mỗi nhóm việc không được là số âm.";
            }

            if (f.SupportMaxPoint + f.ExecuteMaxPoint <= 0)
            {
                return "Tổng điểm tối đa của hai nhóm phải lớn hơn 0 — bằng 0 thì mọi người đều 0 điểm.";
            }

            if (f.ExecuteHoursShare <= 0 || f.ExecuteHoursShare > 1)
            {
                return "Định mức giờ nhóm Thực hiện phải nằm trong khoảng 0 đến 1 " +
                       "(0,7 nghĩa là 70% giờ yêu cầu của tháng).";
            }

            if (f.SupportLateFreeCount < 0)
            {
                return "Số lần trễ được bỏ qua không được là số âm.";
            }

            if (f.SupportLate2Penalty < 0 || f.SupportLateMorePenalty < 0 || f.ExecuteLatePenalty < 0)
            {
                return "Mức trừ khi báo cáo trễ không được là số âm.";
            }

            if (f.HoursPerDay <= 0 || f.HoursPerDay > 24)
            {
                return "Số giờ của một ngày công phải nằm trong khoảng 0 đến 24.";
            }

            if (!(f.RankExcellent > f.RankGood && f.RankGood > f.RankFair && f.RankFair > f.RankPass))
            {
                return "Ngưỡng xếp loại phải giảm dần: Xuất sắc > Tốt > Khá > Đạt.";
            }

            if (f.RankPass < 0)
            {
                return "Ngưỡng xếp loại không được là số âm.";
            }

            return null;
        }

        private void FillViewBag()
        {
            ViewBag.Defaults = KpiConfigService.Defaults();
            ViewBag.IsCustomized = KpiConfigService.IsCustomized;

            // Tháng hiện tại, để nút "Đi tới màn KPI" mở đúng kỳ đang chấm.
            ViewBag.Year = DateTime.Today.Year;
            ViewBag.Month = DateTime.Today.Month;
        }
    }
}
