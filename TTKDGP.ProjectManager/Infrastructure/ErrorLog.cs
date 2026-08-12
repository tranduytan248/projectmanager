using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Ghi lỗi chưa bắt được ra file để còn biết đường mà sửa.
    ///
    /// Trước đây mọi lỗi 500 đều biến mất không dấu vết: customErrors đẩy người dùng về trang chủ,
    /// còn hộp thoại gửi bằng AJAX thì chỉ hiện "Không lưu được. Hãy thử lại." — không ai biết
    /// máy chủ hỏng ở đâu. File này là chỗ duy nhất trả lời câu hỏi đó.
    ///
    /// Ghi vào App_Data vì thư mục đó KHÔNG phục vụ thẳng qua web (lỗi hay kèm dữ liệu nhạy cảm)
    /// và cũng nằm ngoài kho mã nguồn nên bản deploy đè lên không xoá mất.
    /// </summary>
    public static class ErrorLog
    {
        private static readonly object Gate = new object();

        // Giữ log gọn: quá cỡ thì đổi tên thành .1 (ghi đè bản .1 cũ) rồi mở file mới. Chỉ giữ
        // hai đời — mục đích là xem lỗi vừa xảy ra, không phải lưu trữ lâu dài.
        private const long MaxBytes = 5 * 1024 * 1024;

        public static void Write(Exception ex, HttpContext context)
        {
            Write(ex, context == null ? null : new HttpContextWrapper(context));
        }

        /// <summary>Bản dùng cho tầng MVC, nơi ngữ cảnh đi lại dưới dạng HttpContextBase.</summary>
        public static void Write(Exception ex, HttpContextBase context)
        {
            if (ex == null) return;

            try
            {
                var text = Format(ex, context);
                var file = LogFile();
                Directory.CreateDirectory(Path.GetDirectoryName(file));

                lock (Gate)
                {
                    Rotate(file);
                    File.AppendAllText(file, text, Encoding.UTF8);
                }
            }
            catch
            {
                // Ghi log mà ném lỗi thì thành che mất lỗi thật. Im lặng bỏ qua.
            }
        }

        private static void Rotate(string file)
        {
            var info = new FileInfo(file);
            if (!info.Exists || info.Length < MaxBytes) return;

            var old = file + ".1";
            if (File.Exists(old)) File.Delete(old);
            File.Move(file, old);
        }

        private static string Format(Exception ex, HttpContextBase context)
        {
            var sb = new StringBuilder();
            sb.AppendLine(new string('-', 78));
            sb.AppendFormat("Thoi diem : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now).AppendLine();

            if (context != null && context.Request != null)
            {
                var req = context.Request;
                sb.AppendFormat("Duong dan : {0} {1}", req.HttpMethod, req.RawUrl).AppendLine();

                var user = context.User != null && context.User.Identity != null
                    && context.User.Identity.IsAuthenticated
                        ? context.User.Identity.Name
                        : "(chua dang nhap)";
                sb.AppendFormat("Nguoi dung: {0}", user).AppendLine();
                sb.AppendFormat("Dia chi   : {0}", req.UserHostAddress).AppendLine();
            }

            // Lần theo cả chuỗi lỗi lồng nhau: lỗi SQL thật thường nằm ở tầng trong cùng.
            var level = 0;
            for (var e = ex; e != null; e = e.InnerException, level++)
            {
                sb.AppendLine();
                sb.AppendFormat("[{0}] {1}", level == 0 ? "Loi" : "Loi ben trong", e.GetType().FullName).AppendLine();
                sb.AppendFormat("     {0}", e.Message).AppendLine();
                if (!string.IsNullOrEmpty(e.StackTrace)) sb.AppendLine(e.StackTrace);
            }

            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>Đường dẫn file log. Ngoài IIS (chạy nền) thì lấy theo thư mục ứng dụng.</summary>
        public static string LogFile()
        {
            var dir = HostingEnvironment.IsHosted
                ? HostingEnvironment.MapPath("~/App_Data")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");

            return Path.Combine(dir, "errors.log");
        }
    }
}
