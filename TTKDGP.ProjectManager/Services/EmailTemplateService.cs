using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Mẫu email của thông báo cá nhân: định nghĩa cố định (tham số hợp lệ + nội dung mặc định),
    /// bản chỉnh sửa lưu trong CSDL, phép thay tham số và phép gửi.
    ///
    /// Gửi mail luôn chạy ở LUỒNG NỀN và nuốt lỗi — mail là kênh phụ, không được làm chậm hay
    /// làm hỏng thao tác chính. Nội dung được dựng xong xuôi TRƯỚC khi đẩy sang luồng nền.
    /// </summary>
    public static class EmailTemplateService
    {
        // ---------- Định nghĩa cố định ----------

        /// <summary>
        /// Bốn mẫu, mã trùng mã loại thông báo. Đây là nguồn sự thật về THAM SỐ hợp lệ của từng
        /// mẫu; nội dung chỉ là giá trị mặc định — bản thật nằm trong bảng EmailTemplates.
        /// </summary>
        public static readonly List<EmailTemplateDefinition> Definitions = new List<EmailTemplateDefinition>
        {
            new EmailTemplateDefinition
            {
                Code = NotificationTypes.ProjectAdded,
                Name = "Được thêm vào dự án",
                Description = "Gửi cho người vừa được phân công vào một dự án.",
                Params =
                {
                    new EmailTemplateParam("HoTen", "Họ tên người nhận"),
                    new EmailTemplateParam("TenDuAn", "Tên dự án"),
                    new EmailTemplateParam("LienKet", "Liên kết mở hệ thống (cần đăng nhập)")
                },
                DefaultSubject = "[NCPT] Bạn được thêm vào dự án {{TenDuAn}}",
                DefaultBody = "<p>Chào {{HoTen}},</p>"
                    + "<p>Bạn vừa được thêm vào dự án <strong>{{TenDuAn}}</strong>.</p>"
                    + "<p><a href=\"{{LienKet}}\">Mở \"Dự án của tôi\" để xem chi tiết</a> (cần đăng nhập).</p>"
            },
            new EmailTemplateDefinition
            {
                Code = NotificationTypes.ProjectRemoved,
                Name = "Được rút khỏi dự án",
                Description = "Gửi cho người vừa kết thúc tham gia một dự án.",
                Params =
                {
                    new EmailTemplateParam("HoTen", "Họ tên người nhận"),
                    new EmailTemplateParam("TenDuAn", "Tên dự án"),
                    new EmailTemplateParam("LienKet", "Liên kết mở hệ thống (cần đăng nhập)")
                },
                DefaultSubject = "[NCPT] Bạn đã được rút khỏi dự án {{TenDuAn}}",
                DefaultBody = "<p>Chào {{HoTen}},</p>"
                    + "<p>Bạn đã được rút khỏi dự án <strong>{{TenDuAn}}</strong>.</p>"
                    + "<p><a href=\"{{LienKet}}\">Mở \"Dự án của tôi\" để xem lại</a> (cần đăng nhập).</p>"
            },
            new EmailTemplateDefinition
            {
                Code = NotificationTypes.Mentioned,
                Name = "Được nhắc tên trong trao đổi",
                Description = "Gửi khi có người nhắc (@) bạn trong trao đổi của một công việc.",
                Params =
                {
                    new EmailTemplateParam("HoTen", "Họ tên người nhận"),
                    new EmailTemplateParam("NguoiNhac", "Người đã nhắc bạn"),
                    new EmailTemplateParam("TenCongViec", "Tên công việc"),
                    new EmailTemplateParam("TenDuAn", "Tên dự án"),
                    new EmailTemplateParam("NoiDung", "Nội dung trao đổi"),
                    new EmailTemplateParam("TepDinhKem", "Liên kết file đính kèm (tự trống nếu không có)"),
                    new EmailTemplateParam("LienKet", "Liên kết mở trao đổi (cần đăng nhập)")
                },
                DefaultSubject = "[NCPT] {{NguoiNhac}} nhắc bạn trong công việc {{TenCongViec}}",
                DefaultBody = "<p>Chào {{HoTen}},</p>"
                    + "<p><strong>{{NguoiNhac}}</strong> đã nhắc bạn trong trao đổi của công việc "
                    + "<strong>{{TenCongViec}}</strong> (dự án {{TenDuAn}}):</p>"
                    + "<blockquote style=\"margin:8px 0;padding:8px 14px;border-left:3px solid #1a56a8;background:#f4f7fb\">{{NoiDung}}</blockquote>"
                    + "{{TepDinhKem}}"
                    + "<p><a href=\"{{LienKet}}\">Mở trao đổi trong hệ thống</a> (cần đăng nhập).</p>"
            },
            new EmailTemplateDefinition
            {
                Code = NotificationTypes.CommentAdded,
                Name = "Có trao đổi mới trong công việc",
                Description = "Gửi cho người tạo việc và người đang được giao việc khi có lượt "
                    + "trao đổi mới (trừ chính người vừa viết).",
                Params =
                {
                    new EmailTemplateParam("HoTen", "Họ tên người nhận"),
                    new EmailTemplateParam("NguoiTraoDoi", "Người vừa gửi trao đổi"),
                    new EmailTemplateParam("TenCongViec", "Tên công việc"),
                    new EmailTemplateParam("TenDuAn", "Tên dự án"),
                    new EmailTemplateParam("NoiDung", "Nội dung trao đổi"),
                    new EmailTemplateParam("TepDinhKem", "Liên kết file đính kèm (tự trống nếu không có)"),
                    new EmailTemplateParam("LienKet", "Liên kết mở trao đổi (cần đăng nhập)")
                },
                DefaultSubject = "[NCPT] {{NguoiTraoDoi}} vừa trao đổi trong công việc {{TenCongViec}}",
                DefaultBody = "<p>Chào {{HoTen}},</p>"
                    + "<p><strong>{{NguoiTraoDoi}}</strong> vừa gửi một lượt trao đổi mới trong công việc "
                    + "<strong>{{TenCongViec}}</strong> (dự án {{TenDuAn}}):</p>"
                    + "<blockquote style=\"margin:8px 0;padding:8px 14px;border-left:3px solid #1a56a8;background:#f4f7fb\">{{NoiDung}}</blockquote>"
                    + "{{TepDinhKem}}"
                    + "<p><a href=\"{{LienKet}}\">Mở trao đổi trong hệ thống</a> (cần đăng nhập).</p>"
            },
            new EmailTemplateDefinition
            {
                Code = NotificationTypes.TaskAssigned,
                Name = "Được giao việc riêng",
                Description = "Gửi khi Quản lý Tổ giao thẳng một việc riêng (ngoài dự án) cho bạn.",
                Params =
                {
                    new EmailTemplateParam("HoTen", "Họ tên người nhận"),
                    new EmailTemplateParam("NguoiGiao", "Người giao việc"),
                    new EmailTemplateParam("TenCongViec", "Tên công việc"),
                    new EmailTemplateParam("NoiDung", "Nội dung công việc"),
                    new EmailTemplateParam("HanHoanThanh", "Hạn hoàn thành (dd/MM/yyyy)"),
                    new EmailTemplateParam("DiemCong", "Điểm cộng KPI khi hoàn thành đúng hạn"),
                    new EmailTemplateParam("TepDinhKem", "Liên kết file đính kèm (tự trống nếu không có)"),
                    new EmailTemplateParam("LienKet", "Liên kết mở công việc (cần đăng nhập)")
                },
                DefaultSubject = "[NCPT] Bạn được giao việc riêng: {{TenCongViec}}",
                DefaultBody = "<p>Chào {{HoTen}},</p>"
                    + "<p><strong>{{NguoiGiao}}</strong> vừa giao cho bạn việc riêng "
                    + "<strong>{{TenCongViec}}</strong> — hạn hoàn thành <strong>{{HanHoanThanh}}</strong>, "
                    + "hoàn thành đúng hạn được cộng <strong>{{DiemCong}}</strong> điểm KPI.</p>"
                    + "<blockquote style=\"margin:8px 0;padding:8px 14px;border-left:3px solid #1a56a8;background:#f4f7fb\">{{NoiDung}}</blockquote>"
                    + "{{TepDinhKem}}"
                    + "<p><a href=\"{{LienKet}}\">Mở công việc trong hệ thống</a> (cần đăng nhập).</p>"
            },
            new EmailTemplateDefinition
            {
                Code = NotificationTypes.ProjectTaskAssigned,
                Name = "Được giao việc trong dự án",
                Description = "Gửi khi bạn được giao một đầu việc thuộc dự án, hoặc việc được "
                    + "chuyển sang cho bạn.",
                Params =
                {
                    new EmailTemplateParam("HoTen", "Họ tên người nhận"),
                    new EmailTemplateParam("NguoiGiao", "Người giao việc"),
                    new EmailTemplateParam("TenCongViec", "Tên công việc"),
                    new EmailTemplateParam("TenDuAn", "Tên dự án"),
                    new EmailTemplateParam("LoaiViec", "Loại việc: Triển khai hoặc Hỗ trợ"),
                    new EmailTemplateParam("NoiDung", "Nội dung công việc"),
                    new EmailTemplateParam("HanHoanThanh", "Hạn hoàn thành (dd/MM/yyyy)"),
                    new EmailTemplateParam("LienKet", "Liên kết mở công việc (cần đăng nhập)")
                },
                DefaultSubject = "[NCPT] Bạn được giao việc: {{TenCongViec}}",
                DefaultBody = "<p>Chào {{HoTen}},</p>"
                    + "<p><strong>{{NguoiGiao}}</strong> vừa giao cho bạn công việc "
                    + "<strong>{{TenCongViec}}</strong> thuộc dự án <strong>{{TenDuAn}}</strong> "
                    + "({{LoaiViec}}) — hạn hoàn thành <strong>{{HanHoanThanh}}</strong>.</p>"
                    + "<blockquote style=\"margin:8px 0;padding:8px 14px;border-left:3px solid #1a56a8;background:#f4f7fb\">{{NoiDung}}</blockquote>"
                    + "<p><a href=\"{{LienKet}}\">Mở công việc trong hệ thống</a> (cần đăng nhập).</p>"
            },
            new EmailTemplateDefinition
            {
                Code = NotificationTypes.DueSoon,
                Name = "Công việc gần tới hạn",
                Description = "Gửi khi công việc của bạn còn tối đa 1 ngày hoặc đã quá hạn.",
                Params =
                {
                    new EmailTemplateParam("HoTen", "Họ tên người nhận"),
                    new EmailTemplateParam("TenCongViec", "Tên công việc"),
                    new EmailTemplateParam("TenDuAn", "Tên dự án"),
                    new EmailTemplateParam("HanHoanThanh", "Hạn hoàn thành (dd/MM/yyyy)"),
                    new EmailTemplateParam("TinhTrang", "Tình trạng: sắp đến hạn / đã quá hạn"),
                    new EmailTemplateParam("LienKet", "Liên kết mở hệ thống (cần đăng nhập)")
                },
                DefaultSubject = "[NCPT] Công việc {{TenCongViec}} {{TinhTrang}}",
                DefaultBody = "<p>Chào {{HoTen}},</p>"
                    + "<p>Công việc <strong>{{TenCongViec}}</strong> (dự án {{TenDuAn}}) "
                    + "<strong>{{TinhTrang}}</strong> — hạn hoàn thành {{HanHoanThanh}}.</p>"
                    + "<p><a href=\"{{LienKet}}\">Mở hệ thống để cập nhật tiến độ</a> (cần đăng nhập).</p>"
            }
        };

        public static EmailTemplateDefinition Definition(string code)
        {
            return Definitions.FirstOrDefault(d => d.Code == code);
        }

        // ---------- Bản lưu trong CSDL ----------

        /// <summary>Tạo các dòng mẫu còn thiếu từ định nghĩa mặc định. Idempotent.</summary>
        public static void EnsureDefaults()
        {
            try
            {
                var existing = new HashSet<string>(
                    Repository.EmailTemplates.All().Select(t => t.Code),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var d in Definitions.Where(d => !existing.Contains(d.Code)))
                {
                    Repository.EmailTemplates.Insert(new EmailTemplate
                    {
                        Code = d.Code,
                        Name = d.Name,
                        Subject = d.DefaultSubject,
                        Body = d.DefaultBody,
                        IsActive = true
                    });
                }
            }
            catch (Exception)
            {
                // SQL chập chờn thì bỏ qua, lần mở màn quản trị sau tạo tiếp.
            }
        }

        /// <summary>Mẫu đang hiệu lực của một loại; chưa có trong CSDL thì dùng bản mặc định.</summary>
        public static EmailTemplate Get(string code)
        {
            var saved = Repository.EmailTemplates.FirstOrDefault(
                t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
            if (saved != null) return saved;

            var d = Definition(code);
            if (d == null) return null;

            return new EmailTemplate
            {
                Code = d.Code,
                Name = d.Name,
                Subject = d.DefaultSubject,
                Body = d.DefaultBody,
                IsActive = true
            };
        }

        // ---------- Thay tham số ----------

        /// <summary>Thay {{Key}} bằng giá trị chữ thường (không escape) — dùng cho tiêu đề.</summary>
        private static string RenderText(string template, IDictionary<string, string> values)
        {
            var result = template ?? string.Empty;
            foreach (var pair in values)
            {
                result = result.Replace("{{" + pair.Key + "}}", pair.Value ?? string.Empty);
            }
            return result;
        }

        /// <summary>
        /// Thay tham số vào thân HTML. Giá trị chữ được HTML-encode trước khi thay — nội dung
        /// trao đổi là văn bản người dùng nhập, không được để lọt thẻ vào mail. Riêng htmlValues
        /// là các mảnh HTML do chính hệ thống dựng (liên kết file đính kèm) thì thay nguyên vẹn.
        /// </summary>
        private static string RenderHtml(string template, IDictionary<string, string> textValues,
            IDictionary<string, string> htmlValues)
        {
            var result = template ?? string.Empty;

            foreach (var pair in textValues)
            {
                result = result.Replace("{{" + pair.Key + "}}",
                    HttpUtility.HtmlEncode(pair.Value ?? string.Empty));
            }

            if (htmlValues != null)
            {
                foreach (var pair in htmlValues)
                {
                    result = result.Replace("{{" + pair.Key + "}}", pair.Value ?? string.Empty);
                }
            }

            return result;
        }

        /// <summary>Khung chung của mọi mail: font hệ thống và dòng chân "mail tự động".</summary>
        private static string Wrap(string body)
        {
            return "<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#25324a;line-height:1.6\">"
                + body
                + "<hr style=\"border:none;border-top:1px solid #e0e5ee;margin:18px 0 10px\" />"
                + "<p style=\"font-size:12px;color:#8b94a7\">Mail tự động từ hệ thống Quản lý dự án NCPT — "
                + HttpUtility.HtmlEncode(AppSettings.PublicUrl) + ". Vui lòng không trả lời mail này.</p>"
                + "</div>";
        }

        // ---------- Gửi ----------

        /// <summary>
        /// Gửi mail một loại thông báo cho một người dùng, chạy ở luồng nền. Tự thêm tham số
        /// HoTen từ hồ sơ người nhận. Chưa cấu hình SMTP, người nhận không có email, hoặc mẫu
        /// đang tắt thì lặng lẽ bỏ qua.
        /// </summary>
        public static void Send(int userId, string code, Dictionary<string, string> textValues,
            Dictionary<string, string> htmlValues = null)
        {
            try
            {
                if (!AppSettings.Email.IsConfigured) return;

                var user = Repository.Users.Find(userId);
                if (user == null || string.IsNullOrWhiteSpace(user.Email)) return;

                var template = Get(code);
                if (template == null || !template.IsActive) return;

                var values = new Dictionary<string, string>(textValues ?? new Dictionary<string, string>());
                if (!values.ContainsKey("HoTen")) values["HoTen"] = user.FullName;

                // Dựng xong nội dung TRƯỚC khi sang luồng nền — luồng nền chỉ việc gửi.
                var subject = RenderText(template.Subject, values);
                var body = Wrap(RenderHtml(template.Body, values, htmlValues));
                var to = user.Email.Trim();

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    // Mail là kênh phụ nên lỗi KHÔNG được làm hỏng thao tác đang chạy, nhưng phải
                    // để lại dấu vết: nuốt trọn thì máy chủ ngừng gửi mail cả tháng cũng không ai
                    // hay. Trace ghi được cả ở bản Release, khác với Debug.
                    try
                    {
                        var result = EmailClient.Send(to, subject, body);
                        if (result != null && !result.Ok)
                        {
                            Trace.WriteLine(string.Format(
                                "[Email] Gui that bai toi {0} (mau {1}): {2}", to, code, result.Error));
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(string.Format(
                            "[Email] Loi gui toi {0} (mau {1}): {2}", to, code, ex.Message));
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("[Email] Loi dung noi dung (mau {0}): {1}", code, ex.Message));
            }
        }

        /// <summary>
        /// Gửi thử mẫu với dữ liệu minh hoạ tới một địa chỉ — cho nút "Gửi thử" trên màn soạn mẫu.
        /// Gửi ĐỒNG BỘ để báo được kết quả ngay cho người bấm.
        /// </summary>
        public static EmailResult SendSample(string code, string toEmail, string toName)
        {
            var template = Get(code);
            if (template == null) return EmailResult.Fail("Không có mẫu này.");

            var values = SampleValues(code);
            values["HoTen"] = string.IsNullOrWhiteSpace(toName) ? "Người dùng" : toName;

            var htmlValues = new Dictionary<string, string>
            {
                { "TepDinhKem", "<p>File đính kèm: <a href=\"" + AppSettings.PublicLink
                    + "\">tai-lieu-mau.pdf</a> <em>(đăng nhập để tải)</em></p>" }
            };

            var subject = RenderText(template.Subject, values);
            var body = Wrap(RenderHtml(template.Body, values, htmlValues));

            return EmailClient.Send(toEmail, subject, body);
        }

        /// <summary>Bộ giá trị minh hoạ cho mail gửi thử.</summary>
        private static Dictionary<string, string> SampleValues(string code)
        {
            return new Dictionary<string, string>
            {
                { "TenDuAn", "Dự án minh hoạ" },
                { "TenCongViec", "Công việc minh hoạ" },
                { "NguoiNhac", "Nguyễn Văn A" },
                { "NguoiTraoDoi", "Nguyễn Văn A" },
                { "NoiDung", "Đây là nội dung trao đổi minh hoạ cho mail gửi thử." },
                { "HanHoanThanh", DateTime.Today.AddDays(1).ToString("dd/MM/yyyy") },
                { "TinhTrang", "sắp đến hạn (còn 1 ngày)" },
                { "LienKet", AppSettings.PublicLink ?? "#" }
            };
        }
    }
}
