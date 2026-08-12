using System.Web.Mvc;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// HandleErrorAttribute gốc không để lại dấu vết gì khi có lỗi — người dùng chỉ thấy trang lỗi
    /// chung, còn lỗi thật nằm ở đâu thì không ai biết được nữa vì customErrors đã nuốt mất.
    /// Ghi lại toàn bộ lỗi ra file trước khi giao lại cho hành vi gốc, để lần sau gặp lại còn có
    /// stack trace mà tra thay vì phải đoán.
    ///
    /// Chỗ ghi là <see cref="ErrorLog"/> — dùng chung với Application_Error để mọi lỗi nằm gọn
    /// trong MỘT file. Hai nơi cùng bắt lỗi là có chủ đích: filter này thấy lỗi ném ra từ trong
    /// controller, còn Application_Error đỡ nốt phần ngoài tầm với của MVC (ví dụ HTML gửi lên bị
    /// chặn ở bước dựng model — lỗi đó xảy ra TRƯỚC khi filter kịp chạy).
    /// </summary>
    public class LoggingHandleErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext != null)
            {
                ErrorLog.Write(filterContext.Exception, filterContext.HttpContext);
            }

            base.OnException(filterContext);
        }
    }
}
