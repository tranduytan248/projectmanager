using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TTKDGP.ProjectManager.Infrastructure;

namespace TTKDGP.ProjectManager.Data
{
    /// <summary>
    /// Chuyển dữ liệu bộ quản lý công việc từ khoá NHÂN SỰ (bảng WorkStaff cũ) sang khoá
    /// NGƯỜI DÙNG (bảng Users): điền UserId/UserFullName cho phân công, đầu việc, dự án,
    /// trao đổi, phiếu KPI và báo cáo tuần dựa trên các cột StaffId cũ còn nằm trong SQL.
    ///
    /// Phải chạy TRƯỚC khi bất kỳ SqlStore nào nạp dữ liệu — store nạp xong mới sửa SQL thì
    /// bản trong bộ nhớ vẫn là số 0 cũ cho tới lần khởi động sau. Vì vậy toàn bộ ở đây dùng
    /// ADO thẳng, không đụng tới Repository.
    ///
    /// Cách nối: WorkStaff.UserId nếu đã gắn; chưa gắn thì dò tài khoản theo email rồi tới
    /// họ tên (chỉ nhận khi tên là duy nhất). Không nối được thì vẫn chép TÊN hiển thị sang
    /// cột mới để danh sách không trống, còn quyền hạn thì chờ phân công lại theo tài khoản.
    ///
    /// Idempotent: chỉ đụng vào dòng có cột mới còn trống, chạy lại nhiều lần không sao.
    /// </summary>
    public static class WorkUserMigration
    {
        public static void RunIfNeeded()
        {
            try
            {
                using (var connection = Db.Open())
                {
                    var map = BuildStaffMap(connection);

                    // Bảng chưa tồn tại (bản cài mới tinh) hoặc chưa từng có cột Staff cũ thì
                    // từng bước bên dưới tự bỏ qua — không có gì để chuyển.
                    MigrateIds(connection, "WorkAssignments", "StaffId", "UserId", map);
                    MigrateNames(connection, "WorkAssignments", "StaffId", "StaffName", "UserFullName", map);

                    MigrateIds(connection, "WorkTasks", "AssigneeStaffId", "AssigneeUserId", map);
                    MigrateIds(connection, "WorkTasks", "AssignedByStaffId", "AssignedByUserId", map);

                    MigrateIds(connection, "WorkProjects", "PmStaffId", "PmUserId", map);

                    MigrateIds(connection, "WorkComments", "StaffId", "UserId", map);
                    MigrateNames(connection, "WorkComments", "StaffId", "StaffName", "AuthorName", map);

                    MigrateIds(connection, "KpiSheets", "StaffId", "UserId", map);
                    MigrateNames(connection, "KpiSheets", "StaffId", "StaffName", "UserFullName", map);
                    MigrateIds(connection, "KpiSheets", "PmApprovedByStaffId", "PmApprovedByUserId", map);
                    MigrateIds(connection, "KpiSheets", "ManagerApprovedByStaffId", "ManagerApprovedByUserId", map);

                    MigrateIds(connection, "WorkWeekReports", "SubmittedByStaffId", "SubmittedByUserId", map);
                }
            }
            catch (Exception)
            {
                // Không để lỗi hạ tầng (SQL chập chờn) làm sập lúc khởi động; lần chạy sau chuyển tiếp.
            }
        }

        /// <summary>Id nhân sự cũ -> tài khoản tương ứng (0 nếu không nối được) kèm tên hiển thị.</summary>
        private class StaffRef
        {
            public int UserId;
            public string FullName;
        }

        private static Dictionary<int, StaffRef> BuildStaffMap(SqlConnection connection)
        {
            var map = new Dictionary<int, StaffRef>();

            if (!ColumnExists(connection, "WorkStaff", "Id")) return map;

            // Tài khoản tra theo email và theo họ tên. Tên chỉ dùng khi DUY NHẤT — hai người
            // trùng tên mà đoán bừa thì việc của người này gán sang người kia.
            var usersByEmail = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var usersByName = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
            var dupNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var userIds = new HashSet<int>();

            using (var cmd = new SqlCommand("SELECT [Id], [FullName], [Email] FROM [Users]", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var name = reader.IsDBNull(1) ? null : reader.GetString(1);
                    var email = reader.IsDBNull(2) ? null : reader.GetString(2);

                    userIds.Add(id);

                    if (!string.IsNullOrWhiteSpace(email)) usersByEmail[email.Trim()] = id;

                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var key = name.Trim();
                    if (dupNames.Contains(key)) continue;
                    if (usersByName.ContainsKey(key))
                    {
                        usersByName.Remove(key);
                        dupNames.Add(key);
                    }
                    else
                    {
                        usersByName[key] = id;
                    }
                }
            }

            var hasUserIdCol = ColumnExists(connection, "WorkStaff", "UserId");
            var sql = hasUserIdCol
                ? "SELECT [Id], [FullName], [Email], [UserId] FROM [WorkStaff]"
                : "SELECT [Id], [FullName], [Email], 0 FROM [WorkStaff]";

            using (var cmd = new SqlCommand(sql, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var staffId = reader.GetInt32(0);
                    var name = reader.IsDBNull(1) ? null : reader.GetString(1);
                    var email = reader.IsDBNull(2) ? null : reader.GetString(2);
                    var linked = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3));

                    var userId = 0;
                    if (linked > 0 && userIds.Contains(linked)) userId = linked;

                    if (userId == 0 && !string.IsNullOrWhiteSpace(email))
                    {
                        usersByEmail.TryGetValue(email.Trim(), out userId);
                    }

                    if (userId == 0 && !string.IsNullOrWhiteSpace(name))
                    {
                        usersByName.TryGetValue(name.Trim(), out userId);
                    }

                    map[staffId] = new StaffRef { UserId = userId, FullName = name };
                }
            }

            return map;
        }

        /// <summary>
        /// Điền cột Id mới từ cột Id nhân sự cũ cho những dòng cột mới còn 0/NULL.
        /// Bảng hoặc cột cũ không tồn tại thì bỏ qua êm.
        /// </summary>
        private static void MigrateIds(SqlConnection connection, string table,
            string oldCol, string newCol, Dictionary<int, StaffRef> map)
        {
            if (map.Count == 0) return;
            if (!ColumnExists(connection, table, oldCol)) return;

            EnsureColumn(connection, table, newCol, "INT");

            foreach (var pair in map)
            {
                if (pair.Value.UserId <= 0) continue;

                var sql = string.Format(
                    "UPDATE [{0}] SET [{1}] = @userId WHERE ISNULL([{1}], 0) = 0 AND [{2}] = @staffId",
                    table, newCol, oldCol);

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", pair.Value.UserId);
                    cmd.Parameters.AddWithValue("@staffId", pair.Key);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Chép tên hiển thị từ cột tên cũ sang cột tên mới cho những dòng còn trống tên —
        /// kể cả khi không nối được tài khoản, để danh sách không hiện dòng trắng.
        /// </summary>
        private static void MigrateNames(SqlConnection connection, string table,
            string oldIdCol, string oldNameCol, string newNameCol, Dictionary<int, StaffRef> map)
        {
            if (!ColumnExists(connection, table, oldNameCol)) return;

            EnsureColumn(connection, table, newNameCol, "NVARCHAR(MAX)");

            // Ưu tiên tên trong hồ sơ nhân sự cũ (đầy đủ dấu); dòng nào không tra được thì
            // lấy luôn tên đã lưu kèm trên chính dòng đó.
            var sql = string.Format(
                "UPDATE [{0}] SET [{1}] = [{2}] WHERE ([{1}] IS NULL OR LTRIM(RTRIM([{1}])) = '') "
                + "AND [{2}] IS NOT NULL AND LTRIM(RTRIM([{2}])) <> ''",
                table, newNameCol, oldNameCol);

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.ExecuteNonQuery();
            }

            if (string.IsNullOrEmpty(oldIdCol) || !ColumnExists(connection, table, oldIdCol)) return;

            foreach (var pair in map)
            {
                if (string.IsNullOrWhiteSpace(pair.Value.FullName)) continue;

                var fix = string.Format(
                    "UPDATE [{0}] SET [{1}] = @name WHERE ([{1}] IS NULL OR LTRIM(RTRIM([{1}])) = '') "
                    + "AND [{2}] = @staffId",
                    table, newNameCol, oldIdCol);

                using (var cmd = new SqlCommand(fix, connection))
                {
                    cmd.Parameters.AddWithValue("@name", pair.Value.FullName);
                    cmd.Parameters.AddWithValue("@staffId", pair.Key);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool ColumnExists(SqlConnection connection, string table, string column)
        {
            using (var cmd = new SqlCommand("SELECT COL_LENGTH(@t, @c)", connection))
            {
                cmd.Parameters.AddWithValue("@t", table);
                cmd.Parameters.AddWithValue("@c", column);
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
        }

        /// <summary>
        /// Thêm cột mới nếu chưa có. Bình thường SqlStore tự thêm lúc nạp bảng, nhưng bước chuyển
        /// này chạy TRƯỚC khi store nào kịp nạp nên phải tự lo.
        /// </summary>
        private static void EnsureColumn(SqlConnection connection, string table, string column, string type)
        {
            var sql = string.Format(
                "IF COL_LENGTH(@t, @c) IS NULL EXEC('ALTER TABLE [' + @t + '] ADD [' + @c + '] {0} NULL')",
                type);

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@t", table);
                cmd.Parameters.AddWithValue("@c", column);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
