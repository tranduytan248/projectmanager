using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Bộ lọc HTML cho nội dung nhập từ trình soạn thảo WYSIWYG (Mô tả dự án).
    ///
    /// Nguyên tắc: DANH SÁCH TRẮNG — chỉ giữ đúng các thẻ định dạng văn bản vô hại, mọi thẻ khác
    /// bị bỏ (giữ lại phần chữ bên trong). Thuộc tính bị bỏ sạch, trừ href của thẻ a và chỉ khi
    /// là http/https/mailto — nhờ vậy không còn chỗ cho script, style hay on-event lọt qua.
    ///
    /// Nội dung này về sau hiển thị bằng Html.Raw, nên MỌI đường vào (lưu form, import...) đều
    /// phải đi qua Clean; khi hiển thị lại lọc thêm lần nữa qua ToDisplay cho chắc.
    /// </summary>
    public static class HtmlSanitizer
    {
        /// <summary>Các thẻ được giữ. div/p/br cho xuống dòng vì contenteditable mỗi trình duyệt sinh một kiểu.</summary>
        private static readonly HashSet<string> AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "p", "div", "br", "b", "strong", "i", "em", "u", "s", "strike",
            "ul", "ol", "li", "blockquote", "h3", "h4", "a"
        };

        private static readonly Regex HrefPattern = new Regex(
            "href\\s*=\\s*(\"(?<v>[^\"]*)\"|'(?<v>[^']*)'|(?<v>[^\\s>]+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Lọc một đoạn HTML về tập thẻ an toàn. Trả về null khi nội dung rỗng
        /// hoặc chỉ còn khoảng trắng sau khi lọc.
        /// </summary>
        public static string Clean(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            var sb = new StringBuilder(html.Length);
            var i = 0;

            while (i < html.Length)
            {
                var c = html[i];
                if (c != '<')
                {
                    sb.Append(c);
                    i++;
                    continue;
                }

                var end = html.IndexOf('>', i);
                if (end < 0)
                {
                    // Dấu "<" mồ côi cuối chuỗi — mã hoá lại để không thành thẻ dở dang.
                    sb.Append("&lt;");
                    i++;
                    continue;
                }

                sb.Append(CleanTag(html.Substring(i + 1, end - i - 1)));
                i = end + 1;
            }

            var result = sb.ToString().Trim();

            // Chỉ còn thẻ rỗng (kiểu "<div><br /></div>") thì coi như không có mô tả.
            var textOnly = Regex.Replace(result, "<[^>]+>", string.Empty);
            return string.IsNullOrWhiteSpace(HttpUtility.HtmlDecode(textOnly)) ? null : result;
        }

        /// <summary>
        /// Chuẩn bị một giá trị đã lưu để hiển thị bằng Html.Raw. Dữ liệu cũ là chữ thường
        /// (chưa có thẻ nào) thì mã hoá và đổi xuống dòng thành &lt;br /&gt; để không mất định dạng.
        /// </summary>
        public static string ToDisplay(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return null;

            if (stored.IndexOf('<') < 0)
            {
                return HttpUtility.HtmlEncode(stored).Replace("\r\n", "\n").Replace("\n", "<br />");
            }

            return Clean(stored);
        }

        /// <summary>
        /// Gỡ hết thẻ, trả về phần chữ thuần. Dùng khi cần DÒ NỘI DUNG chứ không phải hiển thị —
        /// ví dụ tìm cụm "@Tên" để gửi thông báo nhắc tên.
        ///
        /// Cần thiết vì trình soạn thảo có thể xen thẻ vào giữa một cái tên: người dùng bôi đậm
        /// mỗi chữ "Hoàng" thì nội dung thành "@Nguyễn Ngọc Châu &lt;b&gt;Hoàng&lt;/b&gt;", so
        /// chuỗi thô sẽ không khớp và thông báo lặng lẽ không gửi.
        ///
        /// Đổi thẻ xuống dòng thành khoảng trắng để hai chữ ở hai dòng không dính liền nhau.
        /// </summary>
        public static string ToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var sb = new StringBuilder(html.Length);
            var insideTag = false;

            foreach (var c in html)
            {
                if (c == '<') { insideTag = true; sb.Append(' '); continue; }
                if (c == '>') { insideTag = false; continue; }
                if (!insideTag) sb.Append(c);
            }

            // Giải mã thực thể (&amp; &#64; ...) rồi gom khoảng trắng thừa do việc gỡ thẻ sinh ra.
            var text = HttpUtility.HtmlDecode(sb.ToString());
            return Regex.Replace(text, "\\s+", " ").Trim();
        }

        /// <summary>Dựng lại một thẻ sạch từ phần ruột giữa "&lt;" và "&gt;"; thẻ lạ trả về chuỗi rỗng.</summary>
        private static string CleanTag(string inner)
        {
            var trimmed = inner.Trim();
            if (trimmed.Length == 0) return string.Empty;

            var closing = trimmed.StartsWith("/", StringComparison.Ordinal);
            var body = closing ? trimmed.Substring(1).TrimStart() : trimmed;

            var nameEnd = 0;
            while (nameEnd < body.Length && (char.IsLetterOrDigit(body[nameEnd]))) nameEnd++;
            var name = body.Substring(0, nameEnd).ToLowerInvariant();

            if (name.Length == 0 || !AllowedTags.Contains(name)) return string.Empty;

            if (closing)
            {
                return name == "br" ? string.Empty : "</" + name + ">";
            }

            if (name == "br") return "<br />";

            if (name == "a")
            {
                var href = SafeHref(body);
                if (href == null) return "<a>";

                // target/rel đặt cứng ở đây chứ không nhận từ người dùng.
                return "<a href=\"" + HttpUtility.HtmlAttributeEncode(href)
                     + "\" target=\"_blank\" rel=\"noopener noreferrer\">";
            }

            return "<" + name + ">";
        }

        /// <summary>Lấy href nếu là liên kết http/https/mailto; mọi giao thức khác đều bỏ.</summary>
        private static string SafeHref(string tagBody)
        {
            var match = HrefPattern.Match(tagBody);
            if (!match.Success) return null;

            var value = HttpUtility.HtmlDecode(match.Groups["v"].Value).Trim();
            if (value.Length == 0) return null;

            if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            return null;
        }
    }
}
