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
    /// Trọng số và mức trừ đọc từ Web.config (khoá "Kpi:...") để đổi được mà không phải build lại.
    /// </summary>
    public static class KpiService
    {
        /// <summary>Một ngày công bằng 8 giờ.</summary>
        public const int HoursPerDay = 8;

        // ---------- Cấu hình (KPI_Config) ----------

        public static decimal SupportMaxPoint { get { return AppSettings.Kpi.SupportMaxPoint; } }
        public static decimal ExecuteMaxPoint { get { return AppSettings.Kpi.ExecuteMaxPoint; } }

        // ---------- Ngày công ----------

        /// <summary>Số ngày làm việc chuẩn của tháng: tổng số ngày trừ Thứ 7 và Chủ nhật.</summary>
        public static int StandardWorkingDays(int year, int month)
        {
            var days = DateTime.DaysInMonth(year, month);
            var count = 0;

            for (var d = 1; d <= days; d++)
            {
                var dow = new DateTime(year, month, d).DayOfWeek;
                if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday) count++;
            }

            return count;
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
            return Repository.WorkTasks.All()
                .Where(t => t.AssigneeUserId == userId
                            && t.State != TaskStates.Cancelled
                            && TaskInMonth(t, year, month))
                .ToList();
        }

        // ---------- Giờ thực hiện ----------

        /// <summary>
        /// Các ngày làm việc (bỏ Thứ 7, Chủ nhật) mà một đầu việc chiếm chỗ. Khoảng lấy từ ngày bắt
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
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) days.Add(d);
            }

            return days;
        }

        /// <summary>
        /// Giờ của một NHÓM đầu việc, đếm theo NGÀY LÀM VIỆC RIÊNG BIỆT.
        ///
        /// Không cộng giờ của từng việc: một người làm nhiều việc song song trong cùng khoảng thì
        /// vẫn chỉ là bấy nhiêu ngày công. Cộng dồn từng việc sẽ thổi phồng số giờ theo số đầu việc
        /// — 5 việc cùng khoảng 1/8–5/8 thành 25 ngày thay vì 5.
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
        /// Tổng giờ thực hiện trong tháng, tính trên những việc ĐÃ HOÀN THÀNH hoặc ĐANG LÀM.
        /// Việc chưa bắt đầu hay tạm dừng chưa phải là giờ đã bỏ ra nên không đếm.
        /// </summary>
        public static decimal WorkedHours(IEnumerable<WorkTask> tasks)
        {
            return HoursOf(tasks.Where(t => t.State == TaskStates.Done || t.State == TaskStates.InProgress));
        }

        // ---------- Tính điểm ----------

        /// <summary>
        /// Điểm một nhóm việc theo kiểu trừ dần: đủ việc hoàn thành thì được điểm tối đa theo
        /// tỷ lệ hoàn thành, rồi trừ tiếp phần phạt báo cáo trễ. Không có việc nào thì hưởng
        /// nguyên điểm tối đa — không có gì để trừ.
        /// </summary>
        private static decimal GroupPoint(decimal maxPoint, int total, int done, decimal latePenalty)
        {
            var basePoint = total == 0 ? maxPoint : maxPoint * done / total;
            var point = basePoint - latePenalty;
            return point < 0 ? 0 : Math.Round(point, 2);
        }

        /// <summary>Mức trừ nhóm hỗ trợ theo số lần trễ: 0–1 lần không trừ, 2 lần trừ 1%, hơn nữa trừ 2%.</summary>
        public static decimal SupportLatePenalty(int lateCount)
        {
            if (lateCount <= 1) return 0;
            if (lateCount == 2) return AppSettings.Kpi.SupportLate2Penalty;
            return AppSettings.Kpi.SupportLateMorePenalty;
        }

        /// <summary>Mức trừ nhóm thực hiện: mỗi lần trễ trừ 0,25%.</summary>
        public static decimal ExecuteLatePenalty(int lateCount)
        {
            return lateCount * AppSettings.Kpi.ExecuteLatePenalty;
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
            var execute = tasks.Where(t => t.Kind == TaskKinds.Checklist).ToList();
            var assigned = tasks.Where(t => t.Kind == TaskKinds.Standalone).ToList();

            // "Báo cáo trễ" = việc hoàn thành sau hạn. Việc quá hạn còn treo không đếm vào đây —
            // nó đã bị phạt qua tỷ lệ hoàn thành (done/total) rồi.
            row.SupportTotal = support.Count;
            row.SupportDone = support.Count(t => t.State == TaskStates.Done);
            row.SupportLateCount = support.Count(t => t.State == TaskStates.Done && !t.IsOnTime);
            row.SupportPoint = GroupPoint(SupportMaxPoint, row.SupportTotal, row.SupportDone,
                SupportLatePenalty(row.SupportLateCount));

            row.ExecuteTotal = execute.Count;
            row.ExecuteDone = execute.Count(t => t.State == TaskStates.Done);
            row.ExecuteLateCount = execute.Count(t => t.State == TaskStates.Done && !t.IsOnTime);
            row.ExecutePoint = GroupPoint(ExecuteMaxPoint, row.ExecuteTotal, row.ExecuteDone,
                ExecuteLatePenalty(row.ExecuteLateCount));

            // Việc riêng: điểm cộng đã ấn định lúc giao (BonusPercent), chỉ tính khi hoàn thành
            // ĐÚNG HẠN — đúng định nghĩa "điểm cộng KPI khi hoàn thành đúng hạn" lúc giao việc.
            row.AssignedTotal = assigned.Count;
            row.AssignedDone = assigned.Count(t => t.State == TaskStates.Done);
            row.AssignedPoint = Math.Round(
                assigned.Where(t => t.State == TaskStates.Done && t.IsOnTime).Sum(t => t.BonusPercent), 2);

            row.QualityPoint = Math.Round(row.SupportPoint + row.ExecutePoint + row.AssignedPoint, 2);

            // Ngày công. LeaveDays nay lấy thẳng từ các ĐƠN NGHỈ PHÉP ĐÃ DUYỆT của tháng, không
            // còn là ô nhập tay: hai nguồn số liệu song song thì sớm muộn cũng lệch nhau, và ô
            // nhập tay sửa được nghĩa là ai có quyền chấm KPI cũng tự nâng điểm được cho mình.
            // Muốn đổi số ngày nghỉ thì sửa ở màn Duyệt nghỉ phép.
            row.StandardDays = StandardWorkingDays(row.Year, row.Month);
            row.LeaveDays = LeaveService.ApprovedDays(row.UserId, row.Year, row.Month);
            var requiredDays = row.StandardDays - row.LeaveDays;
            row.RequiredHours = requiredDays > 0 ? requiredDays * HoursPerDay : 0;
            row.WorkingHours = Math.Round(WorkedHours(tasks), 2);

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
