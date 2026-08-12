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
    /// Bảng đơn vị của HRM (Odoo, hrm.vnpt.vn) — độc lập với HrWorkplaces (GoConnect), xem
    /// <see cref="DepartmentHrm"/>. Không có ràng buộc FK tới chính nó (self-reference) vì
    /// ghi theo mẻ có thể chèn con trước cha; tra cứu cây khi đọc.
    /// </summary>
    public static class DepartmentHrmStore
    {
        private const string Table = "department_hrm";
        private static bool _schemaReady;
        private static readonly object Sync = new object();
        private const int NameLen = 255;

        private const string MergeSql =
            "MERGE [" + Table + "] AS t " +
            "USING (SELECT @department_id AS department_id) AS s ON t.[department_id] = s.department_id " +
            "WHEN MATCHED THEN UPDATE SET " +
            "  [department_name]=@department_name,[department_parent_id]=@department_parent_id,[updated_at]=@updated_at " +
            "WHEN NOT MATCHED THEN INSERT " +
            "  ([department_id],[department_name],[department_parent_id],[updated_at]) " +
            "  VALUES (@department_id,@department_name,@department_parent_id,@updated_at);";

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
                    "  [department_id] INT NOT NULL PRIMARY KEY," +
                    "  [department_name] NVARCHAR(255) NULL," +
                    "  [department_parent_id] INT NULL," +
                    "  [updated_at] DATETIME2 NOT NULL" +
                    ")";

                using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();
                _schemaReady = true;
            }
        }

        /// <summary>Xoá sạch bảng — dùng trước khi nạp lại toàn bộ danh bạ từ HRM, để đơn vị
        /// đã giải thể/đổi tên bên nguồn không còn tồn đọng lại đây.</summary>
        public static void DeleteAll()
        {
            using (var conn = Db.Open())
            {
                EnsureTable(conn);
                using (var cmd = new SqlCommand("TRUNCATE TABLE [" + Table + "]", conn)) cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Ghi cả mẻ đơn vị, trùng department_id thì cập nhật.</summary>
        public static int SaveMany(IEnumerable<DepartmentHrm> departments, Action<string> progress = null)
        {
            if (departments == null) throw new ArgumentNullException("departments");

            var now = DateTime.Now;
            var list = departments
                .Where(d => d != null)
                .GroupBy(d => d.DepartmentId)
                .Select(g => g.First())
                .ToList();

            foreach (var d in list) d.UpdatedAt = now;

            return HrBulk.Upsert(list, MergeSql, DeclareParams, BindParams, EnsureTable, progress, "đơn vị HRM");
        }

        public static List<DepartmentHrm> All()
        {
            var result = new List<DepartmentHrm>();

            using (var conn = Db.Open())
            {
                EnsureTable(conn);

                using (var cmd = new SqlCommand("SELECT * FROM [" + Table + "] ORDER BY [department_id]", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) result.Add(ReadRow(reader));
                }
            }

            return result;
        }

        private static DepartmentHrm ReadRow(IDataRecord row)
        {
            return new DepartmentHrm
            {
                DepartmentId = Convert.ToInt32(row["department_id"]),
                DepartmentName = HrBulk.Str(row, "department_name"),
                DepartmentParentId = HrBulk.Int(row, "department_parent_id"),
                UpdatedAt = row["updated_at"] is DateTime ? (DateTime)row["updated_at"] : DateTime.MinValue
            };
        }

        private static void DeclareParams(SqlCommand cmd)
        {
            cmd.Parameters.Add("@department_id", SqlDbType.Int);
            cmd.Parameters.Add("@department_name", SqlDbType.NVarChar, NameLen);
            cmd.Parameters.Add("@department_parent_id", SqlDbType.Int);
            cmd.Parameters.Add("@updated_at", SqlDbType.DateTime2);
        }

        private static void BindParams(SqlCommand cmd, DepartmentHrm d)
        {
            cmd.Parameters["@department_id"].Value = d.DepartmentId;
            cmd.Parameters["@department_name"].Value = HrBulk.Fit(d.DepartmentName, NameLen);
            cmd.Parameters["@department_parent_id"].Value = HrBulk.Num(d.DepartmentParentId);
            cmd.Parameters["@updated_at"].Value = d.UpdatedAt;
        }
    }

    /// <summary>Bảng chức danh (hr.job) của HRM (Odoo) — độc lập với HrPositions (GoConnect).</summary>
    public static class JobHrmStore
    {
        private const string Table = "job_hrm";
        private static bool _schemaReady;
        private static readonly object Sync = new object();
        private const int CodeLen = 50;
        private const int NameLen = 255;

        private const string MergeSql =
            "MERGE [" + Table + "] AS t " +
            "USING (SELECT @job_id AS job_id) AS s ON t.[job_id] = s.job_id " +
            "WHEN MATCHED THEN UPDATE SET " +
            "  [job_code]=@job_code,[job_name]=@job_name,[updated_at]=@updated_at " +
            "WHEN NOT MATCHED THEN INSERT " +
            "  ([job_id],[job_code],[job_name],[updated_at]) " +
            "  VALUES (@job_id,@job_code,@job_name,@updated_at);";

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
                    "  [job_id] INT NOT NULL PRIMARY KEY," +
                    "  [job_code] NVARCHAR(50) NULL," +
                    "  [job_name] NVARCHAR(255) NULL," +
                    "  [updated_at] DATETIME2 NOT NULL" +
                    ")";

                using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();
                _schemaReady = true;
            }
        }

        /// <summary>Xoá sạch bảng — dùng trước khi nạp lại toàn bộ danh bạ từ HRM.</summary>
        public static void DeleteAll()
        {
            using (var conn = Db.Open())
            {
                EnsureTable(conn);
                using (var cmd = new SqlCommand("TRUNCATE TABLE [" + Table + "]", conn)) cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Ghi cả mẻ chức danh, trùng job_id thì cập nhật.</summary>
        public static int SaveMany(IEnumerable<JobHrm> jobs, Action<string> progress = null)
        {
            if (jobs == null) throw new ArgumentNullException("jobs");

            var now = DateTime.Now;
            var list = jobs
                .Where(j => j != null)
                .GroupBy(j => j.JobId)
                .Select(g => g.First())
                .ToList();

            foreach (var j in list) j.UpdatedAt = now;

            return HrBulk.Upsert(list, MergeSql, DeclareParams, BindParams, EnsureTable, progress, "chức danh HRM");
        }

        public static List<JobHrm> All()
        {
            var result = new List<JobHrm>();

            using (var conn = Db.Open())
            {
                EnsureTable(conn);

                using (var cmd = new SqlCommand("SELECT * FROM [" + Table + "] ORDER BY [job_id]", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) result.Add(ReadRow(reader));
                }
            }

            return result;
        }

        private static JobHrm ReadRow(IDataRecord row)
        {
            return new JobHrm
            {
                JobId = Convert.ToInt32(row["job_id"]),
                JobCode = HrBulk.Str(row, "job_code"),
                JobName = HrBulk.Str(row, "job_name"),
                UpdatedAt = row["updated_at"] is DateTime ? (DateTime)row["updated_at"] : DateTime.MinValue
            };
        }

        private static void DeclareParams(SqlCommand cmd)
        {
            cmd.Parameters.Add("@job_id", SqlDbType.Int);
            cmd.Parameters.Add("@job_code", SqlDbType.NVarChar, CodeLen);
            cmd.Parameters.Add("@job_name", SqlDbType.NVarChar, NameLen);
            cmd.Parameters.Add("@updated_at", SqlDbType.DateTime2);
        }

        private static void BindParams(SqlCommand cmd, JobHrm j)
        {
            cmd.Parameters["@job_id"].Value = j.JobId;
            cmd.Parameters["@job_code"].Value = HrBulk.Fit(j.JobCode, CodeLen);
            cmd.Parameters["@job_name"].Value = HrBulk.Fit(j.JobName, NameLen);
            cmd.Parameters["@updated_at"].Value = j.UpdatedAt;
        }
    }

    /// <summary>Bảng nhân sự của HRM (Odoo, model vnpt.hr.danhba.view) — độc lập với
    /// HrEmployees (GoConnect).</summary>
    public static class EmployeeHrmStore
    {
        private const string Table = "employee_hrm";
        private static bool _schemaReady;
        private static readonly object Sync = new object();

        private const int CodeLen = 50;
        private const int NameLen = 255;
        private const int PhoneLen = 30;
        private const int EmailLen = 255;
        private const int GenderLen = 20;

        private const string MergeSql =
            "MERGE [" + Table + "] AS t " +
            "USING (SELECT @employee_id AS employee_id) AS s ON t.[employee_id] = s.employee_id " +
            "WHEN MATCHED THEN UPDATE SET " +
            "  [employee_code]=@employee_code,[full_name]=@full_name,[mobile_phone]=@mobile_phone," +
            "  [work_email]=@work_email,[department_id]=@department_id,[job_id]=@job_id," +
            "  [vitri_congviec_id]=@vitri_congviec_id,[vitri_congviec_name]=@vitri_congviec_name," +
            "  [vitri_congviec_code]=@vitri_congviec_code,[birthday]=@birthday,[gioi_tinh]=@gioi_tinh," +
            "  [is_congtacvien]=@is_congtacvien,[updated_at]=@updated_at " +
            "WHEN NOT MATCHED THEN INSERT " +
            "  ([employee_id],[employee_code],[full_name],[mobile_phone],[work_email],[department_id]," +
            "   [job_id],[vitri_congviec_id],[vitri_congviec_name],[vitri_congviec_code],[birthday]," +
            "   [gioi_tinh],[is_congtacvien],[updated_at]) " +
            "  VALUES (@employee_id,@employee_code,@full_name,@mobile_phone,@work_email,@department_id," +
            "   @job_id,@vitri_congviec_id,@vitri_congviec_name,@vitri_congviec_code,@birthday," +
            "   @gioi_tinh,@is_congtacvien,@updated_at);";

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
                    "  [employee_id] INT NOT NULL PRIMARY KEY," +
                    "  [employee_code] NVARCHAR(50) NULL," +
                    "  [full_name] NVARCHAR(255) NULL," +
                    "  [mobile_phone] NVARCHAR(30) NULL," +
                    "  [work_email] NVARCHAR(255) NULL," +
                    "  [department_id] INT NULL," +
                    "  [job_id] INT NULL," +
                    "  [vitri_congviec_id] INT NULL," +
                    "  [vitri_congviec_name] NVARCHAR(255) NULL," +
                    "  [vitri_congviec_code] NVARCHAR(50) NULL," +
                    "  [birthday] DATE NULL," +
                    "  [gioi_tinh] NVARCHAR(20) NULL," +
                    "  [is_congtacvien] BIT NOT NULL," +
                    "  [updated_at] DATETIME2 NOT NULL" +
                    ")";

                using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();

                const string index =
                    "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_" + Table + "_department_id') " +
                    "CREATE INDEX [IX_" + Table + "_department_id] ON [" + Table + "] ([department_id])";

                using (var cmd = new SqlCommand(index, conn)) cmd.ExecuteNonQuery();

                _schemaReady = true;
            }
        }

        /// <summary>Xoá sạch bảng — dùng trước khi nạp lại toàn bộ danh bạ từ HRM.</summary>
        public static void DeleteAll()
        {
            using (var conn = Db.Open())
            {
                EnsureTable(conn);
                using (var cmd = new SqlCommand("TRUNCATE TABLE [" + Table + "]", conn)) cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Ghi cả mẻ nhân sự, trùng employee_id thì cập nhật.</summary>
        public static int SaveMany(IEnumerable<EmployeeHrm> employees, Action<string> progress = null)
        {
            if (employees == null) throw new ArgumentNullException("employees");

            var now = DateTime.Now;
            var list = employees
                .Where(e => e != null)
                .GroupBy(e => e.EmployeeId)
                .Select(g => g.First())
                .ToList();

            foreach (var e in list) e.UpdatedAt = now;

            return HrBulk.Upsert(list, MergeSql, DeclareParams, BindParams, EnsureTable, progress, "nhân sự HRM");
        }

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
            cmd.Parameters.Add("@employee_id", SqlDbType.Int);
            cmd.Parameters.Add("@employee_code", SqlDbType.NVarChar, CodeLen);
            cmd.Parameters.Add("@full_name", SqlDbType.NVarChar, NameLen);
            cmd.Parameters.Add("@mobile_phone", SqlDbType.NVarChar, PhoneLen);
            cmd.Parameters.Add("@work_email", SqlDbType.NVarChar, EmailLen);
            cmd.Parameters.Add("@department_id", SqlDbType.Int);
            cmd.Parameters.Add("@job_id", SqlDbType.Int);
            cmd.Parameters.Add("@vitri_congviec_id", SqlDbType.Int);
            cmd.Parameters.Add("@vitri_congviec_name", SqlDbType.NVarChar, NameLen);
            cmd.Parameters.Add("@vitri_congviec_code", SqlDbType.NVarChar, CodeLen);
            cmd.Parameters.Add("@birthday", SqlDbType.Date);
            cmd.Parameters.Add("@gioi_tinh", SqlDbType.NVarChar, GenderLen);
            cmd.Parameters.Add("@is_congtacvien", SqlDbType.Bit);
            cmd.Parameters.Add("@updated_at", SqlDbType.DateTime2);
        }

        private static void BindParams(SqlCommand cmd, EmployeeHrm e)
        {
            cmd.Parameters["@employee_id"].Value = e.EmployeeId;
            cmd.Parameters["@employee_code"].Value = HrBulk.Fit(e.EmployeeCode, CodeLen);
            cmd.Parameters["@full_name"].Value = HrBulk.Fit(e.FullName, NameLen);
            cmd.Parameters["@mobile_phone"].Value = HrBulk.Fit(e.MobilePhone, PhoneLen);
            cmd.Parameters["@work_email"].Value = HrBulk.Fit(e.WorkEmail, EmailLen);
            cmd.Parameters["@department_id"].Value = HrBulk.Num(e.DepartmentId);
            cmd.Parameters["@job_id"].Value = HrBulk.Num(e.JobId);
            cmd.Parameters["@vitri_congviec_id"].Value = HrBulk.Num(e.ViTriCongViecId);
            cmd.Parameters["@vitri_congviec_name"].Value = HrBulk.Fit(e.ViTriCongViecName, NameLen);
            cmd.Parameters["@vitri_congviec_code"].Value = HrBulk.Fit(e.ViTriCongViecCode, CodeLen);
            cmd.Parameters["@birthday"].Value = e.Birthday.HasValue ? (object)e.Birthday.Value : DBNull.Value;
            cmd.Parameters["@gioi_tinh"].Value = HrBulk.Fit(e.GioiTinh, GenderLen);
            cmd.Parameters["@is_congtacvien"].Value = e.IsCongTacVien;
            cmd.Parameters["@updated_at"].Value = e.UpdatedAt;
        }
    }
}
