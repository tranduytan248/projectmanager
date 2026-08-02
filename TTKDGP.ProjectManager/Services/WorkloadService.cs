using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Một dòng trong bảng thống kê khối lượng công việc theo tháng của một dự án.
    /// </summary>
    public class WorkloadRow
    {
        public int UserId { get; set; }
        public string FullName { get; set; }

        // ---------- Hỗ trợ ----------

        /// <summary>Giờ hỗ trợ tính được từ các đầu việc, TRƯỚC khi áp trần 30%.</summary>
        public decimal SupportHoursRaw { get; set; }

        /// <summary>Giờ hỗ trợ sau khi áp trần — đây là số được tính vào khối lượng.</summary>
        public decimal SupportHours { get; set; }

        public int SupportTaskCount { get; set; }

        /// <summary>Giờ hỗ trợ có vượt trần không — để bảng đánh dấu cho người đọc biết.</summary>
        public bool SupportCapped { get { return SupportHoursRaw > SupportHours; } }

        // ---------- Triển khai ----------

        public decimal ImplementHours { get; set; }
        public int ImplementTaskCount { get; set; }

        // ---------- Quỹ giờ của tháng ----------

        /// <summary>Số ngày công chuẩn của tháng (đã bỏ Thứ 7, Chủ nhật).</summary>
        public int StandardDays { get; set; }

        /// <summary>Số ngày nghỉ phép ĐÃ DUYỆT trong tháng.</summary>
        public decimal LeaveDays { get; set; }

        /// <summary>Quỹ giờ của tháng = (ngày công chuẩn − ngày nghỉ đã duyệt) × 8.</summary>
        public decimal MonthHours { get; set; }

        /// <summary>Trần giờ hỗ trợ = 30% quỹ giờ tháng.</summary>
        public decimal SupportCap { get; set; }

        /// <summary>Tổng khối lượng được ghi nhận = giờ hỗ trợ (đã áp trần) + giờ triển khai.</summary>
        public decimal TotalHours { get { return SupportHours + ImplementHours; } }
    }

    /// <summary>
    /// Thống kê khối lượng công việc của từng cá nhân TRONG MỘT DỰ ÁN theo tháng.
    ///
    /// Quy tắc tính (theo yêu cầu nghiệp vụ):
    ///   • Quỹ giờ tháng = (số ngày làm việc của tháng − số ngày phép đã duyệt) × 8 giờ.
    ///     Ngày làm việc đã bỏ Thứ 7 và Chủ nhật.
    ///   • Thời gian hỗ trợ: nếu vượt quá 30% quỹ giờ tháng thì CHỈ TÍNH 30% — phần vượt bị cắt.
    ///     Đây là trần cứng: hỗ trợ là việc nền, không được phép chiếm hết quỹ thời gian.
    ///   • Thời gian thực hiện triển khai: tính đủ, không có trần.
    ///
    /// Giờ của một nhóm việc dùng lại <see cref="KpiService.HoursOf"/> để bảng này và bảng KPI
    /// không bao giờ ra hai con số khác nhau. Hàm đó đếm theo NGÀY LÀM VIỆC RIÊNG BIỆT: nhiều việc
    /// chạy song song trong cùng khoảng chỉ tính một lần, không nhân lên theo số đầu việc.
    /// </summary>
    public static class WorkloadService
    {
        /// <summary>Phần quỹ giờ tháng tối đa mà công việc hỗ trợ được tính (30%).</summary>
        public const decimal SupportCapRate = 0.30m;

        /// <summary>
        /// Dựng bảng khối lượng của một dự án trong một tháng.
        ///
        /// Người được liệt kê là những ai CÓ VIỆC trong dự án ở tháng đó, cộng những ai đang trong
        /// giai đoạn tham gia dự án dù chưa có việc nào — thiếu vế sau thì người mới vào dự án
        /// không xuất hiện, nhìn như chưa được phân công.
        /// </summary>
        public static List<WorkloadRow> Build(int projectId, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var tasks = Repository.WorkTasks.All()
                .Where(t => t.ProjectId == projectId
                            && t.AssigneeUserId > 0
                            && t.State != TaskStates.Cancelled
                            && TaskKinds.InProject.Contains(t.Kind)
                            && KpiService.TaskInMonth(t, year, month))
                .ToList();

            // Người tham gia dự án trong tháng — kể cả khi chưa có đầu việc nào.
            var members = WorkService.AssignmentsOfProject(projectId)
                .Where(a => a.OverlapsRange(from, to))
                .ToList();

            var userIds = new HashSet<int>(tasks.Select(t => t.AssigneeUserId));
            foreach (var m in members.Where(m => m.UserId > 0)) userIds.Add(m.UserId);

            if (userIds.Count == 0) return new List<WorkloadRow>();

            var standardDays = KpiService.StandardWorkingDays(year, month);
            var leaveByUser = LeaveService.ApprovedDaysByUser(year, month);

            // Tên lấy từ bảng Users; người đã bị xoá tài khoản thì lùi về tên đã lưu trên đầu việc
            // hoặc trên dòng phân công, để dòng không hiện ra trống trơn.
            var users = Repository.Users.All()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.FullName);

            var rows = new List<WorkloadRow>();

            foreach (var userId in userIds)
            {
                var mine = tasks.Where(t => t.AssigneeUserId == userId).ToList();

                var support = mine.Where(t => t.Kind == TaskKinds.Support).ToList();
                var implement = mine.Where(t => t.Kind == TaskKinds.Checklist).ToList();

                decimal leaveDays;
                leaveByUser.TryGetValue(userId, out leaveDays);

                // Quỹ giờ không bao giờ âm: nghỉ nhiều hơn số ngày công chuẩn thì coi như hết quỹ.
                var workingDays = standardDays - leaveDays;
                if (workingDays < 0) workingDays = 0;

                var monthHours = workingDays * KpiService.HoursPerDay;
                var cap = Math.Round(monthHours * SupportCapRate, 2);

                var supportRaw = Math.Round(KpiService.HoursOf(support), 2);

                string name;
                if (!users.TryGetValue(userId, out name) || string.IsNullOrWhiteSpace(name))
                {
                    name = mine.Select(t => t.AssigneeName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                           ?? members.Where(m => m.UserId == userId)
                                .Select(m => m.UserFullName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                           ?? ("Người dùng #" + userId);
                }

                rows.Add(new WorkloadRow
                {
                    UserId = userId,
                    FullName = name,
                    SupportTaskCount = support.Count,
                    SupportHoursRaw = supportRaw,
                    SupportHours = supportRaw > cap ? cap : supportRaw,
                    ImplementTaskCount = implement.Count,
                    ImplementHours = Math.Round(KpiService.HoursOf(implement), 2),
                    StandardDays = standardDays,
                    LeaveDays = leaveDays,
                    MonthHours = monthHours,
                    SupportCap = cap
                });
            }

            return rows
                .OrderByDescending(r => r.TotalHours)
                .ThenBy(r => r.FullName, StringComparer.CurrentCulture)
                .ToList();
        }
    }
}
