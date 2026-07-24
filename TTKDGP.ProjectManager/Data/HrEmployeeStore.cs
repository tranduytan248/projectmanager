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
    /// Lưu thông tin nhân sự (đồng bộ từ GoConnect) vào SQL Server. Tự tạo bảng nếu chưa có.
    ///
    /// Ghi theo kiểu thêm-mới-hoặc-cập-nhật theo <see cref="HrEmployee.Email"/>: người đã có
    /// thì cập nhật lại thông tin (đổi phòng ban, đổi chức danh, đổi số điện thoại), người
    /// chưa có thì thêm mới. Đơn vị và chức danh chỉ lưu định danh, tên nằm ở
    /// <see cref="HrWorkplaceStore"/> và <see cref="HrPositionStore"/>.
    /// </summary>
    public static class HrEmployeeStore
    {
        private const string Table = "HrEmployees";
        private static bool _schemaReady;
        private static readonly object Sync = new object();

        // Độ dài cột, khớp với lệnh tạo bảng bên dưới. Đo trên dữ liệu thật (780 người) rồi
        // nhân đôi cho thoải mái: email 27, họ tên 28, mã 10, điện thoại 10, các định danh 36.
        private const int EmailLen = 255;
        private const int CodeLen = 50;
        private const int NameLen = 255;
        private const int PhoneLen = 30;
        private const int GenderLen = 20;
        private const int IdLen = 64;

        private const string MergeSql =
            "MERGE [" + Table + "] AS t " +
            "USING (SELECT @Email AS Email) AS s ON t.[Email] = s.Email " +
            "WHEN MATCHED THEN UPDATE SET " +
            "  [Code]=@Code,[FullName]=@FullName,[PhoneNumber]=@PhoneNumber,[Gender]=@Gender," +
            "  [WorkplaceId]=@WorkplaceId,[PositionId]=@PositionId,[UserId]=@UserId," +
            "  [AccountId]=@AccountId,[UpdatedAt]=@UpdatedAt " +
            "WHEN NOT MATCHED THEN INSERT " +
            "  ([Email],[Code],[FullName],[PhoneNumber],[Gender],[WorkplaceId],[PositionId]," +
            "   [UserId],[AccountId],[UpdatedAt]) " +
            "  VALUES (@Email,@Code,@FullName,@PhoneNumber,@Gender,@WorkplaceId,@PositionId," +
            "   @UserId,@AccountId,@UpdatedAt);";

        // Đọc kèm tên đơn vị và tên chức danh. LEFT JOIN để người thiếu đơn vị/chức danh
        // vẫn hiện ra thay vì biến mất khỏi kết quả.
        private const string SelectSql =
            "SELECT e.*, w.[WpName] AS DepartmentName, p.[PosName] AS PositionName " +
            "FROM [" + Table + "] e " +
            "LEFT JOIN [HrWorkplaces] w ON w.[WpId] = e.[WorkplaceId] " +
            "LEFT JOIN [HrPositions] p ON p.[PosId] = e.[PositionId] ";

        /// <summary>Tạo bảng nếu chưa tồn tại. An toàn khi gọi nhiều lần.</summary>
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
                    "  [Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY," +
                    "  [Email] NVARCHAR(255) NOT NULL," +
                    "  [Code] NVARCHAR(50) NULL," +
                    "  [FullName] NVARCHAR(255) NULL," +
                    "  [PhoneNumber] NVARCHAR(30) NULL," +
                    "  [Gender] NVARCHAR(20) NULL," +
                    "  [WorkplaceId] NVARCHAR(64) NULL," +
                    "  [PositionId] NVARCHAR(64) NULL," +
                    "  [UserId] NVARCHAR(64) NULL," +
                    "  [AccountId] NVARCHAR(64) NULL," +
                    "  [UpdatedAt] DATETIME2 NOT NULL," +
                    "  CONSTRAINT [UQ_" + Table + "_Email] UNIQUE ([Email])" +
                    ")";

                using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();

                // Tra cứu theo mã nhân viên vẫn hay dùng dù mã không còn là khoá.
                const string index =
                    "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_" + Table + "_Code') " +
                    "CREATE INDEX [IX_" + Table + "_Code] ON [" + Table + "] ([Code])";

                using (var cmd = new SqlCommand(index, conn)) cmd.ExecuteNonQuery();

                _schemaReady = true;
            }
        }

        /// <summary>
        /// Thêm mới hoặc cập nhật một người theo email.
        /// </summary>
        public static void Save(HrEmployee employee)
        {
            if (employee == null) throw new ArgumentNullException("employee");
            if (string.IsNullOrWhiteSpace(employee.Email))
                throw new ArgumentException("Thiếu email nên không thể lưu.", "employee");

            SaveMany(new[] { employee });
        }

        /// <summary>
        /// Ghi cả mẻ. Dòng thiếu email bị bỏ qua (không ném lỗi) để một vài bản ghi lạ không
        /// chặn cả đợt đồng bộ; số bỏ qua trả về qua <paramref name="skipped"/>.
        /// </summary>
        public static int SaveMany(IEnumerable<HrEmployee> employees, Action<string> progress = null)
        {
            int skipped;
            return SaveMany(employees, out skipped, progress);
        }

        public static int SaveMany(IEnumerable<HrEmployee> employees, out int skipped, Action<string> progress = null)
        {
            if (employees == null) throw new ArgumentNullException("employees");

            var all = employees.Where(e => e != null).ToList();

            // Gộp trùng email trong cùng một mẻ, giữ dòng đầu — tránh ghi đi ghi lại một người.
            var list = all
                .Where(e => !string.IsNullOrWhiteSpace(e.Email))
                .GroupBy(e => e.Email.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            skipped = all.Count - list.Count;
            if (list.Count == 0) return 0;

            var now = DateTime.Now;
            foreach (var e in list) e.UpdatedAt = now;

            return HrBulk.Upsert(list, MergeSql, DeclareParams, BindParams, EnsureTable, progress, "nhân sự");
        }

        /// <summary>Lấy một người theo email, null nếu chưa có.</summary>
        public static HrEmployee GetByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            using (var conn = Db.Open())
            {
                EnsureTable(conn);
                HrWorkplaceStore.EnsureTable(conn);
                HrPositionStore.EnsureTable(conn);

                using (var cmd = new SqlCommand(SelectSql + "WHERE e.[Email] = @Email", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email.Trim());
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read() ? ReadRow(reader) : null;
                    }
                }
            }
        }

        /// <summary>Lấy một người theo mã nhân viên. Mã không còn là khoá nên có thể ra nhiều
        /// dòng về lý thuyết; trả dòng cập nhật gần nhất.</summary>
        public static HrEmployee GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            using (var conn = Db.Open())
            {
                EnsureTable(conn);
                HrWorkplaceStore.EnsureTable(conn);
                HrPositionStore.EnsureTable(conn);

                using (var cmd = new SqlCommand(
                    SelectSql + "WHERE e.[Code] = @Code ORDER BY e.[UpdatedAt] DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code.Trim());
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read() ? ReadRow(reader) : null;
                    }
                }
            }
        }

        /// <summary>Cho các lớp cùng nhóm gọi nhờ tạo bảng khi truy vấn có nối sang bảng này.</summary>
        internal static void EnsureTableFor(SqlConnection conn)
        {
            EnsureTable(conn);
        }

        /// <summary>
        /// Lấy một trang nhân sự, kèm tên đơn vị và tên chức danh. Từ khoá tìm trên họ tên,
        /// email, mã nhân viên, số điện thoại, tên đơn vị và tên chức danh — người dùng gõ gì
        /// cũng ra, không phải chọn đúng ô.
        /// </summary>
        public static PagedList<HrEmployee> Page(
            int page, int pageSize, string keyword, string workplaceId = null, string positionId = null)
        {
            var like = HrBulk.Like(keyword);

            // Cùng một mệnh đề lọc cho cả đếm tổng lẫn lấy trang, để hai con số không lệch nhau.
            const string where =
                " WHERE (@Like IS NULL OR e.[FullName] LIKE @Like ESCAPE '\\' OR e.[Email] LIKE @Like ESCAPE '\\'" +
                "        OR e.[Code] LIKE @Like ESCAPE '\\' OR e.[PhoneNumber] LIKE @Like ESCAPE '\\'" +
                "        OR w.[WpName] LIKE @Like ESCAPE '\\' OR p.[PosName] LIKE @Like ESCAPE '\\')" +
                "   AND (@WorkplaceId IS NULL OR e.[WorkplaceId] = @WorkplaceId)" +
                "   AND (@PositionId IS NULL OR e.[PositionId] = @PositionId)";

            const string from =
                " FROM [" + Table + "] e" +
                " LEFT JOIN [HrWorkplaces] w ON w.[WpId] = e.[WorkplaceId]" +
                " LEFT JOIN [HrPositions] p ON p.[PosId] = e.[PositionId]";

            using (var conn = Db.Open())
            {
                EnsureTable(conn);
                HrWorkplaceStore.EnsureTable(conn);
                HrPositionStore.EnsureTable(conn);

                int total;
                using (var cmd = new SqlCommand("SELECT COUNT(*)" + from + where, conn))
                {
                    AddFilters(cmd, like, workplaceId, positionId);
                    total = Convert.ToInt32(cmd.ExecuteScalar());
                }

                page = PagedList<HrEmployee>.Clamp(page,
                    Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));

                var sql =
                    "SELECT e.*, w.[WpName] AS DepartmentName, p.[PosName] AS PositionName" + from + where +
                    " ORDER BY e.[FullName], e.[Email]" +
                    " OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var items = new List<HrEmployee>();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    AddFilters(cmd, like, workplaceId, positionId);
                    cmd.Parameters.AddWithValue("@Skip", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@Take", pageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) items.Add(ReadRow(reader));
                    }
                }

                return PagedList<HrEmployee>.FromSlice(items, page, pageSize, total);
            }
        }

        private static void AddFilters(SqlCommand cmd, string like, string workplaceId, string positionId)
        {
            cmd.Parameters.AddWithValue("@Like", (object)like ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@WorkplaceId",
                string.IsNullOrWhiteSpace(workplaceId) ? (object)DBNull.Value : workplaceId.Trim());
            cmd.Parameters.AddWithValue("@PositionId",
                string.IsNullOrWhiteSpace(positionId) ? (object)DBNull.Value : positionId.Trim());
        }

        /// <summary>Số nhân sự đang có trong bảng.</summary>
        public static int Count()
        {
            using (var conn = Db.Open())
            {
                EnsureTable(conn);
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM [" + Table + "]", conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private static void DeclareParams(SqlCommand cmd)
        {
            cmd.Parameters.Add("@Email", SqlDbType.NVarChar, EmailLen);
            cmd.Parameters.Add("@Code", SqlDbType.NVarChar, CodeLen);
            cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, NameLen);
            cmd.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, PhoneLen);
            cmd.Parameters.Add("@Gender", SqlDbType.NVarChar, GenderLen);
            cmd.Parameters.Add("@WorkplaceId", SqlDbType.NVarChar, IdLen);
            cmd.Parameters.Add("@PositionId", SqlDbType.NVarChar, IdLen);
            cmd.Parameters.Add("@UserId", SqlDbType.NVarChar, IdLen);
            cmd.Parameters.Add("@AccountId", SqlDbType.NVarChar, IdLen);
            cmd.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2);
        }

        private static void BindParams(SqlCommand cmd, HrEmployee e)
        {
            cmd.Parameters["@Email"].Value = HrBulk.Fit(e.Email, EmailLen);
            cmd.Parameters["@Code"].Value = HrBulk.Fit(e.Code, CodeLen);
            cmd.Parameters["@FullName"].Value = HrBulk.Fit(e.FullName, NameLen);
            cmd.Parameters["@PhoneNumber"].Value = HrBulk.Fit(e.PhoneNumber, PhoneLen);
            cmd.Parameters["@Gender"].Value = HrBulk.Fit(e.Gender, GenderLen);
            cmd.Parameters["@WorkplaceId"].Value = HrBulk.Fit(e.WorkplaceId, IdLen);
            cmd.Parameters["@PositionId"].Value = HrBulk.Fit(e.PositionId, IdLen);
            cmd.Parameters["@UserId"].Value = HrBulk.Fit(e.UserId, IdLen);
            cmd.Parameters["@AccountId"].Value = HrBulk.Fit(e.AccountId, IdLen);
            cmd.Parameters["@UpdatedAt"].Value = e.UpdatedAt;
        }

        private static HrEmployee ReadRow(IDataRecord row)
        {
            return new HrEmployee
            {
                Email = HrBulk.Str(row, "Email"),
                Code = HrBulk.Str(row, "Code"),
                FullName = HrBulk.Str(row, "FullName"),
                PhoneNumber = HrBulk.Str(row, "PhoneNumber"),
                Gender = HrBulk.Str(row, "Gender"),
                WorkplaceId = HrBulk.Str(row, "WorkplaceId"),
                PositionId = HrBulk.Str(row, "PositionId"),
                UserId = HrBulk.Str(row, "UserId"),
                AccountId = HrBulk.Str(row, "AccountId"),
                DepartmentName = HrBulk.Str(row, "DepartmentName"),
                PositionName = HrBulk.Str(row, "PositionName"),
                UpdatedAt = row["UpdatedAt"] is DateTime ? (DateTime)row["UpdatedAt"] : DateTime.MinValue
            };
        }
    }
}
