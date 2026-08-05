using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Đọc/ghi phần cấu hình được của danh mục chức năng — xem <see cref="PermissionSetting"/>.
    ///
    /// KHÔNG BAO GIỜ ném lỗi ra ngoài: lớp này nằm trên đường đi của MỌI lượt kiểm quyền, để lọt
    /// một lỗi kết nối là cả hệ thống không ai vào được màn nào. Đọc trượt thì coi như chưa cấu
    /// hình gì — tức mọi chức năng đều bật, đúng như trước khi có màn này.
    /// </summary>
    public static class PermissionSettingService
    {
        private const string CacheKey = "TTKDGP:PermSettings";

        /// <summary>
        /// Toàn bộ dòng cấu hình, khoá theo mã chức năng. Nhớ trong phạm vi MỘT request: một lượt
        /// mở trang hỏi quyền hàng chục lần (bộ lọc AppAuthorize, ViewBag.Can trong view, dựng
        /// menu), mà mỗi lượt hỏi lại quét cả bảng.
        ///
        /// Nhớ theo request là mức an toàn nhất: bật/tắt có hiệu lực ngay ở lượt xem kế tiếp,
        /// không phải chờ hết hạn bộ đệm hay khởi động lại.
        /// </summary>
        private static Dictionary<string, PermissionSetting> Map
        {
            get
            {
                var context = HttpContext.Current;

                if (context != null)
                {
                    var cached = context.Items[CacheKey] as Dictionary<string, PermissionSetting>;
                    if (cached != null) return cached;
                }

                var map = Load();

                if (context != null) context.Items[CacheKey] = map;
                return map;
            }
        }

        private static Dictionary<string, PermissionSetting> Load()
        {
            var map = new Dictionary<string, PermissionSetting>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var row in Repository.PermissionSettings.All())
                {
                    if (string.IsNullOrWhiteSpace(row.Code)) continue;
                    map[row.Code.Trim()] = row;
                }
            }
            catch (Exception ex)
            {
                // SQL chập chờn: coi như chưa cấu hình gì (mọi chức năng bật). Có ghi lại —
                // nuốt lỗi hoàn toàn thì chức năng đang tắt bỗng bật lại mà không ai lần ra.
                System.Diagnostics.Debug.WriteLine(
                    "Đọc cấu hình chức năng trượt, dùng danh mục gốc: " + ex.Message);
            }

            return map;
        }

        /// <summary>Bỏ bản nhớ trong request để phần còn lại của lượt xử lý này thấy giá trị mới.</summary>
        private static void ClearRequestCache()
        {
            var context = HttpContext.Current;
            if (context != null) context.Items.Remove(CacheKey);
        }

        // ---------- Tra cứu ----------

        /// <summary>
        /// Chức năng này có đang mở không. Mã chưa có dòng cấu hình thì mặc định MỞ — chức năng
        /// mới thêm vào mã nguồn chạy được ngay, không phải chờ bật tay.
        /// </summary>
        public static bool IsEnabled(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return true;

            PermissionSetting row;
            return !Map.TryGetValue(code.Trim(), out row) || row.IsEnabled;
        }

        /// <summary>Tên hiển thị của một chức năng: tên đã sửa nếu có, không thì tên gốc.</summary>
        public static string DisplayName(string code, string fallback)
        {
            if (string.IsNullOrWhiteSpace(code)) return fallback;

            PermissionSetting row;
            if (Map.TryGetValue(code.Trim(), out row) && !string.IsNullOrWhiteSpace(row.DisplayName))
            {
                return row.DisplayName;
            }

            return fallback;
        }

        /// <summary>Dòng cấu hình của một mã, null nếu chưa có.</summary>
        public static PermissionSetting Find(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            PermissionSetting row;
            return Map.TryGetValue(code.Trim(), out row) ? row : null;
        }

        /// <summary>Các mã đang bị TẮT — dùng để cảnh báo trên màn hình.</summary>
        public static List<string> DisabledCodes()
        {
            return Map.Values
                .Where(r => !r.IsEnabled && !string.IsNullOrWhiteSpace(r.Code))
                .Select(r => r.Code)
                .ToList();
        }

        // ---------- Ghi ----------

        /// <summary>
        /// Lưu tên hiển thị và trạng thái của một mã. Chỉ nhận mã CÓ THẬT trong danh mục — mã lạ
        /// bị bỏ qua, vì nó không tương ứng endpoint nào và chỉ làm bảng rác thêm.
        /// Trả về false nếu mã không hợp lệ.
        /// </summary>
        public static bool Save(string code, string displayName, bool isEnabled, string note,
            string userFullName)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            var trimmed = code.Trim();
            if (!Permissions.AllCodes().Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            var existing = Repository.PermissionSettings.FirstOrDefault(
                r => string.Equals(r.Code, trimmed, StringComparison.OrdinalIgnoreCase));

            var row = existing ?? new PermissionSetting { Code = trimmed };

            row.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
            row.IsEnabled = isEnabled;
            row.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            row.UpdatedAt = DateTime.Now;
            row.UpdatedBy = userFullName;

            if (existing == null) Repository.PermissionSettings.Insert(row);
            else Repository.PermissionSettings.Update(row);

            ClearRequestCache();
            return true;
        }

        /// <summary>
        /// Bật/tắt hàng loạt theo danh sách mã ĐANG BẬT gửi lên từ biểu mẫu. Mã có thật nhưng
        /// không nằm trong danh sách thì bị tắt.
        ///
        /// Chỉ ghi những mã THỰC SỰ đổi trạng thái: bảng này nằm trên đường kiểm quyền, ghi lại
        /// cả trăm dòng không đổi mỗi lần bấm lưu là tự tạo tải vô ích.
        /// Trả về số mã đã đổi.
        /// </summary>
        public static int SaveEnabledSet(IEnumerable<string> enabledCodes, string userFullName)
        {
            var enabled = new HashSet<string>(
                (enabledCodes ?? Enumerable.Empty<string>())
                    .Select(c => (c ?? string.Empty).Trim())
                    .Where(c => c.Length > 0),
                StringComparer.OrdinalIgnoreCase);

            var changed = 0;

            foreach (var code in Permissions.AllCodes())
            {
                var shouldEnable = enabled.Contains(code);
                if (IsEnabled(code) == shouldEnable) continue;

                var existing = Find(code);
                Save(code, existing == null ? null : existing.DisplayName, shouldEnable,
                    existing == null ? null : existing.Note, userFullName);
                changed++;
            }

            return changed;
        }

        /// <summary>Xoá mọi tuỳ chỉnh, đưa danh mục chức năng về nguyên trạng mã nguồn.</summary>
        public static int ResetAll()
        {
            var removed = Repository.PermissionSettings.DeleteWhere(r => true);
            ClearRequestCache();
            return removed;
        }
    }
}
