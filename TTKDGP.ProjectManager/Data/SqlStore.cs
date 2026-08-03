using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Data
{
    /// <summary>
    /// Kho dữ liệu SQL Server cho một loại entity, thay cho <see cref="JsonStore{T}"/>.
    /// Giữ NGUYÊN API (All/Find/FirstOrDefault/Insert/Update/Delete/DeleteWhere/UpdateWhere)
    /// để phần còn lại của ứng dụng không phải thay đổi.
    ///
    /// Mô hình vẫn như cũ: nạp toàn bộ bản ghi vào bộ nhớ một lần rồi thao tác trên đó;
    /// khác biệt là mỗi thay đổi được ghi thẳng xuống một dòng trong SQL thay vì ghi lại cả file.
    /// Cột được suy ra từ thuộc tính public có cả get lẫn set. Kiểu List&lt;int&gt; (ParticipantIds)
    /// lưu dưới dạng chuỗi JSON trong một cột NVARCHAR(MAX).
    /// </summary>
    public class SqlStore<T> where T : class, IEntity
    {
        private readonly string _table;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private List<T> _items;
        private bool _schemaReady;

        /// <summary>
        /// Chỉ mục tra nhanh theo Id cho <see cref="Find"/>. Dựng lười ở lần tra đầu tiên và bị bỏ
        /// đi ngay khi danh sách thay đổi — xem <see cref="InvalidateIndex"/>.
        /// </summary>
        private Dictionary<int, T> _byId;

        /// <summary>
        /// Bỏ chỉ mục Id đang giữ. Phải gọi trong lúc còn giữ khoá ghi, ở MỌI chỗ có đụng vào
        /// <c>_items</c>: sót một chỗ là Find còn trả về bản ghi đã xoá hoặc bỏ sót bản ghi vừa thêm.
        /// </summary>
        private void InvalidateIndex()
        {
            _byId = null;
        }

        /// <summary>Các thuộc tính được ánh xạ thành cột, kể cả Id. Tính một lần cho mỗi kiểu T.</summary>
        private static readonly PropertyInfo[] MappedProps = BuildMappedProps();

        /// <summary>Các cột ghi khi Insert/Update — mọi cột trừ Id (khoá tự tăng).</summary>
        private static readonly PropertyInfo[] WritableProps =
            MappedProps.Where(p => !IsId(p)).ToArray();

        public SqlStore(string tableName)
        {
            _table = tableName;
        }

        /// <summary>
        /// Báo hiệu dữ liệu của bảng vừa đổi (thêm/sửa/xoá). Dùng để bỏ các kết quả đã gom nhóm
        /// giữ trong <see cref="AppCache"/> — đăng ký ở đây thì mọi nơi ghi dữ liệu đều được tính
        /// đến, khỏi phải nhớ gọi tay ở từng controller.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// Bắn sự kiện <see cref="Changed"/>. Luôn gọi SAU khi đã nhả khoá ghi: handler chạy trong
        /// cùng luồng, nếu còn giữ khoá mà handler lỡ đọc lại kho này thì treo.
        /// </summary>
        private void OnChanged()
        {
            var handler = Changed;
            if (handler != null) handler();
        }

        // ---------- Ánh xạ thuộc tính ↔ cột ----------

        private static bool IsId(PropertyInfo p)
        {
            return string.Equals(p.Name, "Id", StringComparison.Ordinal);
        }

        private static PropertyInfo[] BuildMappedProps()
        {
            return typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .Where(p => IsSupported(p.PropertyType))
                .ToArray();
        }

        private static bool IsIntList(Type type)
        {
            return type == typeof(List<int>);
        }

        private static bool IsSupported(Type type)
        {
            if (IsIntList(type)) return true;

            var t = Nullable.GetUnderlyingType(type) ?? type;
            if (t.IsEnum) return true;

            return t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) ||
                   t == typeof(bool) || t == typeof(string) || t == typeof(DateTime) ||
                   t == typeof(decimal) || t == typeof(double) || t == typeof(float) || t == typeof(Guid);
        }

        /// <summary>Kiểu cột SQL cho một thuộc tính. Mọi cột trừ Id đều cho phép NULL cho an toàn.</summary>
        private static string SqlColumnType(Type type)
        {
            if (IsIntList(type) || type == typeof(string)) return "NVARCHAR(MAX)";

            var t = Nullable.GetUnderlyingType(type) ?? type;
            if (t.IsEnum) return "INT";
            if (t == typeof(int) || t == typeof(short) || t == typeof(byte)) return "INT";
            if (t == typeof(long)) return "BIGINT";
            if (t == typeof(bool)) return "BIT";
            if (t == typeof(DateTime)) return "DATETIME2";
            if (t == typeof(decimal)) return "DECIMAL(18,2)";
            if (t == typeof(double) || t == typeof(float)) return "FLOAT";
            if (t == typeof(Guid)) return "UNIQUEIDENTIFIER";

            return "NVARCHAR(MAX)";
        }

        /// <summary>Chuyển giá trị đọc từ SQL về đúng kiểu thuộc tính.</summary>
        private static object FromDb(object dbValue, Type type)
        {
            if (dbValue == null || dbValue is DBNull)
            {
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }

            if (IsIntList(type))
            {
                var text = dbValue as string;
                if (string.IsNullOrWhiteSpace(text)) return new List<int>();
                return JsonConvert.DeserializeObject<List<int>>(text) ?? new List<int>();
            }

            var t = Nullable.GetUnderlyingType(type) ?? type;
            if (t.IsEnum) return Enum.ToObject(t, Convert.ToInt32(dbValue));

            return Convert.ChangeType(dbValue, t);
        }

        /// <summary>Chuyển giá trị thuộc tính thành giá trị tham số gửi xuống SQL.</summary>
        private static object ToDb(object propValue, Type type)
        {
            if (propValue == null) return DBNull.Value;

            if (IsIntList(type))
            {
                return JsonConvert.SerializeObject(propValue, Formatting.None);
            }

            var t = Nullable.GetUnderlyingType(type) ?? type;
            if (t.IsEnum) return Convert.ToInt32(propValue);

            return propValue;
        }

        // ---------- Nạp và tạo bảng ----------

        private void EnsureSchema(SqlConnection connection)
        {
            if (_schemaReady) return;

            var columns = new StringBuilder();
            columns.Append("[Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY");
            foreach (var p in WritableProps)
            {
                columns.AppendFormat(", [{0}] {1} NULL", p.Name, SqlColumnType(p.PropertyType));
            }

            var sql = string.Format(
                "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = @t) " +
                "EXEC('CREATE TABLE [' + @t + '] ({0})')",
                columns.ToString().Replace("'", "''"));

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@t", _table);
                cmd.ExecuteNonQuery();
            }

            // Bảng tạo từ trước có thể thiếu cột của thuộc tính mới thêm vào model. Thêm bù từng
            // cột còn thiếu — CHỈ thêm, không bao giờ sửa hay xoá — để model tiến hoá được mà
            // không phải chuyển đổi CSDL bằng tay. Thiếu bước này thì ReadRow sẽ ném lỗi
            // "không tìm thấy cột" ngay khi nạp bảng cũ.
            //
            // Hỏi MỘT lượt xem bảng đang có cột nào, rồi mới thêm phần còn thiếu. Trước đây mỗi
            // cột là một lượt hỏi riêng xuống máy chủ: nhân với mấy chục cột và gần ba chục bảng
            // thành hàng trăm lượt đi về nối đuôi nhau ngay lần đầu chạm tới dữ liệu sau khi khởi
            // động lại — trên đường truyền chập chờn thì đó là cả một khoảng chờ dài.
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SqlCommand(
                "SELECT c.name FROM sys.columns c " +
                "JOIN sys.tables t ON t.object_id = c.object_id WHERE t.name = @t", connection))
            {
                cmd.Parameters.AddWithValue("@t", _table);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) existing.Add(reader.GetString(0));
                }
            }

            var missing = WritableProps.Where(p => !existing.Contains(p.Name)).ToList();
            if (missing.Count > 0)
            {
                // Gộp các cột thiếu vào một câu ALTER duy nhất. Tên cột do model sinh ra chứ không
                // phải dữ liệu người dùng nhập, nhưng vẫn nhân đôi dấu ngoặc vuông cho chắc.
                var adds = string.Join(", ", missing.Select(p => string.Format(
                    "[{0}] {1} NULL", p.Name.Replace("]", "]]"), SqlColumnType(p.PropertyType))));

                using (var cmd = new SqlCommand(
                    string.Format("ALTER TABLE [{0}] ADD {1}", _table.Replace("]", "]]"), adds), connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            _schemaReady = true;
        }

        private void EnsureLoaded()
        {
            if (_items != null) return;

            _lock.EnterWriteLock();
            try
            {
                if (_items != null) return;

                var loaded = new List<T>();
                using (var connection = Db.Open())
                {
                    EnsureSchema(connection);

                    using (var cmd = new SqlCommand(string.Format("SELECT * FROM [{0}] ORDER BY [Id]", _table), connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            loaded.Add(ReadRow(reader));
                        }
                    }
                }

                _items = loaded;
                InvalidateIndex();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private static T ReadRow(SqlDataReader reader)
        {
            var item = (T)Activator.CreateInstance(typeof(T));
            foreach (var p in MappedProps)
            {
                var ordinal = reader.GetOrdinal(p.Name);
                var value = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
                p.SetValue(item, FromDb(value, p.PropertyType));
            }
            return item;
        }

        // ---------- Ghi từng dòng xuống SQL ----------

        private void AddParameters(SqlCommand cmd, T item, IEnumerable<PropertyInfo> props)
        {
            foreach (var p in props)
            {
                cmd.Parameters.AddWithValue("@" + p.Name, ToDb(p.GetValue(item), p.PropertyType));
            }
        }

        private void InsertRow(SqlConnection connection, T item)
        {
            var cols = string.Join(", ", WritableProps.Select(p => "[" + p.Name + "]"));
            var vals = string.Join(", ", WritableProps.Select(p => "@" + p.Name));
            var sql = string.Format(
                "INSERT INTO [{0}] ({1}) VALUES ({2}); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                _table, cols, vals);

            using (var cmd = new SqlCommand(sql, connection))
            {
                AddParameters(cmd, item, WritableProps);
                item.Id = (int)cmd.ExecuteScalar();
            }
        }

        private void UpdateRow(SqlConnection connection, T item)
        {
            var sets = string.Join(", ", WritableProps.Select(p => "[" + p.Name + "] = @" + p.Name));
            var sql = string.Format("UPDATE [{0}] SET {1} WHERE [Id] = @Id", _table, sets);

            using (var cmd = new SqlCommand(sql, connection))
            {
                AddParameters(cmd, item, WritableProps);
                cmd.Parameters.AddWithValue("@Id", item.Id);
                cmd.ExecuteNonQuery();
            }
        }

        private void DeleteRow(SqlConnection connection, int id)
        {
            using (var cmd = new SqlCommand(string.Format("DELETE FROM [{0}] WHERE [Id] = @Id", _table), connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // ---------- API công khai (khớp với JsonStore) ----------

        public List<T> All()
        {
            EnsureLoaded();
            _lock.EnterReadLock();
            try
            {
                return _items.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Tra bản ghi theo Id qua chỉ mục thay vì dò từ đầu danh sách. Find là phép hay dùng nhất
        /// trong toàn ứng dụng — một lượt mở trang gọi hàng chục lần — nên dò tuyến tính ở đây làm
        /// chậm đều khắp nơi khi dữ liệu nhiều dần.
        ///
        /// Chỉ mục dựng theo kiểu lười và bị bỏ đi mỗi khi danh sách đổi, nên không bao giờ lệch
        /// với dữ liệu thật.
        /// </summary>
        public T Find(int id)
        {
            EnsureLoaded();
            _lock.EnterReadLock();
            try
            {
                // Đọc _byId ra biến cục bộ RỒI mới dùng, và chỉ dựng trên biến cục bộ. Nhiều luồng
                // cùng đọc có thể cùng lúc thấy chỉ mục trống và cùng dựng — chấp nhận được: mỗi
                // luồng tra trên bản của chính mình nên kết quả luôn đúng, chỉ phí công dựng thêm
                // một lần. Đổi sang khoá ghi để tránh phí công đó thì mọi lượt đọc phải xếp hàng,
                // lợi bất cập hại. Tuyệt đối không đọc thẳng _byId hai lần trong hàm này: giữa hai
                // lần đọc, một luồng ghi có thể xoá chỉ mục và biến nó thành null.
                var index = _byId;
                if (index == null)
                {
                    // Trùng Id về nguyên tắc không xảy ra (khoá chính tự tăng), nhưng dữ liệu nạp
                    // tay từ thời JSON có thể còn sót; giữ bản ghi đầu để khớp với cách dò cũ.
                    index = new Dictionary<int, T>();
                    foreach (var item in _items)
                    {
                        if (!index.ContainsKey(item.Id)) index[item.Id] = item;
                    }
                    _byId = index;
                }

                T found;
                return index.TryGetValue(id, out found) ? found : null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public T FirstOrDefault(Func<T, bool> predicate)
        {
            EnsureLoaded();
            _lock.EnterReadLock();
            try
            {
                return _items.FirstOrDefault(predicate);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public T Insert(T item)
        {
            EnsureLoaded();
            _lock.EnterWriteLock();
            try
            {
                using (var connection = Db.Open())
                {
                    InsertRow(connection, item);
                }
                _items.Add(item);
                InvalidateIndex();
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            OnChanged();
            return item;
        }

        /// <summary>Thay thế bản ghi cùng Id. Trả về false nếu không tìm thấy.</summary>
        public bool Update(T item)
        {
            EnsureLoaded();
            var updated = false;
            _lock.EnterWriteLock();
            try
            {
                var index = _items.FindIndex(x => x.Id == item.Id);
                if (index < 0) return false;

                using (var connection = Db.Open())
                {
                    UpdateRow(connection, item);
                }
                _items[index] = item;
                InvalidateIndex();
                updated = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            if (updated) OnChanged();
            return updated;
        }

        public bool Delete(int id)
        {
            EnsureLoaded();
            var deleted = false;
            _lock.EnterWriteLock();
            try
            {
                var index = _items.FindIndex(x => x.Id == id);
                if (index < 0) return false;

                using (var connection = Db.Open())
                {
                    DeleteRow(connection, id);
                }
                _items.RemoveAt(index);
                InvalidateIndex();
                deleted = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            if (deleted) OnChanged();
            return deleted;
        }

        /// <summary>Xoá nhiều bản ghi. Trả về số bản ghi đã xoá.</summary>
        public int DeleteWhere(Predicate<T> predicate)
        {
            EnsureLoaded();
            var removed = 0;
            _lock.EnterWriteLock();
            try
            {
                var toRemove = _items.Where(x => predicate(x)).ToList();
                if (toRemove.Count == 0) return 0;

                using (var connection = Db.Open())
                {
                    foreach (var item in toRemove) DeleteRow(connection, item.Id);
                }

                _items.RemoveAll(predicate);
                InvalidateIndex();
                removed = toRemove.Count;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            if (removed > 0) OnChanged();
            return removed;
        }

        /// <summary>Sửa hàng loạt các bản ghi khớp điều kiện rồi ghi từng dòng xuống SQL.</summary>
        public void UpdateWhere(Func<T, bool> predicate, Action<T> mutate)
        {
            EnsureLoaded();
            var changed = false;
            _lock.EnterWriteLock();
            try
            {
                var affected = _items.Where(predicate).ToList();
                if (affected.Count == 0) return;

                foreach (var item in affected) mutate(item);

                using (var connection = Db.Open())
                {
                    foreach (var item in affected) UpdateRow(connection, item);
                }

                changed = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            if (changed) OnChanged();
        }

        // ---------- Dùng cho việc nạp dữ liệu ban đầu từ JSON ----------

        /// <summary>Số bản ghi hiện có trong bảng SQL.</summary>
        public int Count()
        {
            EnsureLoaded();
            _lock.EnterReadLock();
            try
            {
                return _items.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Chèn một bản ghi GIỮ NGUYÊN Id sẵn có (bật IDENTITY_INSERT). Chỉ dùng khi nạp dữ liệu
        /// lần đầu từ JSON sang, để các tham chiếu chéo theo Id không bị lệch.
        /// </summary>
        public void InsertPreservingId(T item)
        {
            EnsureLoaded();
            _lock.EnterWriteLock();
            try
            {
                var cols = "[Id], " + string.Join(", ", WritableProps.Select(p => "[" + p.Name + "]"));
                var vals = "@Id, " + string.Join(", ", WritableProps.Select(p => "@" + p.Name));
                var sql = string.Format(
                    "SET IDENTITY_INSERT [{0}] ON; INSERT INTO [{0}] ({1}) VALUES ({2}); SET IDENTITY_INSERT [{0}] OFF;",
                    _table, cols, vals);

                using (var connection = Db.Open())
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", item.Id);
                    AddParameters(cmd, item, WritableProps);
                    cmd.ExecuteNonQuery();
                }

                _items.Add(item);
                InvalidateIndex();
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            OnChanged();
        }
    }
}
