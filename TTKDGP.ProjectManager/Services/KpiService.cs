using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Bộ tính KPI tháng theo đặc tả mới.
    ///
    /// KPI chất lượng = điểm hỗ trợ (tối đa 30) + điểm thực hiện (tối đa 70)
    ///                + điểm việc riêng (cộng % từng việc hoàn thành đúng hạn).
    /// KPI cuối cùng = KPI chất lượng, nhân tỷ lệ ngày công nếu thiếu giờ, làm tròn về số nguyên.
    ///
    /// Mọi trọng số, mức trừ, số giờ một ngày công và ngưỡng xếp loại đều lấy từ
    /// <see cref="KpiConfigService"/> — sửa trên màn "Cấu hình KPI", không phải sửa mã nguồn.
    /// </summary>
    public static class KpiService
    {
        /// <summary>Bộ tham số đang áp dụng. Gọi tắt cho gọn, KpiConfigService đã nhớ theo request.</summary>
        private static KpiConfig Config { get { return KpiConfigService.Current; } }

        /// <summary>Số giờ của một ngày công.</summary>
        public static decimal HoursPerDay { get { return Config.HoursPerDay; } }

        // ---------- Cấu hình (KPI_Config) ----------

        public static decimal SupportMaxPoint { get { return Config.SupportMaxPoint; } }
        public static decimal ExecuteMaxPoint { get { return Config.ExecuteMaxPoint; } }

        /// <summary>Điểm chất lượng tối đa khi chưa cộng việc riêng — thang của thanh tiến độ.</summary>
        public static decimal MaxQualityPoint { get { return SupportMaxPoint + ExecuteMaxPoint; } }

        // ---------- Ngày công ----------

        /// <summary>
        /// Số ngày làm việc chuẩn của tháng: tổng số ngày trừ những ngày cuối tuần KHÔNG được
        /// cấu hình tính vào ngày công (mặc định bỏ cả Thứ 7 lẫn Chủ nhật).
        /// </summary>
        public static int StandardWorkingDays(int year, int month)
        {
            var days = DateTime.DaysInMonth(year, month);
            var count = 0;

            for (var d = 1; d <= days; d++)
            {
                if (IsWorkingDay(new DateTime(year, month, d))) count++;
            }

            return count;
        }

        /// <summary>Ngày này có được tính là ngày làm việc không, theo cấu hình cuối tuần.</summary>
        public static bool IsWorkingDay(DateTime day)
        {
            var config = Config;

            if (day.DayOfWeek == DayOfWeek.Saturday) return config.CountSaturday;
            if (day.DayOfWeek == DayOfWeek.Sunday) return config.CountSunday;

            return true;
        }

        // ---------- Chọn việc thuộc tháng ----------

        /// <summary>
        /// Đầu việc có thuộc tháng này không — cùng quy tắc với bảng tổng hợp tháng ở trang chủ:
        /// ưu tiên hạn hoàn thành (việc đến hạn trong kỳ thì chấm trong kỳ); việc không có hạn thì
        /// theo tuần được phân; còn lại dựa vào mốc hoàn thành.
        /// </summary>
        public static bool TaskInMonth(WorkTask task, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            if (task.DueDate.HasValue)
            {
                var d = task.DueDate.Value.Date;
                return d >= from && d <= to;
            }

            if (task.Year > 0 && task.Week > 0)
            {
                return WeekHelper.FirstDayOfWeek(task.Year, task.Week) <= to
                    && WeekHelper.LastDayOfWeek(task.Year, task.Week) >= from;
            }

            return task.CompletedAt.HasValue
                && task.CompletedAt.Value.Date >= from
                && task.CompletedAt.Value.Date <= to;
        }

        /// <summary>
        /// Việc được chấm của một người trong tháng: việc đã giao cho người đó, thuộc tháng,
        /// bỏ việc đã huỷ (huỷ là rút khỏi kế hoạch, chấm nữa thì thành phạt oan).
        /// </summary>
        public static List<WorkTask> TasksOfUserInMonth(int userId, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var loggedTaskIds = new HashSet<int>(
                Repository.WorkTimeLogs.All()
                    .Where(l => l.UserId == userId && l.WorkDate.Date >= from && l.WorkDate.Date <= to)
                    .Select(l => l.TaskId));

            return Repository.WorkTasks.All()
                .Where(t => t.State != TaskStates.Cancelled
                            && (t.AssigneeUserId == userId || loggedTaskIds.Contains(t.Id))
                            && (TaskInMonth(t, year, month) || loggedTaskIds.Contains(t.Id)))
                .ToList();
        }

        // ---------- Giờ thực hiện ----------

        /// <summary>
        /// Các ngày làm việc (bỏ cuối tuần theo cấu hình) mà một đầu việc chiếm chỗ. Khoảng lấy từ ngày bắt
        /// đầu đến hạn hoàn thành; thiếu ngày bắt đầu thì lấy ngày tạo; việc không có hạn nhưng gắn
        /// tuần thì lấy trọn tuần đó; không có cả hai thì không tính được — rỗng.
        /// </summary>
        private static List<DateTime> WorkingDaysOf(WorkTask task)
        {
            var days = new List<DateTime>();
            DateTime from, to;

            if (task.DueDate.HasValue)
            {
                to = task.DueDate.Value.Date;
                from = task.StartDate.HasValue ? task.StartDate.Value.Date : task.CreatedAt.Date;
                if (from > to) from = to;
            }
            else if (task.Year > 0 && task.Week > 0)
            {
                from = WeekHelper.FirstDayOfWeek(task.Year, task.Week);
                to = WeekHelper.LastDayOfWeek(task.Year, task.Week);
            }
            else
            {
                return days;
            }

            for (var d = from; d <= to; d = d.AddDays(1))
            {
                if (IsWorkingDay(d)) days.Add(d);
            }

            return days;
        }

        /// <summary>
        /// Giờ của một NHÓM đầu việc, đếm theo NGÀY LÀM VIỆC RIÊNG BIỆT.
        ///
        /// Không cộng giờ của từng việc: một người làm nhiều việc song song trong cùng khoảng thì
        /// vẫn chỉ là bấy nhiêu ngày công. Cộng dồn từng việc sẽ thổi phồng số giờ theo số đầu việc
        /// — 5 việc cùng khoảng 1/8–5/8 thành 25 ngày thay vì 5.
        ///
        /// <paramref name="year"/>/<paramref name="month"/> giới hạn phạm vi đếm về đúng tháng
        /// đang chấm. Việc trải qua nhiều tháng (bắt đầu 20/7, hạn 05/8) chỉ được tính phần ngày
        /// NẰM TRONG tháng — thiếu chặn này thì giờ của tháng 8 mang theo cả tháng 7 và vượt quá
        /// số ngày công của chính tháng đó.
        /// </summary>
        public static decimal HoursOf(IEnumerable<WorkTask> tasks, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var days = new HashSet<DateTime>();

            foreach (var task in tasks)
            {
                foreach (var day in WorkingDaysOf(task))
                {
                    if (day >= from && day <= to) days.Add(day);
                }
            }

            return days.Count * HoursPerDay;
        }

        /// <summary>
        /// Giờ của một nhóm việc, KHÔNG giới hạn tháng. Giữ cho những nơi tự cắt phạm vi từ trước
        /// (bảng khối lượng theo dự án). Bộ chấm KPI luôn dùng bản có tháng ở trên.
        /// </summary>
        public static decimal HoursOf(IEnumerable<WorkTask> tasks)
        {
            var days = new HashSet<DateTime>();

            foreach (var task in tasks)
            {
                foreach (var day in WorkingDaysOf(task)) days.Add(day);
            }

            return days.Count * HoursPerDay;
        }

        /// <summary>
        /// Tổng giờ được công nhận trong tháng của một người — tính từ TỔNG GIỜ LOGTIME THỰC TẾ (WorkTimeLogs)
        /// mà người đó đã ghi trong tháng (ngoại trừ công việc Chưa bắt đầu hoặc Huỷ).
        /// </summary>
        public static decimal WorkedHours(int userId, IEnumerable<WorkTask> tasks, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var validTaskIds = new HashSet<int>(
                tasks.Where(t => t.State != TaskStates.NotStarted && t.State != TaskStates.Cancelled)
                     .Select(t => t.Id));

            if (validTaskIds.Count == 0) return 0m;

            return Repository.WorkTimeLogs.All()
                .Where(l => l.UserId == userId && validTaskIds.Contains(l.TaskId) && l.WorkDate.Date >= from && l.WorkDate.Date <= to)
                .Sum(l => (decimal?)l.Hours) ?? 0m;
        }

        /// <summary>
        /// Giờ hỗ trợ được TÍNH của một người: giờ thực tế, nhưng chặn trên ở
        /// <see cref="KpiConfig.SupportHoursShare"/> phần quỹ giờ tháng (mặc định 30%).
        ///
        /// Hỗ trợ là việc phát sinh theo tuần, một người có thể bị kéo vào rất nhiều đầu việc hỗ
        /// trợ rải khắp tháng — để nguyên thì giờ hỗ trợ nuốt trọn quỹ giờ và người đó "đủ giờ
        /// công" mà chẳng triển khai được gì. Trần này giữ hỗ trợ đúng vai phụ của nó.
        /// </summary>
        public static decimal CappedSupportHours(decimal rawSupportHours, decimal requiredHours)
        {
            if (requiredHours <= 0) return rawSupportHours;

            var cap = requiredHours * Config.SupportHoursShare;
            return rawSupportHours > cap ? Math.Round(cap, 2) : rawSupportHours;
        }

        // ---------- Tính điểm ----------

        /// <summary>
        /// Điểm nhóm HỖ TRỢ, tính theo GIỜ đã bỏ ra — CÓ chặn trần.
        ///
        /// Mốc: dành đúng <see cref="KpiConfig.SupportHoursShare"/> phần giờ yêu cầu của tháng cho
        /// việc hỗ trợ thì được trọn điểm nhóm (mặc định 30% giờ → 30 điểm). Khác nhóm thực hiện ở
        /// chỗ CÓ TRẦN: hỗ trợ là việc phụ, làm quá mức không nên được thưởng thêm điểm — phần giờ
        /// vượt cũng đã không được tính vào giờ công (xem <see cref="CappedSupportHours"/>).
        ///
        /// Không đếm theo số việc xong/tổng: một việc hỗ trợ có thể là nửa buổi hoặc cả tuần, đếm
        /// đầu việc thì ai bị giao nhiều việc vụn lại thiệt.
        ///
        /// KHÔNG có việc hỗ trợ nào thì được 0 — điểm phản ánh việc đã làm. Trước đây nhóm rỗng
        /// hưởng trọn điểm tối đa, cho ra kết quả ngược đời: người duy nhất có việc hỗ trợ nhưng
        /// chưa xong bị 0 điểm, trong khi cả tổ không ai được giao việc lại đều đủ 30.
        /// </summary>
        private static decimal SupportPoint(decimal maxPoint, decimal supportHours,
            decimal requiredHours, int total, decimal latePenalty)
        {
            if (total == 0) return 0;

            // Chưa xác định được giờ yêu cầu (nghỉ trọn tháng, hoặc dữ liệu thiếu) thì không có
            // mẫu số để so — lùi về 0 thay vì chia cho 0.
            var target = requiredHours * Config.SupportHoursShare;
            if (target <= 0) return 0;

            var earned = maxPoint * supportHours / target;
            if (earned > maxPoint) earned = maxPoint;

            var point = earned - latePenalty;
            return point < 0 ? 0 : Math.Round(point, 2);
        }

        /// <summary>
        /// Điểm nhóm THỰC HIỆN, tính theo GIỜ đã bỏ ra chứ không theo số đầu việc — và KHÔNG chặn
        /// trần.
        ///
        /// Mốc: dành đúng <see cref="KpiConfig.ExecuteHoursShare"/> phần giờ yêu cầu của tháng cho
        /// việc triển khai thì được trọn điểm tối đa (mặc định 70% giờ → 70 điểm). Dành nhiều hơn
        /// thì điểm cao hơn 70 — làm hơn 70% thời gian trong tháng là chuyện bình thường, chặn ở
        /// đó thì người gánh nhiều dự án và người vừa đủ định mức nhận cùng một điểm.
        ///
        /// Không đếm theo số việc xong/tổng như nhóm hỗ trợ: một đầu việc có thể là nửa ngày hoặc
        /// cả tháng, đếm đầu việc thì ai chẻ nhỏ công việc ra sẽ có lợi.
        /// </summary>
        private static decimal ExecutePoint(decimal maxPoint, decimal executeHours,
            decimal requiredHours, int total, decimal latePenalty)
        {
            if (total == 0) return 0;

            // Chưa xác định được giờ yêu cầu (tháng nghỉ trọn, hoặc dữ liệu thiếu) thì không có
            // mẫu số để so — lùi về 0 thay vì chia cho 0.
            var target = requiredHours * Config.ExecuteHoursShare;
            if (target <= 0) return 0;

            var point = maxPoint * executeHours / target - latePenalty;
            return point < 0 ? 0 : Math.Round(point, 2);
        }

        /// <summary>
        /// Mức trừ nhóm hỗ trợ theo số lần trễ, ba bậc theo cấu hình: trong mức miễn thì không
        /// trừ, vượt đúng một lần trừ mức thứ nhất, vượt nhiều hơn trừ mức thứ hai.
        /// Mặc định: 0–1 lần không trừ, 2 lần trừ 1, hơn nữa trừ 2.
        /// </summary>
        public static decimal SupportLatePenalty(int lateCount)
        {
            var config = Config;

            if (lateCount <= config.SupportLateFreeCount) return 0;
            if (lateCount == config.SupportLateFreeCount + 1) return config.SupportLate2Penalty;
            return config.SupportLateMorePenalty;
        }

        /// <summary>Mức trừ nhóm thực hiện: mỗi lần trễ trừ một mức cố định (mặc định 0,25).</summary>
        public static decimal ExecuteLatePenalty(int lateCount)
        {
            return lateCount * Config.ExecuteLatePenalty;
        }

        /// <summary>Làm tròn về số nguyên, nửa điểm trở lên làm tròn lên (99,5 → 100).</summary>
        public static decimal RoundFinal(decimal value)
        {
            return Math.Round(value, 0, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Tính (hoặc tính lại) KPI tháng cho một người và lưu xuống. Toàn bộ số liệu đều được
        /// tính lại từ nguồn: điểm từ các đầu việc trong tháng, số ngày nghỉ từ các đơn nghỉ phép
        /// đã duyệt. Không còn ô nhập tay nào trên dòng KPI.
        /// </summary>
        public static KpiMonth CalculateUser(int year, int month, User user)
        {
            var existing = Repository.KpiMonths.FirstOrDefault(
                k => k.Year == year && k.Month == month && k.UserId == user.Id);

            var row = existing ?? new KpiMonth
            {
                Year = year,
                Month = month,
                UserId = user.Id,
                CreatedAt = DateTime.Now
            };

            row.UserFullName = user.FullName;
            Fill(row, TasksOfUserInMonth(user.Id, year, month));
            row.UpdatedAt = DateTime.Now;

            if (existing == null) Repository.KpiMonths.Insert(row);
            else Repository.KpiMonths.Update(row);

            return row;
        }

        /// <summary>
        /// Tính KPI cho toàn bộ nhân sự được theo dõi trong một tháng.
        ///
        /// Bỏ qua lãnh đạo Tổ: hàm này GHI xuống bảng KpiMonths, nên nếu tính cả họ thì mỗi lần
        /// bấm "Tính KPI" lại sinh ra một phiếu 0 điểm mang tên người quản lý — vừa sai nghiệp vụ
        /// vừa kéo tụt điểm trung bình của cả Tổ.
        /// </summary>
        public static List<KpiMonth> CalculateAll(int year, int month)
        {
            return WorkService.TrackedUsers()
                .Select(u => CalculateUser(year, month, u))
                .ToList();
        }

        /// <summary>
        /// Đổ điểm vào một dòng KPI từ danh sách việc trong tháng. Tách riêng để màn chi tiết
        /// xem trước được kết quả của người CHƯA có dòng lưu mà không phải ghi gì xuống.
        /// </summary>
        public static void Fill(KpiMonth row, List<WorkTask> tasks)
        {
            var support = tasks.Where(t => t.Kind == TaskKinds.Support).ToList();
            var execute = tasks.Where(t => t.Kind == TaskKinds.Checklist || t.Kind == TaskKinds.Standalone).ToList();
            var assigned = tasks.Where(t => t.Kind == TaskKinds.Standalone).ToList();

            // Ngày công tính TRƯỚC phần điểm: nhóm thực hiện nay chấm theo giờ, mà giờ yêu cầu
            // của tháng chính là mẫu số của nó. Đảo thứ tự là chia cho 0.
            //
            // LeaveDays lấy thẳng từ các ĐƠN NGHỈ PHÉP ĐÃ DUYỆT của tháng, không còn là ô nhập
            // tay: hai nguồn số liệu song song thì sớm muộn cũng lệch nhau, và ô nhập tay sửa
            // được nghĩa là ai có quyền chấm KPI cũng tự nâng điểm được cho mình. Muốn đổi số
            // ngày nghỉ thì sửa ở màn Duyệt nghỉ phép.
            row.StandardDays = StandardWorkingDays(row.Year, row.Month);
            row.LeaveDays = LeaveService.ApprovedDays(row.UserId, row.Year, row.Month);
            var requiredDays = row.StandardDays - row.LeaveDays;
            row.RequiredHours = requiredDays > 0 ? requiredDays * HoursPerDay : 0;

            // Số lần trễ hạn = việc đã hoàn thành sau hạn HOẶC việc đang bị quá hạn chưa xong.
            row.SupportTotal = support.Count;
            row.SupportDone = support.Count(t => t.State == TaskStates.Done);
            row.SupportLateCount = support.Count(t => (t.State == TaskStates.Done && !t.IsOnTime) || t.IsOverdue);

            // Giờ hỗ trợ bị CHẶN TRẦN ở phần cấu hình (mặc định 30% quỹ giờ tháng).
            row.SupportHoursRaw = Math.Round(WorkedHours(row.UserId, support, row.Year, row.Month), 2);
            row.SupportHours = CappedSupportHours(row.SupportHoursRaw, row.RequiredHours);

            row.SupportPoint = SupportPoint(SupportMaxPoint, row.SupportHours, row.RequiredHours,
                row.SupportTotal, SupportLatePenalty(row.SupportLateCount));

            // Nhóm thực hiện chấm theo GIỜ đã bỏ ra, không theo số đầu việc — và không chặn trần.
            row.ExecuteTotal = execute.Count;
            row.ExecuteDone = execute.Count(t => t.State == TaskStates.Done);
            row.ExecuteLateCount = execute.Count(t => (t.State == TaskStates.Done && !t.IsOnTime) || t.IsOverdue);
            row.ExecuteHours = Math.Round(WorkedHours(row.UserId, execute, row.Year, row.Month), 2);
            row.ExecutePoint = ExecutePoint(ExecuteMaxPoint, row.ExecuteHours, row.RequiredHours,
                row.ExecuteTotal, ExecuteLatePenalty(row.ExecuteLateCount));

            // Tổng giờ công = Tổng giờ logtime thực tế trừ phần hỗ trợ vượt trần
            var totalHours = WorkedHours(row.UserId, tasks, row.Year, row.Month);
            var supportOver = row.SupportHoursRaw - row.SupportHours;

            var working = totalHours - supportOver;
            row.WorkingHours = Math.Round(working > 0 ? working : 0, 2);

            // Việc riêng: điểm cộng đã ấn định lúc giao (BonusPercent), chỉ tính khi hoàn thành
            // ĐÚNG HẠN — đúng định nghĩa "điểm cộng KPI khi hoàn thành đúng hạn" lúc giao việc.
            row.AssignedTotal = assigned.Count;
            row.AssignedDone = assigned.Count(t => t.State == TaskStates.Done);
            row.AssignedPoint = Math.Round(
                assigned.Where(t => t.State == TaskStates.Done && t.IsOnTime).Sum(t => t.BonusPercent), 2);

            row.QualityPoint = Math.Round(row.SupportPoint + row.ExecutePoint + row.AssignedPoint, 2);

            row.AttendanceRate = AttendanceRate(row);
            row.FinalPoint = RoundFinal(FinalBeforeRounding(row));
        }

        /// <summary>Tỷ lệ ngày công (%), chặn trên 100.</summary>
        private static decimal AttendanceRate(KpiMonth row)
        {
            if (row.RequiredHours <= 0) return 100;

            var rate = (row.WorkingHours ?? 0) / row.RequiredHours * 100;
            return rate >= 100 ? 100 : Math.Round(rate, 2);
        }

        /// <summary>Đủ giờ thì giữ nguyên KPI chất lượng; thiếu giờ thì nhân tỷ lệ ngày công.</summary>
        private static decimal FinalBeforeRounding(KpiMonth row)
        {
            if ((row.WorkingHours ?? 0) >= row.RequiredHours)
            {
                return row.QualityPoint;
            }

            return row.QualityPoint * row.AttendanceRate / 100;
        }
    }
}
