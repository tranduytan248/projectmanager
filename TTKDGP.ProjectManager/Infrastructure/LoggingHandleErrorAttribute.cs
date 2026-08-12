using System;
using System.IO;
using System.Web.Hosting;
using System.Web.Mvc;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// HandleErrorAttribute gốc không để lại dấu vết gì khi có lỗi — người dùng chỉ thấy trang lỗi
    /// chung, còn lỗi thật nằm ở đâu thì không ai biết được nữa vì customErrors đã nuốt mất.
    /// Ghi lại toàn bộ lỗi ra file trước khi giao lại cho hành vi gốc, để lần sau gặp lại còn có
    /// stack trace mà tra thay vì phải đoán.
    /// </summary>
    public class LoggingHandleErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            TryLog(filterContext);
            base.OnException(filterContext);
        }

        private static void TryLog(ExceptionContext filterContext)
        {
            try
            {
                if (filterContext == null || filterContext.Exception == null) return;

                var dir = HostingEnvironment.MapPath("~/App_Data/errors");
                if (dir == null) return;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, "error-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                var request = filterContext.HttpContext.Request;
                var user = filterContext.HttpContext.User;
                var userName = user != null && user.Identity != null && user.Identity.IsAuthenticated
                    ? user.Identity.Name
                    : "(chua dang nhap)";

                var entry =
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] "
                    + request.HttpMethod + " " + request.Url + Environment.NewLine
                    + "Nguoi dung: " + userName + Environment.NewLine
                    + filterContext.Exception + Environment.NewLine
                    + new string('-', 80) + Environment.NewLine;

                File.AppendAllText(path, entry);
            }
            catch
            {
                // Ghi log thất bại thì bỏ qua — không được để việc ghi log làm hỏng luôn trang lỗi.
            }
        }
    }
}
