using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Nhãn ô nhập có đánh dấu bắt buộc: tiêu đề in đỏ kèm dấu * đứng sau.
    ///
    /// Dùng <c>@Html.LabelReq(m =&gt; m.Ten)</c> thay cho <c>@Html.LabelFor(...)</c>. Trường nào
    /// bắt buộc thì TỰ nhận ra từ chính model, không phải khai lại ở view — nhờ vậy sửa ràng buộc
    /// ở model là mọi giao diện đổi theo, không sợ nhãn nói một đằng máy chủ kiểm một nẻo.
    ///
    /// Trường bắt buộc theo nghiệp vụ nhưng KHÔNG khai được bằng attribute (ví dụ Hạn hoàn thành
    /// của công việc — cùng một model WorkTask nhưng luồng nhập tiến độ lại không bắt buộc) thì
    /// truyền <c>required: true</c> để đánh dấu tay.
    /// </summary>
    public static class LabelHelpers
    {
        /// <summary>
        /// Nhãn kèm dấu * nếu trường bắt buộc.
        /// </summary>
        /// <param name="required">
        /// Bỏ trống thì tự dò từ model. Truyền true/false để ép, dùng cho trường bắt buộc có điều
        /// kiện — ví dụ Mật khẩu chỉ bắt buộc lúc tạo tài khoản mới.
        /// </param>
        public static MvcHtmlString LabelReq<TModel, TValue>(
            this HtmlHelper<TModel> html,
            Expression<Func<TModel, TValue>> expression,
            bool? required = null,
            string labelText = null)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);
            var isRequired = required ?? IsRequired(metadata);

            var name = ExpressionHelper.GetExpressionText(expression);
            var text = labelText
                ?? metadata.DisplayName
                ?? metadata.PropertyName
                ?? name.Split('.').Last();

            return Render(html, name, text, isRequired);
        }

        /// <summary>
        /// Nhãn cho ô nhập KHÔNG gắn với model — các form gửi tham số rời (báo cáo tuần, ngày nghỉ
        /// KPI…). Không có metadata để dò nên phải nói rõ bắt buộc hay không.
        /// </summary>
        public static MvcHtmlString LabelReq(
            this HtmlHelper html, string fieldName, string labelText, bool required = true)
        {
            return Render(html, fieldName, labelText, required);
        }

        private static MvcHtmlString Render(HtmlHelper html, string fieldName, string text, bool isRequired)
        {
            var fullName = html.ViewContext.ViewData
                .TemplateInfo.GetFullHtmlFieldName(fieldName ?? string.Empty);

            var tag = new TagBuilder("label");
            if (!string.IsNullOrEmpty(fullName))
            {
                tag.Attributes.Add("for", TagBuilder.CreateSanitizedId(fullName));
            }

            var inner = new StringBuilder(html.Encode(text));
            if (isRequired)
            {
                tag.AddCssClass("is-required");

                // Dấu * để ngoài nhãn chữ và ẩn với trình đọc màn hình: người khiếm thị đã nghe
                // "bắt buộc" qua aria-hidden của ô nhập, đọc thêm "sao" chỉ gây nhiễu.
                inner.Append(" <span class=\"req-mark\" aria-hidden=\"true\">*</span>");
            }

            tag.InnerHtml = inner.ToString();
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }

        /// <summary>
        /// Trường có bắt buộc không, đọc từ ràng buộc đã khai trong model.
        ///
        /// Ngoài [Required] còn xét [Range(1, int.MaxValue)]: các ô CHỌN kiểu int (PM phụ trách,
        /// Thành viên, Dự án…) không dùng được [Required] vì 0 vẫn là số hợp lệ, nên cả mã nguồn
        /// đang chặn "chưa chọn" bằng cách đó.
        ///
        /// Phải xét cả cận trên chứ không chỉ cận dưới, vì [Range] còn được dùng với nghĩa khác
        /// hẳn là giới hạn miền giá trị — [Range(1, 53)] của ô chọn tuần chẳng hạn. Ô đó luôn có
        /// sẵn giá trị, không bỏ trống được, nên không phải "bắt buộc nhập". Chỉ dạng mở đến
        /// int.MaxValue mới đúng là "phải chọn một mục".
        /// </summary>
        private static bool IsRequired(ModelMetadata metadata)
        {
            if (metadata.IsRequired && !metadata.ModelType.IsValueType) return true;

            var container = metadata.ContainerType;
            if (container == null || string.IsNullOrEmpty(metadata.PropertyName)) return false;

            var property = container.GetProperty(metadata.PropertyName);
            if (property == null) return false;

            var attributes = property.GetCustomAttributes(true);
            if (attributes.OfType<RequiredAttribute>().Any()) return true;

            return attributes.OfType<RangeAttribute>().Any(r =>
                r.Minimum is int && (int)r.Minimum >= 1
                && r.Maximum is int && (int)r.Maximum == int.MaxValue);
        }
    }
}
