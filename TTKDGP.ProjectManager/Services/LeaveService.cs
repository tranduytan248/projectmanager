using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Nghỉ phép: nơi DUY NHẤT trả lời "tháng này người đó nghỉ bao nhiêu ngày".
    ///
    /// Cả bộ chấm KPI (giảm giờ yêu cầu) lẫn bảng thống kê khối lượng (giảm quỹ giờ để tính trần
    /// 30% hỗ trợ) đều gọi vào đây. Tách riêng để hai nơi không bao giờ ra hai con số khác nhau.
    /// </summary>
    public static class LeaveService
    {
        /// <summary>
        /// Số ngày nghỉ ĐÃ DUYỆT của một người trong một tháng.
        ///
        /// Đơn chờ duyệt/từ chối/đã huỷ đều không tính: chưa duyệt mà đã trừ giờ yêu cầu thì chỉ
        /// cần gửi đơn khống là KPI tự đẹp lên.
        /// </summary>
        public static decimal ApprovedDays(int userId, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            return Repository.LeaveRequests.All()
                .Where(l => l.UserId == userId && l.IsApproved && l.OverlapsRange(from, to))
                .Sum(l => l.DaysInRange(from, to));
        }

        /// <summary>
        /// Số ngày nghỉ đã duyệt trong tháng của NHIỀU người cùng lúc, khoá theo Id người dùng.
        /// Bảng thống kê khối lượng cần con số này cho cả chục người — gọi
        /// <see cref="ApprovedDays"/> trong vòng lặp là mỗi dòng một lượt nạp toàn bảng.
        /// </summary>
        public static Dictionary<int, decimal> ApprovedDaysByUser(int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            return Repository.LeaveRequests.All()
                .Where(l => l.IsApproved && l.OverlapsRange(from, to))
                .GroupBy(l => l.UserId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.DaysInRange(from, to)));
        }

        /// <summary>Đơn của một người, mới nhất trước.</summary>
        public static List<LeaveRequest> OfUser(int userId)
        {
            return Repository.LeaveRequests.All()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.FromDate)
                .ThenByDescending(l => l.Id)
                .ToList();
        }

        /// <summary>Số đơn đang chờ duyệt — hiện lên nhãn trên menu của Quản lý Tổ.</summary>
        public static int PendingCount()
        {
            try
            {
                return Repository.LeaveRequests.All().Count(l => l.State == LeaveStates.Pending);
            }
            catch (Exception)
            {
                // Bảng chưa kịp tạo hoặc SQL chập chờn: không để menu làm sập cả trang.
                return 0;
            }
        }

        /// <summary>
        /// Các đợt nghỉ ĐÃ DUYỆT chồng lấn với đơn đang định gửi — dùng để chặn đăng ký trùng ngày.
        /// Bỏ qua chính đơn đang sửa (excludeId).
        /// </summary>
        public static LeaveRequest FindOverlap(int userId, DateTime from, DateTime to, int excludeId = 0)
        {
            return Repository.LeaveRequests.All()
                .FirstOrDefault(l => l.UserId == userId
                                     && l.Id != excludeId
                                     && (l.State == LeaveStates.Pending || l.State == LeaveStates.Approved)
                                     && l.OverlapsRange(from, to));
        }
    }
}
