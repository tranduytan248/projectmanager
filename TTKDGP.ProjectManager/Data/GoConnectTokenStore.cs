using System;
using System.Data;
using System.Data.SqlClient;
using TTKDGP.ProjectManager.Infrastructure;

namespace TTKDGP.ProjectManager.Data
{
    /// <summary>
    /// Lưu cặp token GoConnect (lấy được sau khi đăng nhập bằng /hrm) vào SQL, để bản chạy thật
    /// dùng lại mà không cần đăng nhập lại.
    ///
    /// Quy trình: máy local chạy /hrm → đăng nhập → lưu token vào bảng này. Bản live khoá /hrm,
    /// chỉ chạy /dongbo → đọc token từ đây để gọi API. Hai nơi dùng chung một CSDL nên token
    /// lưu ở local hiện ngay trên live.
    ///
    /// Bảng chỉ giữ MỘT dòng (khoá cố định "default"): mỗi lần /hrm chạy là ghi đè token mới.
    ///
    /// Lưu ý bảo mật: token lưu ở dạng thuần, cùng mức bảo vệ với file phiên trong App_Data và
    /// với toàn bộ dữ liệu nghiệp vụ khác trong CSDL (chỉ truy cập được bằng tài khoản SQL).
    /// Token GoConnect có hạn ngắn (access ~1 tháng) nên rủi ro giới hạn theo thời gian.
    /// </summary>
    public static class GoConnectTokenStore
    {
        private const string Table = "HrGoConnectTokens";
        private const string DefaultKey = "default";
        private static bool _schemaReady;
        private static readonly object Sync = new object();

        public static void EnsureTable()
        {
            if (_schemaReady) return;
            using (var conn = Db.Open()) EnsureTable(conn);
        }

        private static void EnsureTable(SqlConnection conn)
        {
            if (_schemaReady) return;

            lock (Sync)
            {
                if (_schemaReady) return;

                const string sql =
                    "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '" + Table + "') " +
                    "CREATE TABLE [" + Table + "] (" +
                    "  [Key] NVARCHAR(50) NOT NULL PRIMARY KEY," +
                    "  [AccessToken] NVARCHAR(MAX) NULL," +
                    "  [RefreshToken] NVARCHAR(MAX) NULL," +
                    "  [AccessTokenExpiry] DATETIME2 NULL," +
                    "  [WorkplaceId] NVARCHAR(64) NULL," +
                    "  [UpdatedAt] DATETIME2 NOT NULL" +
                    ")";

                using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();
                _schemaReady = true;
            }
        }

        /// <summary>Ghi đè token mới nhất. Gọi mỗi lần /hrm đăng nhập thành công.</summary>
        public static void Save(GoConnectTokens tokens)
        {
            if (tokens == null) throw new ArgumentNullException("tokens");
            if (!tokens.HasAccessToken)
                throw new ArgumentException("Không có access token để lưu.", "tokens");

            const string sql =
                "MERGE [" + Table + "] AS t " +
                "USING (SELECT @Key AS [Key]) AS s ON t.[Key] = s.[Key] " +
                "WHEN MATCHED THEN UPDATE SET " +
                "  [AccessToken]=@AccessToken,[RefreshToken]=@RefreshToken," +
                "  [AccessTokenExpiry]=@AccessTokenExpiry,[WorkplaceId]=@WorkplaceId,[UpdatedAt]=@UpdatedAt " +
                "WHEN NOT MATCHED THEN INSERT " +
                "  ([Key],[AccessToken],[RefreshToken],[AccessTokenExpiry],[WorkplaceId],[UpdatedAt]) " +
                "  VALUES (@Key,@AccessToken,@RefreshToken,@AccessTokenExpiry,@WorkplaceId,@UpdatedAt);";

            using (var conn = Db.Open())
            {
                EnsureTable(conn);

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Key", DefaultKey);
                    cmd.Parameters.AddWithValue("@AccessToken", (object)tokens.AccessToken ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RefreshToken", (object)tokens.RefreshToken ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AccessTokenExpiry",
                        tokens.AccessTokenExpiry.HasValue ? (object)tokens.AccessTokenExpiry.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@WorkplaceId", (object)tokens.OwnWorkplaceId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Đọc token đã lưu. Trả null nếu chưa có (chưa từng chạy /hrm).</summary>
        public static GoConnectTokens Read()
        {
            using (var conn = Db.Open())
            {
                EnsureTable(conn);

                using (var cmd = new SqlCommand(
                    "SELECT * FROM [" + Table + "] WHERE [Key] = @Key", conn))
                {
                    cmd.Parameters.AddWithValue("@Key", DefaultKey);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return null;

                        return new GoConnectTokens
                        {
                            AccessToken = Str(reader, "AccessToken"),
                            RefreshToken = Str(reader, "RefreshToken"),
                            AccessTokenExpiry = reader["AccessTokenExpiry"] is DateTime
                                ? (DateTime?)reader["AccessTokenExpiry"] : null,
                            OwnWorkplaceId = Str(reader, "WorkplaceId")
                        };
                    }
                }
            }
        }

        /// <summary>Thời điểm token được cập nhật gần nhất, null nếu chưa có.</summary>
        public static DateTime? LastUpdatedAt()
        {
            using (var conn = Db.Open())
            {
                EnsureTable(conn);

                using (var cmd = new SqlCommand(
                    "SELECT [UpdatedAt] FROM [" + Table + "] WHERE [Key] = @Key", conn))
                {
                    cmd.Parameters.AddWithValue("@Key", DefaultKey);
                    var value = cmd.ExecuteScalar();
                    return value is DateTime ? (DateTime?)value : null;
                }
            }
        }

        private static string Str(IDataRecord row, string column)
        {
            var value = row[column];
            return value is DBNull ? null : Convert.ToString(value);
        }
    }
}
