using System;
using System.Security.Cryptography;
using System.Text;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Sinh khoá bí mật cho hệ thống tích hợp và tính checksum theo khoá đó.
    ///
    /// Cách dùng khi tích hợp: bên gửi ghép chuỗi dữ liệu với khoá bí mật rồi băm SHA-256,
    /// gửi kèm checksum; bên nhận tính lại theo cùng công thức và đối chiếu. Trùng nghĩa là
    /// dữ liệu không bị sửa và người gửi biết khoá (đúng nguồn).
    /// </summary>
    public static class IntegrationKey
    {
        /// <summary>Độ dài khoá mặc định (byte). 32 byte = 64 ký tự hex, đủ mạnh cho ký checksum.</summary>
        private const int DefaultBytes = 32;

        /// <summary>
        /// Sinh khoá ngẫu nhiên bằng bộ sinh số ngẫu nhiên an toàn mật mã, trả về dạng hex thường.
        /// Dùng hex cho dễ đọc/sao chép và không dính ký tự đặc biệt khi ghép chuỗi.
        /// </summary>
        public static string Generate(int bytes = DefaultBytes)
        {
            if (bytes < 16) bytes = 16;

            var buffer = new byte[bytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(buffer);
            }

            return ToHex(buffer);
        }

        /// <summary>
        /// Tính checksum theo chuẩn tích hợp: ghép các trường theo đúng thứ tự bằng ký tự "|",
        /// đặt khoá bí mật ở CUỐI cùng, rồi băm SHA-256, trả về hex thường.
        ///
        /// Quy tắc (khớp với bên đối tác, ví dụ VNPT Money):
        ///   • Thứ tự trường đúng như bảng mô tả bản tin đầu vào (từ trên xuống).
        ///   • Các trường ngăn nhau bởi "|".
        ///   • Trường không bắt buộc mà rỗng thì để chuỗi rỗng (vẫn giữ dấu "|").
        ///   • Khoá bí mật luôn đứng cuối.
        /// </summary>
        public static string Sign(string secretKey, params string[] fields)
        {
            var count = fields != null ? fields.Length : 0;
            var parts = new string[count + 1];
            for (var i = 0; i < count; i++) parts[i] = fields[i] ?? string.Empty;
            parts[count] = secretKey ?? string.Empty;

            using (var sha = SHA256.Create())
            {
                var input = Encoding.UTF8.GetBytes(string.Join("|", parts));
                return ToHex(sha.ComputeHash(input));
            }
        }

        /// <summary>
        /// Kiểm checksum do bên gửi cung cấp có khớp với checksum tính lại từ các trường không.
        /// So sánh không phân biệt hoa thường và theo thời gian cố định để không lộ manh mối qua
        /// thời gian phản hồi.
        /// </summary>
        public static bool Verify(string provided, string secretKey, params string[] fields)
        {
            if (string.IsNullOrWhiteSpace(provided)) return false;
            return FixedTimeEquals(provided.Trim(), Sign(secretKey, fields));
        }

        /// <summary>
        /// So khoá bí mật đối tác gửi lên với khoá đang lưu của hệ thống. Dùng cho các API truyền
        /// thẳng khoá thay vì ký checksum. So theo thời gian cố định để không lộ manh mối qua
        /// thời gian phản hồi.
        /// </summary>
        public static bool Matches(string provided, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(provided) || string.IsNullOrWhiteSpace(secretKey)) return false;
            return FixedTimeEquals(provided.Trim(), secretKey.Trim());
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                // So từng ký tự đã hạ hoa; checksum là hex nên chỉ chữ số và a-f.
                diff |= char.ToLowerInvariant(a[i]) ^ char.ToLowerInvariant(b[i]);
            }
            return diff == 0;
        }

        /// <summary>
        /// So khoá dạng chuỗi có phải khoá do hàm này sinh ra không (chỉ gồm ký tự hex, đủ dài).
        /// Dùng để nhận biết giá trị hợp lệ, không phải để bảo mật.
        /// </summary>
        public static bool LooksValid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 32) return false;

            foreach (var c in value)
            {
                var isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex) return false;
            }

            return true;
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
