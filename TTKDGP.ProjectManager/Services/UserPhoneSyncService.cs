using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Lấy số điện thoại từ danh bạ nhân sự HRM ĐỔ SANG cột số điện thoại của bảng Người dùng.
    ///
    /// Ghép theo EMAIL — cũng là khoá nghiệp vụ của bảng nhân sự HRM. Không ghép theo đơn vị:
    /// tài khoản trong hệ thống rải ở nhiều tổ khác nhau, lọc theo một đơn vị là bỏ sót người.
    ///
    /// Số được GHI HẲN vào bảng Người dùng chứ không nối bảng lúc cần dùng. Lý do: lúc gửi tin
    /// nhắc hạn (chạy nền, hai lượt mỗi ngày) phải có số ngay trong tài khoản, không phụ thuộc
    /// dữ liệu HRM còn hay mất; và số ghi rồi thì sửa tay được cho những người HRM chưa khai.
    ///
    /// Khác với màn "Mở tài khoản từ HRM" ở chỗ: màn kia MỞ tài khoản mới cho một đơn vị, còn
    /// việc này chỉ ĐIỀN SỐ cho tài khoản đã có, không thêm hay xoá tài khoản nào.
    /// </summary>
    public static class UserPhoneSyncService
    {
        /// <summary>Một dòng đối chiếu giữa tài khoản và số điện thoại tìm được bên HRM.</summary>
        public class Row
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }

            /// <summary>Số đang lưu trên tài khoản. Trống nghĩa là chưa có.</summary>
            public string CurrentPhone { get; set; }

            /// <summary>Số tra được bên HRM theo email. Trống nghĩa là HRM không có.</summary>
            public string HrmPhone { get; set; }

            public bool HasHrmPhone { get { return !string.IsNullOrWhiteSpace(HrmPhone); } }
            public bool HasCurrentPhone { get { return !string.IsNullOrWhiteSpace(CurrentPhone); } }

            /// <summary>Tài khoản chưa điền email nên không có đường nào ghép sang HRM.</summary>
            public bool NoEmail { get { return string.IsNullOrWhiteSpace(Email); } }

            /// <summary>
            /// Lượt đổ này sẽ ghi số mới vào tài khoản: HRM có số, và số đó khác số đang lưu.
            /// HRM bỏ trống thì giữ nguyên số cũ — nhiều người được điền tay vì HRM chưa khai,
            /// lấy khoảng trắng đè lên là mất dữ liệu tốt.
            /// </summary>
            public bool WillUpdate
            {
                get
                {
                    return HasHrmPhone
                        && !string.Equals((HrmPhone ?? string.Empty).Trim(),
                                          (CurrentPhone ?? string.Empty).Trim(),
                                          StringComparison.Ordinal);
                }
            }
        }

        /// <summary>Kết quả rà soát, dùng chung cho màn xem trước và lượt ghi thật.</summary>
        public class Preview
        {
            public List<Row> Rows { get; set; }

            public Preview()
            {
                Rows = new List<Row>();
            }

            /// <summary>Số tài khoản sẽ được ghi số mới.</summary>
            public List<Row> Changes { get { return Rows.Where(r => r.WillUpdate).ToList(); } }

            /// <summary>Tài khoản không tra được số bên HRM — cần điền tay.</summary>
            public List<Row> Missing { get { return Rows.Where(r => !r.HasHrmPhone).ToList(); } }

            /// <summary>Đã có số và số đó khớp HRM — không phải làm gì.</summary>
            public int UpToDateCount
            {
                get { return Rows.Count(r => r.HasHrmPhone && !r.WillUpdate); }
            }
        }

        /// <summary>
        /// Đối chiếu toàn bộ tài khoản đang hoạt động với danh bạ HRM. KHÔNG ghi gì.
        /// </summary>
        public static Preview Build()
        {
            var result = new Preview();

            // Đọc cả danh bạ HRM về một lượt rồi tra trong bộ nhớ, thay vì hỏi lại cho từng
            // tài khoản — 780 người bên HRM mà hỏi từng lượt thì thành hàng chục lượt đi về.
            var phones = HrEmployeeStore.PhonesByEmail();

            foreach (var user in Repository.Users.All()
                .Where(u => u.IsActive)
                .OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase))
            {
                var email = (user.Email ?? string.Empty).Trim();

                string hrmPhone = null;
                if (email.Length > 0) phones.TryGetValue(email, out hrmPhone);

                result.Rows.Add(new Row
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    Email = email,
                    CurrentPhone = (user.Phone ?? string.Empty).Trim(),
                    HrmPhone = (hrmPhone ?? string.Empty).Trim()
                });
            }

            return result;
        }

        /// <summary>
        /// Đổ số từ HRM sang bảng Người dùng. Trả về số tài khoản đã được ghi số mới.
        /// </summary>
        public static int Apply()
        {
            var updated = 0;

            // Dựng lại danh sách từ dữ liệu hiện tại chứ không tin danh sách gửi từ màn xem
            // trước — người khác có thể vừa sửa số trong lúc màn đó còn đang mở.
            foreach (var row in Build().Changes)
            {
                var user = Repository.Users.Find(row.UserId);
                if (user == null) continue;

                user.Phone = row.HrmPhone;
                Repository.Users.Update(user);
                updated++;
            }

            return updated;
        }
    }
}
