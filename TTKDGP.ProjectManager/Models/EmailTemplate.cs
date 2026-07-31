using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Mẫu email của một loại thông báo cá nhân. Mã mẫu (Code) trùng với mã loại thông báo
    /// (<see cref="NotificationTypes"/>) — mỗi loại đúng một mẫu, sửa được trên màn quản trị.
    ///
    /// Nội dung là HTML kèm các tham số dạng {{TenThamSo}}; lúc gửi hệ thống thay tham số bằng
    /// giá trị thật (đã escape) rồi mới gửi đi.
    /// </summary>
    public class EmailTemplate : IEntity
    {
        public int Id { get; set; }

        /// <summary>Mã loại thông báo, xem <see cref="NotificationTypes"/>.</summary>
        public string Code { get; set; }

        /// <summary>Tên hiển thị trên màn quản trị.</summary>
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề mail")]
        [Display(Name = "Tiêu đề mail")]
        [StringLength(300, ErrorMessage = "Tiêu đề tối đa 300 ký tự")]
        public string Subject { get; set; }

        /// <summary>Thân mail dạng HTML, soạn bằng trình soạn thảo trên màn quản trị.</summary>
        /// <remarks>
        /// [Required] ở đây để nhãn tự hiện dấu bắt buộc; máy chủ vẫn kiểm lại bằng tay trong
        /// EmailTemplatesController vì nội dung là HTML — chuỗi rỗng kiểu "&lt;p&gt;&lt;/p&gt;"
        /// lọt qua được [Required].
        /// </remarks>
        [Required(ErrorMessage = "Nội dung mail không được để trống")]
        [Display(Name = "Nội dung mail")]
        public string Body { get; set; }

        /// <summary>Tắt là loại thông báo này không gửi mail nữa (thông báo trên web vẫn có).</summary>
        [Display(Name = "Đang gửi mail")]
        public bool IsActive { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    /// <summary>Một tham số chèn được vào mẫu email, kèm mô tả cho người soạn.</summary>
    public class EmailTemplateParam
    {
        public EmailTemplateParam(string key, string label)
        {
            Key = key;
            Label = label;
        }

        /// <summary>Tên tham số — trong mẫu viết dạng {{Key}}.</summary>
        public string Key { get; private set; }

        public string Label { get; private set; }
    }

    /// <summary>Định nghĩa cố định của một mẫu: tham số hợp lệ và nội dung mặc định.</summary>
    public class EmailTemplateDefinition
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<EmailTemplateParam> Params { get; set; }
        public string DefaultSubject { get; set; }
        public string DefaultBody { get; set; }

        public EmailTemplateDefinition()
        {
            Params = new List<EmailTemplateParam>();
        }
    }
}
