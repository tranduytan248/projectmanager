using System;

namespace TTKDGP.ProjectManager.Infrastructure
{
    public static class ViewHelpers
    {
        /// <summary>Chọn màu nhãn theo trạng thái công việc.</summary>
        public static string StatusBadgeClass(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "badge-inactive";

            if (status.IndexOf("thực hiện", StringComparison.CurrentCultureIgnoreCase) >= 0) return "badge-doing";
            if (status.IndexOf("hỗ trợ", StringComparison.CurrentCultureIgnoreCase) >= 0) return "badge-support";
            if (status.IndexOf("thử nghiệm", StringComparison.CurrentCultureIgnoreCase) >= 0) return "badge-doing";
            if (status.IndexOf("chờ", StringComparison.CurrentCultureIgnoreCase) >= 0) return "badge-waiting";
            if (status.IndexOf("tạm dừng", StringComparison.CurrentCultureIgnoreCase) >= 0) return "badge-waiting";
            if (status.IndexOf("hoàn thành", StringComparison.CurrentCultureIgnoreCase) >= 0) return "badge-active";

            return "badge-inactive";
        }

        /// <summary>
        /// Chữ cái đầu để làm ảnh đại diện. Tên Việt Nam đọc theo họ trước tên sau,
        /// nên lấy chữ đầu của tên riêng (từ cuối) ghép với chữ đầu của họ.
        /// </summary>
        public static string Initials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";

            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();

            var given = parts[parts.Length - 1].Substring(0, 1);
            var family = parts[0].Substring(0, 1);
            return (given + family).ToUpper();
        }

        /// <summary>Hiển thị "—" thay cho ô rỗng để bảng dễ đọc hơn.</summary>
        public static string OrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        }

        /// <summary>
        /// Hiển thị rút gọn khoá bí mật trong bảng: vài ký tự đầu rồi "…", để không phơi cả khoá
        /// mà vẫn nhận ra bản ghi. Xem/sao chép đủ ở màn hình sửa.
        /// </summary>
        public static string KeyPeek(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "—";
            return key.Length <= 10 ? key : key.Substring(0, 8) + "…";
        }

        /// <summary>Phần trăm của một giá trị so với giá trị lớn nhất, dùng để vẽ độ dài thanh.</summary>
        public static int BarWidth(int value, int max)
        {
            if (max <= 0) return 0;
            var pct = (int)Math.Round(value * 100.0 / max);
            return Math.Max(pct, value > 0 ? 3 : 0);
        }
    }
}
