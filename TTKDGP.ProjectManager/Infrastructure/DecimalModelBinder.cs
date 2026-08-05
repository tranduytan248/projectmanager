using System;
using System.Globalization;
using System.Web.Mvc;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Đọc số thập phân từ biểu mẫu bằng CẢ HAI lối viết: dấu chấm và dấu phẩy.
    ///
    /// Vì sao cần: ứng dụng chạy dưới culture vi-VN (xem Web.config và Global.asax), nên bộ đọc
    /// mặc định của MVC hiểu dấu phẩy là dấu thập phân còn dấu chấm là dấu phân nhóm nghìn. Nhưng
    /// ô &lt;input type="number"&gt; của trình duyệt LUÔN gửi lên theo chuẩn HTML — dấu chấm. Kết
    /// quả là người dùng nhập 0,25 thấy trên màn hình là 0.25 rồi gửi đi, máy chủ đọc trượt và
    /// lặng lẽ gán 0. Không có thông báo lỗi nào, chỉ có con số biến mất.
    ///
    /// Cách xử lý: thử đọc theo culture hiện hành trước (giữ nguyên thói quen gõ dấu phẩy của
    /// người dùng ở các ô chữ thường), trượt thì thử lại theo chuẩn bất biến.
    ///
    /// Chỉ nhận MỘT dấu ngăn duy nhất trong chuỗi. Chuỗi kiểu "1.234,56" hay "1,234.56" có cả hai
    /// dấu thì để bộ đọc chuẩn quyết định — đoán bừa ở đây dễ biến "1.234" (một nghìn hai trăm ba
    /// tư) thành 1,234.
    /// </summary>
    public class DecimalModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            var isNullable = Nullable.GetUnderlyingType(bindingContext.ModelType) != null;

            if (value == null)
            {
                return isNullable ? null : bindingContext.ModelMetadata.Model;
            }

            var raw = (value.AttemptedValue ?? string.Empty).Trim();

            if (raw.Length == 0)
            {
                // Ô để trống: kiểu cho phép null thì trả null, không thì coi như 0 — giống cách
                // bộ đọc mặc định xử lý, để trang không đỏ lỗi vì một ô người dùng cố ý bỏ trống.
                return isNullable ? (object)null : 0m;
            }

            decimal result;
            if (TryParse(raw, out result))
            {
                return result;
            }

            bindingContext.ModelState.AddModelError(bindingContext.ModelName,
                string.Format("Giá trị \"{0}\" không phải là một số hợp lệ.", raw));

            return isNullable ? null : (object)0m;
        }

        /// <summary>Đọc số theo culture hiện hành, trượt thì đổi dấu ngăn rồi đọc theo chuẩn bất biến.</summary>
        private static bool TryParse(string raw, out decimal result)
        {
            const NumberStyles styles = NumberStyles.Number;

            if (decimal.TryParse(raw, styles, CultureInfo.CurrentCulture, out result)) return true;
            if (decimal.TryParse(raw, styles, CultureInfo.InvariantCulture, out result)) return true;

            // Còn lại là trường hợp dấu ngăn ngược với culture: vi-VN nhận "0,25" nhưng ô number
            // gửi "0.25", hoặc ngược lại ở culture dùng dấu chấm. Chỉ đổi khi có ĐÚNG MỘT dấu ngăn
            // duy nhất trong chuỗi — nhiều dấu thì không đoán được đâu là phân nhóm nghìn.
            var dots = CountOf(raw, '.');
            var commas = CountOf(raw, ',');

            if (dots + commas == 1)
            {
                var normalized = raw.Replace(',', '.');
                return decimal.TryParse(normalized, styles, CultureInfo.InvariantCulture, out result);
            }

            result = 0m;
            return false;
        }

        private static int CountOf(string text, char c)
        {
            var count = 0;
            foreach (var ch in text)
            {
                if (ch == c) count++;
            }
            return count;
        }

        /// <summary>Gắn bộ đọc này cho decimal và decimal? trên toàn ứng dụng.</summary>
        public static void Register(ModelBinderDictionary binders)
        {
            var binder = new DecimalModelBinder();
            binders[typeof(decimal)] = binder;
            binders[typeof(decimal?)] = binder;
        }
    }
}
