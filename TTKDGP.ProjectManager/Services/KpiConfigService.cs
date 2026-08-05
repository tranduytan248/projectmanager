using System;
using System.Web;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Đọc/ghi bộ tham số công thức KPI. Nguồn chính là bảng KpiConfigs (sửa được trên màn hình);
    /// chưa có dòng nào — hoặc SQL không tới được — thì lùi về giá trị trong Web.config.
    ///
    /// KHÔNG BAO GIỜ ném lỗi ra ngoài: mọi màn hình có dính tới KPI đều gọi qua đây, để lọt một
    /// lỗi kết nối là cả trang trắng. Thà chấm bằng tham số mặc định còn hơn không mở được màn.
    /// </summary>
    public static class KpiConfigService
    {
        /// <summary>
        /// Bản đang áp dụng, nhớ trong phạm vi MỘT request. Một lượt mở trang KPI hỏi tham số hàng
        /// trăm lần (mỗi đầu việc một lần), mà mỗi lần hỏi lại quét bảng và đọc lại Web.config.
        ///
        /// Nhớ theo request là mức an toàn nhất: sửa cấu hình có hiệu lực ngay ở lượt xem kế tiếp,
        /// không phải chờ hết hạn bộ đệm.
        /// </summary>
        public static KpiConfig Current
        {
            get
            {
                var context = HttpContext.Current;
                const string cacheKey = "TTKDGP:KpiConfig";

                if (context != null)
                {
                    var cached = context.Items[cacheKey] as KpiConfig;
                    if (cached != null) return cached;
                }

                var config = Load();

                if (context != null) context.Items[cacheKey] = config;
                return config;
            }
        }

        /// <summary>Dòng đã lưu, hoặc bản mặc định nếu chưa có/không đọc được.</summary>
        private static KpiConfig Load()
        {
            try
            {
                var all = Repository.KpiConfigs.All();

                // Chép ra bản riêng rồi mới nắn: SqlStore trả về chính đối tượng đang giữ trong
                // bộ nhớ, nắn thẳng lên đó là sửa ngầm bản của store mà CSDL vẫn giữ giá trị cũ.
                if (all.Count > 0) return Sanitize(Copy(all[0]));
            }
            catch (Exception ex)
            {
                // SQL chập chờn: chấm bằng tham số mặc định. Có ghi lại — nuốt lỗi hoàn toàn thì
                // điểm bỗng đổi mà không ai lần ra vì sao.
                System.Diagnostics.Debug.WriteLine(
                    "Đọc cấu hình KPI trượt, dùng mặc định: " + ex.Message);
            }

            return Defaults();
        }

        /// <summary>Bản sao đầy đủ của một dòng cấu hình.</summary>
        private static KpiConfig Copy(KpiConfig c)
        {
            return new KpiConfig
            {
                Id = c.Id,
                SupportMaxPoint = c.SupportMaxPoint,
                SupportHoursShare = c.SupportHoursShare,
                ExecuteMaxPoint = c.ExecuteMaxPoint,
                ExecuteHoursShare = c.ExecuteHoursShare,
                SupportLateFreeCount = c.SupportLateFreeCount,
                SupportLate2Penalty = c.SupportLate2Penalty,
                SupportLateMorePenalty = c.SupportLateMorePenalty,
                ExecuteLatePenalty = c.ExecuteLatePenalty,
                HoursPerDay = c.HoursPerDay,
                CountSaturday = c.CountSaturday,
                CountSunday = c.CountSunday,
                RankExcellent = c.RankExcellent,
                RankGood = c.RankGood,
                RankFair = c.RankFair,
                RankPass = c.RankPass,
                UpdatedAt = c.UpdatedAt,
                UpdatedBy = c.UpdatedBy
            };
        }

        /// <summary>Bản mặc định của hệ thống, lấy từ Web.config (khoá "Kpi:...").</summary>
        public static KpiConfig Defaults()
        {
            return new KpiConfig
            {
                SupportMaxPoint = AppSettings.Kpi.SupportMaxPoint,

                // Hỗ trợ được tính tối đa 30% quỹ giờ tháng — nó là việc phụ, quá mức này thì
                // phần vượt không còn phản ánh công triển khai nữa.
                SupportHoursShare = 0.3m,
                ExecuteMaxPoint = AppSettings.Kpi.ExecuteMaxPoint,

                // 70% giờ tháng dành cho triển khai là mức được trọn điểm; phần còn lại là hỗ trợ,
                // họp hành, việc lặt vặt. Làm nhiều hơn thì điểm vượt lên, không bị chặn.
                ExecuteHoursShare = 0.7m,

                SupportLateFreeCount = 1,
                SupportLate2Penalty = AppSettings.Kpi.SupportLate2Penalty,
                SupportLateMorePenalty = AppSettings.Kpi.SupportLateMorePenalty,
                ExecuteLatePenalty = AppSettings.Kpi.ExecuteLatePenalty,

                HoursPerDay = 8,
                CountSaturday = false,
                CountSunday = false,

                RankExcellent = 100,
                RankGood = 95,
                RankFair = 90,
                RankPass = 80
            };
        }

        /// <summary>
        /// Chặn các giá trị vô nghĩa đọc lên từ CSDL (dòng cũ thiếu cột thì thuộc tính bằng 0).
        ///
        /// Đây là lưới an toàn cuối cùng, KHÔNG thay cho phần kiểm khi lưu: giờ công bằng 0 sẽ làm
        /// giờ yêu cầu bằng 0 và mọi người đều 100% ngày công; các ngưỡng xếp loại lộn xộn thì mọi
        /// người đều "Xuất sắc".
        /// </summary>
        private static KpiConfig Sanitize(KpiConfig c)
        {
            var d = Defaults();

            if (c.SupportMaxPoint < 0) c.SupportMaxPoint = d.SupportMaxPoint;
            if (c.ExecuteMaxPoint < 0) c.ExecuteMaxPoint = d.ExecuteMaxPoint;
            if (c.SupportMaxPoint + c.ExecuteMaxPoint <= 0)
            {
                c.SupportMaxPoint = d.SupportMaxPoint;
                c.ExecuteMaxPoint = d.ExecuteMaxPoint;
            }

            // Định mức giờ nhóm thực hiện phải là một phần dương của tháng. Bằng 0 thì mẫu số
            // bằng 0 và không ai có điểm; lớn hơn 1 nghĩa là đòi làm nhiều giờ hơn cả tháng.
            if (c.ExecuteHoursShare <= 0 || c.ExecuteHoursShare > 1) c.ExecuteHoursShare = d.ExecuteHoursShare;
            if (c.SupportHoursShare <= 0 || c.SupportHoursShare > 1) c.SupportHoursShare = d.SupportHoursShare;

            if (c.SupportLateFreeCount < 0) c.SupportLateFreeCount = 0;
            if (c.SupportLate2Penalty < 0) c.SupportLate2Penalty = 0;
            if (c.SupportLateMorePenalty < 0) c.SupportLateMorePenalty = 0;
            if (c.ExecuteLatePenalty < 0) c.ExecuteLatePenalty = 0;

            if (c.HoursPerDay <= 0) c.HoursPerDay = d.HoursPerDay;

            // Ngưỡng phải giảm dần; sai thứ tự thì xếp loại mất nghĩa nên trả cả bộ về mặc định.
            if (c.RankExcellent <= 0 ||
                !(c.RankExcellent > c.RankGood && c.RankGood > c.RankFair && c.RankFair > c.RankPass))
            {
                c.RankExcellent = d.RankExcellent;
                c.RankGood = d.RankGood;
                c.RankFair = d.RankFair;
                c.RankPass = d.RankPass;
            }

            return c;
        }

        /// <summary>
        /// Ghi đè bản đang áp dụng. Trả về dòng đã lưu.
        ///
        /// Luôn ghi vào dòng đầu tiên nếu đã có — bảng này chỉ được phép có một dòng, thêm dòng
        /// mới thì lần đọc sau bốc phải bản nào là chuyện may rủi.
        /// </summary>
        public static KpiConfig Save(KpiConfig input, string userFullName)
        {
            var config = Sanitize(input);
            config.UpdatedAt = DateTime.Now;
            config.UpdatedBy = userFullName;

            var all = Repository.KpiConfigs.All();
            if (all.Count == 0)
            {
                config.Id = 0;
                Repository.KpiConfigs.Insert(config);
            }
            else
            {
                config.Id = all[0].Id;
                Repository.KpiConfigs.Update(config);

                // Dọn dòng thừa nếu lỡ có (nhập tay dưới CSDL chẳng hạn) — giữ đúng một bản.
                if (all.Count > 1) Repository.KpiConfigs.DeleteWhere(x => x.Id != config.Id);
            }

            ClearRequestCache();
            return config;
        }

        /// <summary>Đưa cấu hình về mặc định của hệ thống (giá trị trong Web.config).</summary>
        public static KpiConfig ResetToDefaults(string userFullName)
        {
            return Save(Defaults(), userFullName);
        }

        /// <summary>Bỏ bản nhớ trong request để phần còn lại của lượt xử lý này thấy giá trị mới.</summary>
        private static void ClearRequestCache()
        {
            var context = HttpContext.Current;
            if (context != null) context.Items.Remove("TTKDGP:KpiConfig");
        }

        /// <summary>Cấu hình hiện tại có khác bản mặc định của hệ thống không.</summary>
        public static bool IsCustomized
        {
            get
            {
                var c = Current;
                var d = Defaults();

                return c.SupportMaxPoint != d.SupportMaxPoint
                    || c.ExecuteMaxPoint != d.ExecuteMaxPoint
                    || c.ExecuteHoursShare != d.ExecuteHoursShare
                    || c.SupportHoursShare != d.SupportHoursShare
                    || c.SupportLateFreeCount != d.SupportLateFreeCount
                    || c.SupportLate2Penalty != d.SupportLate2Penalty
                    || c.SupportLateMorePenalty != d.SupportLateMorePenalty
                    || c.ExecuteLatePenalty != d.ExecuteLatePenalty
                    || c.HoursPerDay != d.HoursPerDay
                    || c.CountSaturday != d.CountSaturday
                    || c.CountSunday != d.CountSunday
                    || c.RankExcellent != d.RankExcellent
                    || c.RankGood != d.RankGood
                    || c.RankFair != d.RankFair
                    || c.RankPass != d.RankPass;
            }
        }
    }
}
