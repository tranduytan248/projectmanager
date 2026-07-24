using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Đầu mối mở kết nối tới SQL Server. Thông số máy chủ/CSDL/tài khoản lấy từ
    /// <see cref="AppSettings.Db"/>; mật khẩu nằm trong App_Config\secrets.config.
    /// </summary>
    public static class Db
    {
        // Đường tới máy chủ SQL đôi lúc chập chờn: có thời điểm bắt tay TCP không kịp trong hạn.
        // Thử lại vài lần với khoảng nghỉ ngắn để những cú rớt thoáng qua không thành trang lỗi.
        private const int MaxAttempts = 4;
        private const int RetryBaseDelayMs = 400;

        /// <summary>Chuỗi kết nối dựng từ cấu hình. Dựng mỗi lần gọi để luôn phản ánh cấu hình mới nhất.</summary>
        public static string ConnectionString
        {
            get
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = AppSettings.Db.Server,
                    InitialCatalog = AppSettings.Db.Database,
                    // SQL Server 2012 thường dùng chứng chỉ tự ký, không xác thực CA được.
                    TrustServerCertificate = true,
                    ConnectTimeout = 10,
                    // Toàn hệ thống dùng tiếng Việt, bật MultipleActiveResultSets cho chắc khi đọc lồng nhau.
                    MultipleActiveResultSets = true
                };

                if (AppSettings.Db.IntegratedSecurity)
                {
                    builder.IntegratedSecurity = true;
                }
                else
                {
                    builder.UserID = AppSettings.Db.User;
                    builder.Password = AppSettings.Db.Password;
                }

                return builder.ConnectionString;
            }
        }

        /// <summary>
        /// Mở một kết nối mới đã sẵn sàng dùng, có thử lại khi máy chủ tạm thời không tới được.
        /// Người gọi chịu trách nhiệm Dispose.
        /// </summary>
        public static SqlConnection Open()
        {
            SqlException lastError = null;

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                var connection = new SqlConnection(ConnectionString);
                try
                {
                    connection.Open();
                    return connection;
                }
                catch (SqlException ex)
                {
                    connection.Dispose();
                    lastError = ex;

                    if (attempt < MaxAttempts)
                    {
                        Debug.WriteLine(string.Format(
                            "Mở kết nối SQL trượt (lần {0}/{1}): {2}", attempt, MaxAttempts, ex.Message));
                        Thread.Sleep(RetryBaseDelayMs * attempt);
                    }
                }
            }

            throw new Exception(
                string.Format("Không mở được kết nối SQL sau {0} lần thử (máy chủ đang chập chờn?).", MaxAttempts),
                lastError);
        }
    }
}
