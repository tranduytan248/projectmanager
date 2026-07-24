using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>Kết quả một lần gửi mail.</summary>
    public class EmailResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }

        public static EmailResult Success()
        {
            return new EmailResult { Ok = true };
        }

        public static EmailResult Fail(string error)
        {
            return new EmailResult { Ok = false, Error = error };
        }
    }

    /// <summary>
    /// Gửi mail qua máy chủ SMTP của VNPT. Dùng System.Net.Mail sẵn có của
    /// .NET Framework, không thêm thư viện ngoài — cùng cách làm với TelegramClient.
    /// </summary>
    public static class EmailClient
    {
        static EmailClient()
        {
            // Cổng 587 dùng STARTTLS; máy chủ chỉ chấp nhận TLS 1.2 trở lên.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Gửi một mail HTML tới một người nhận. Mỗi lần gọi mở một kết nối riêng:
        /// số người nhận mỗi tuần chỉ vài chục nên không cần gom chung phiên.
        /// </summary>
        public static EmailResult Send(string toAddress, string subject, string htmlBody)
        {
            if (!AppSettings.Email.IsConfigured)
            {
                return EmailResult.Fail("Chưa cấu hình đủ máy chủ mail (cần bật tính năng, có host, tài khoản và mật khẩu).");
            }

            if (string.IsNullOrWhiteSpace(toAddress)) return EmailResult.Fail("Chưa có địa chỉ người nhận.");
            if (string.IsNullOrWhiteSpace(htmlBody)) return EmailResult.Fail("Nội dung mail rỗng.");

            try
            {
                using (var message = new MailMessage())
                using (var client = new SmtpClient(AppSettings.Email.Host, AppSettings.Email.Port))
                {
                    message.From = new MailAddress(AppSettings.Email.From, AppSettings.Email.DisplayName, Encoding.UTF8);
                    message.To.Add(new MailAddress(toAddress.Trim()));
                    message.Subject = subject;
                    message.SubjectEncoding = Encoding.UTF8;
                    message.Body = htmlBody;
                    message.BodyEncoding = Encoding.UTF8;
                    message.IsBodyHtml = true;

                    client.EnableSsl = AppSettings.Email.EnableSsl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    // Phải đặt UseDefaultCredentials = false TRƯỚC khi gán Credentials,
                    // nếu không .NET sẽ xoá thông tin đăng nhập vừa gán.
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(AppSettings.Email.User, AppSettings.Email.Password);
                    client.Timeout = 30000;

                    client.Send(message);
                }

                return EmailResult.Success();
            }
            catch (SmtpException ex)
            {
                // Mã lỗi SMTP nói rõ nguyên nhân hơn hẳn thông báo mặc định.
                return EmailResult.Fail(string.Format("{0} (mã {1})", ex.Message, ex.StatusCode));
            }
            catch (Exception ex)
            {
                return EmailResult.Fail(ex.Message);
            }
        }

        /// <summary>Gửi thử một mail để kiểm tra cấu hình từ màn hình Thông báo.</summary>
        public static EmailResult SendTest(string toAddress)
        {
            var body = new StringBuilder();
            body.AppendLine("<p>Đây là mail kiểm tra cấu hình từ " + AppSettings.Email.DisplayName + ".</p>");
            body.AppendLine("<p>Nếu anh/chị nhận được mail này thì phần gửi mail đã hoạt động bình thường.</p>");

            return Send(toAddress, "Kiểm tra cấu hình gửi mail", body.ToString());
        }
    }
}
