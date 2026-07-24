using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Data
{
    /// <summary>
    /// Máy ghi hàng loạt dùng chung cho ba bảng nhân sự. Mở MỘT kết nối, dùng lại một lệnh đã
    /// tham số hoá cho mọi dòng, và chốt giao dịch theo từng mẻ nhỏ — đường tới máy chủ SQL đôi
    /// lúc chập chờn nên không gom cả nghìn dòng vào một giao dịch dài; các lệnh đều là
    /// thêm-mới-hoặc-cập-nhật nên chạy lại lần sau là bù được phần dở.
    /// </summary>
    internal static class HrBulk
    {
        public const int ChunkSize = 500;

        public static int Upsert<T>(
            IEnumerable<T> rows,
            string mergeSql,
            Action<SqlCommand> declareParams,
            Action<SqlCommand, T> bindParams,
            Action<SqlConnection> ensureTable,
            Action<string> progress,
            string label)
        {
            var list = rows as IList<T> ?? rows.ToList();
            if (list.Count == 0) return 0;

            var saved = 0;

            using (var conn = Db.Open())
            {
                // Tạo bảng TRƯỚC khi mở giao dịch: lệnh DDL dùng chung kết nối này mà không gắn
                // giao dịch sẽ bị ADO.NET từ chối nếu giao dịch đang mở.
                ensureTable(conn);

                for (var offset = 0; offset < list.Count; offset += ChunkSize)
                {
                    var chunk = list.Skip(offset).Take(ChunkSize).ToList();

                    using (var tran = conn.BeginTransaction())
                    using (var cmd = new SqlCommand(mergeSql, conn, tran))
                    {
                        declareParams(cmd);

                        foreach (var row in chunk)
                        {
                            bindParams(cmd, row);
                            cmd.ExecuteNonQuery();
                            saved++;
                        }

                        tran.Commit();
                    }

                    if (progress != null)
                        progress(string.Format("Đã ghi {0}/{1} {2}.", saved, list.Count, label));
                }
            }

            return saved;
        }

        /// <summary>Cắt cho vừa cột và đổi rỗng thành NULL, để một giá trị dài bất thường
        /// không làm hỏng cả mẻ bằng lỗi tràn cột.</summary>
        public static object Fit(string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            value = value.Trim();
            return value.Length <= max ? value : value.Substring(0, max);
        }

        public static object Num(int? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        public static string Str(IDataRecord row, string column)
        {
            var value = row[column];
            return value is DBNull ? null : Convert.ToString(value);
        }

        /// <summary>
        /// Chuẩn bị từ khoá cho mệnh đề LIKE. Các ký tự %, _, [ mang nghĩa đặc biệt trong LIKE
        /// nên phải rào lại, nếu không người dùng gõ "100%" sẽ ra kết quả vô nghĩa.
        /// Đi kèm ESCAPE '\' trong câu lệnh.
        /// </summary>
        public static string Like(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return null;

            var escaped = keyword.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_")
                .Replace("[", "\\[");

            return "%" + escaped + "%";
        }

        /// <summary>Đếm tổng số dòng khớp điều kiện, để biết có bao nhiêu trang.</summary>
        public static int CountBy(SqlConnection conn, string sql, string like)
        {
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Like", (object)like ?? DBNull.Value);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>Gắn tham số chung cho các truy vấn lấy một trang.</summary>
        public static void AddPageParams(SqlCommand cmd, string like, int page, int pageSize)
        {
            cmd.Parameters.AddWithValue("@Like", (object)like ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Skip", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@Take", pageSize);
        }

        public static int? Int(IDataRecord row, string column)
        {
            var value = row[column];
            return value is DBNull ? (int?)null : Convert.ToInt32(value);
        }
    }

    /// <summary>
    /// Bảng đơn vị (phòng/ban/trung tâm) đồng bộ từ GoConnect. Khoá là WpId của GoConnect.
    /// Cây nhiều cấp nối nhau qua WpParent — muốn dựng lại đường dẫn đầy đủ của một người thì
    /// đi ngược lên theo WpParent từ đơn vị của họ.
    /// </summary>
    public static class HrWorkplaceStore
    {
        private const string Table = "HrWorkplaces";
        private static bool _schemaReady;
        private static readonly object Sync = new object();

        private const int IdLen = 64;
        private const int CodeLen = 64;
        private const int NameLen = 255;

        private const string MergeSql =
            "MERGE [" + Table + "] AS t " +
            "USING (SELECT @WpId AS WpId) AS s ON t.[WpId] = s.WpId " +
            "WHEN MATCHED THEN UPDATE SET " +
            "  [WpCode]=@WpCode,[WpName]=@WpName,[WpParent]=@WpParent,[WpOrder]=@WpOrder," +
            "  [Level]=@Level,[UpdatedAt]=@UpdatedAt " +
            "WHEN NOT MATCHED THEN INSERT " +
            "  ([WpId],[WpCode],[WpName],[WpParent],[WpOrder],[Level],[UpdatedAt]) " +
            "  VALUES (@WpId,@WpCode,@WpName,@WpParent,@WpOrder,@Level,@UpdatedAt);";

        public static void EnsureTable()
        {
            if (_schemaReady) return;
            using (var conn = Db.Open()) EnsureTable(conn);
        }

        internal static void EnsureTable(SqlConnection conn)
        {
            if (_schemaReady) return;

            lock (Sync)
            {
                if (_schemaReady) return;

                const string sql =
                    "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '" + Table + "') " +
                    "CREATE TABLE [" + Table + "] (" +
                    "  [WpId] NVARCHAR(64) NOT NULL PRIMARY KEY," +
                    "  [WpCode] NVARCHAR(64) NULL," +
                    "  [WpName] NVARCHAR(255) NULL," +
                    "  [WpParent] NVARCHAR(64) NULL," +
                    "  [WpOrder] INT NULL," +
                    "  [Level] INT NULL," +
                    "  [UpdatedAt] DATETIME2 NOT NULL" +
                    ")";

                using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();
                _schemaReady = true;
            }
        }

        /// <summary>Ghi cả mẻ đơn vị, trùng WpId thì cập nhật. Trả về số dòng đã ghi.</summary>
        public static int SaveMany(IEnumerable<HrWorkplace> workplaces, Action<string> progress = null)
        {
            if (workplaces == null) throw new ArgumentNullException("workplaces");

            var now = DateTime.Now;
            var list = workplaces
                .Where(w => w != null && !string.IsNullOrWhiteSpace(w.WpId))
                .GroupBy(w => w.WpId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            foreach (var w in list) w.UpdatedAt = now;

            return HrBulk.Upsert(list, MergeSql, DeclareParams, BindParams, EnsureTable, progress, "đơn vị");
        }

        public static List<HrWorkplace> All()
        {
            var result = new List<HrWorkplace>();

            using (var conn = Db.Open())
            {
                EnsureTable(conn);

                using (var cmd = new SqlCommand("SELECT * FROM [" + Table + "] ORDER BY [Level] DESC, [WpCode]", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) result.Add(ReadRow(reader));
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy một trang đơn vị, kèm số nhân sự đang thuộc từng đơn vị. Lọc theo từ khoá trên
        /// tên và mã đơn vị. Sắp xếp từ cấp cao xuống thấp rồi theo mã, để cây tổ chức đọc
        /// thuận mắt.
        /// </summary>
        public static PagedList<HrWorkplace> Page(int page, int pageSize, string keyword)
        {
            var like = HrBulk.Like(keyword);
            const string where =
                " WHERE (@Like IS NULL OR w.[WpName] LIKE @Like ESCAPE '\\' OR w.[WpCode] LIKE @Like ESCAPE '\\')";

            using (var conn = Db.Open())
            {
                EnsureTable(conn);
                HrEmployeeStore.EnsureTableFor(conn);

                var total = HrBulk.CountBy(conn,
                    "SELECT COUNT(*) FROM [" + Table + "] w" + where, like);

                page = PagedList<HrWorkplace>.Clamp(page,
                    Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));

                var sql =
                    "SELECT w.*, (SELECT COUNT(*) FROM [HrEmployees] e WHERE e.[WorkplaceId] = w.[WpId]) AS EmployeeCount " +
                    "FROM [" + Table + "] w" + where +
                    " ORDER BY w.[Level] DESC, w.[WpCode], w.[WpName]" +
                    " OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var items = new List<HrWorkplace>();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    HrBulk.AddPageParams(cmd, like, page, pageSize);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var w = ReadRow(reader);
                            w.EmployeeCount = Convert.ToInt32(reader["EmployeeCount"]);
                            items.Add(w);
                        }
                    }
                }

                return PagedList<HrWorkplace>.FromSlice(items, page, pageSize, total);
            }
        }

        private static HrWorkplace ReadRow(IDataRecord row)
        {
            return new HrWorkplace
            {
                WpId = HrBulk.Str(row, "WpId"),
                WpCode = HrBulk.Str(row, "WpCode"),
                WpName = HrBulk.Str(row, "WpName"),
                WpParent = HrBulk.Str(row, "WpParent"),
                WpOrder = HrBulk.Int(row, "WpOrder"),
                Level = HrBulk.Int(row, "Level"),
                UpdatedAt = row["UpdatedAt"] is DateTime ? (DateTime)row["UpdatedAt"] : DateTime.MinValue
            };
        }

        private static void DeclareParams(SqlCommand cmd)
        {
            cmd.Parameters.Add("@WpId", SqlDbType.NVarChar, IdLen);
            cmd.Parameters.Add("@WpCode", SqlDbType.NVarChar, CodeLen);
            cmd.Parameters.Add("@WpName", SqlDbType.NVarChar, NameLen);
            cmd.Parameters.Add("@WpParent", SqlDbType.NVarChar, IdLen);
            cmd.Parameters.Add("@WpOrder", SqlDbType.Int);
            cmd.Parameters.Add("@Level", SqlDbType.Int);
            cmd.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2);
        }

        private static void BindParams(SqlCommand cmd, HrWorkplace w)
        {
            cmd.Parameters["@WpId"].Value = HrBulk.Fit(w.WpId, IdLen);
            cmd.Parameters["@WpCode"].Value = HrBulk.Fit(w.WpCode, CodeLen);
            cmd.Parameters["@WpName"].Value = HrBulk.Fit(w.WpName, NameLen);
            cmd.Parameters["@WpParent"].Value = HrBulk.Fit(w.WpParent, IdLen);
            cmd.Parameters["@WpOrder"].Value = HrBulk.Num(w.WpOrder);
            cmd.Parameters["@Level"].Value = HrBulk.Num(w.Level);
            cmd.Parameters["@UpdatedAt"].Value = w.UpdatedAt;
        }
    }

    /// <summary>Bảng chức danh đồng bộ từ GoConnect. Khoá là PosId của GoConnect.</summary>
    public static class HrPositionStore
    {
        private const string Table = "HrPositions";
        private static bool _schemaReady;
        private static readonly object Sync = new object();

        private const int IdLen = 64;
        private const int CodeLen = 64;
        private const int NameLen = 255;

        private const string MergeSql =
            "MERGE [" + Table + "] AS t " +
            "USING (SELECT @PosId AS PosId) AS s ON t.[PosId] = s.PosId " +
            "WHEN MATCHED THEN UPDATE SET " +
            "  [PosCode]=@PosCode,[PosName]=@PosName,[PosOrder]=@PosOrder,[UpdatedAt]=@UpdatedAt " +
            "WHEN NOT MATCHED THEN INSERT " +
            "  ([PosId],[PosCode],[PosName],[PosOrder],[UpdatedAt]) " +
            "  VALUES (@PosId,@PosCode,@PosName,@PosOrder,@UpdatedAt);";

        public static void EnsureTable()
        {
            if (_schemaReady) return;
            using (var conn = Db.Open()) EnsureTable(conn);
        }

        internal static void EnsureTable(SqlConnection conn)
        {
            if (_schemaReady) return;

            lock (Sync)
            {
                if (_schemaReady) return;

                const string sql =
                    "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '" + Table + "') " +
                    "CREATE TABLE [" + Table + "] (" +
                    "  [PosId] NVARCHAR(64) NOT NULL PRIMARY KEY," +
                    "  [PosCode] NVARCHAR(64) NULL," +
                    "  [PosName] NVARCHAR(255) NULL," +
                    "  [PosOrder] INT NULL," +
                    "  [UpdatedAt] DATETIME2 NOT NULL" +
                    ")";

                using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();
                _schemaReady = true;
            }
        }

        /// <summary>Ghi cả mẻ chức danh, trùng PosId thì cập nhật. Trả về số dòng đã ghi.</summary>
        public static int SaveMany(IEnumerable<HrPosition> positions, Action<string> progress = null)
        {
            if (positions == null) throw new ArgumentNullException("positions");

            var now = DateTime.Now;
            var list = positions
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.PosId))
                .GroupBy(p => p.PosId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            foreach (var p in list) p.UpdatedAt = now;

            return HrBulk.Upsert(list, MergeSql, DeclareParams, BindParams, EnsureTable, progress, "chức danh");
        }

        public static List<HrPosition> All()
        {
            var result = new List<HrPosition>();

            using (var conn = Db.Open())
            {
                EnsureTable(conn);

                using (var cmd = new SqlCommand("SELECT * FROM [" + Table + "] ORDER BY [PosName]", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) result.Add(ReadRow(reader));
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy một trang chức danh, kèm số nhân sự đang giữ từng chức danh. Lọc theo từ khoá
        /// trên tên và mã chức danh.
        /// </summary>
        public static PagedList<HrPosition> Page(int page, int pageSize, string keyword)
        {
            var like = HrBulk.Like(keyword);
            const string where =
                " WHERE (@Like IS NULL OR p.[PosName] LIKE @Like ESCAPE '\\' OR p.[PosCode] LIKE @Like ESCAPE '\\')";

            using (var conn = Db.Open())
            {
                EnsureTable(conn);
                HrEmployeeStore.EnsureTableFor(conn);

                var total = HrBulk.CountBy(conn,
                    "SELECT COUNT(*) FROM [" + Table + "] p" + where, like);

                page = PagedList<HrPosition>.Clamp(page,
                    Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));

                var sql =
                    "SELECT p.*, (SELECT COUNT(*) FROM [HrEmployees] e WHERE e.[PositionId] = p.[PosId]) AS EmployeeCount " +
                    "FROM [" + Table + "] p" + where +
                    " ORDER BY p.[PosName]" +
                    " OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var items = new List<HrPosition>();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    HrBulk.AddPageParams(cmd, like, page, pageSize);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var p = ReadRow(reader);
                            p.EmployeeCount = Convert.ToInt32(reader["EmployeeCount"]);
                            items.Add(p);
                        }
                    }
                }

                return PagedList<HrPosition>.FromSlice(items, page, pageSize, total);
            }
        }

        private static HrPosition ReadRow(IDataRecord row)
        {
            return new HrPosition
            {
                PosId = HrBulk.Str(row, "PosId"),
                PosCode = HrBulk.Str(row, "PosCode"),
                PosName = HrBulk.Str(row, "PosName"),
                PosOrder = HrBulk.Int(row, "PosOrder"),
                UpdatedAt = row["UpdatedAt"] is DateTime ? (DateTime)row["UpdatedAt"] : DateTime.MinValue
            };
        }

        private static void DeclareParams(SqlCommand cmd)
        {
            cmd.Parameters.Add("@PosId", SqlDbType.NVarChar, IdLen);
            cmd.Parameters.Add("@PosCode", SqlDbType.NVarChar, CodeLen);
            cmd.Parameters.Add("@PosName", SqlDbType.NVarChar, NameLen);
            cmd.Parameters.Add("@PosOrder", SqlDbType.Int);
            cmd.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2);
        }

        private static void BindParams(SqlCommand cmd, HrPosition p)
        {
            cmd.Parameters["@PosId"].Value = HrBulk.Fit(p.PosId, IdLen);
            cmd.Parameters["@PosCode"].Value = HrBulk.Fit(p.PosCode, CodeLen);
            cmd.Parameters["@PosName"].Value = HrBulk.Fit(p.PosName, NameLen);
            cmd.Parameters["@PosOrder"].Value = HrBulk.Num(p.PosOrder);
            cmd.Parameters["@UpdatedAt"].Value = p.UpdatedAt;
        }
    }
}
