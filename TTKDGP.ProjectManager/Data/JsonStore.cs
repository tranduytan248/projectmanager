using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Data
{
    /// <summary>
    /// Kho dữ liệu dạng file JSON cho một loại entity.
    /// Dữ liệu được nạp một lần vào bộ nhớ và ghi lại toàn bộ file sau mỗi thay đổi.
    /// Đủ dùng ở quy mô vài trăm bản ghi như hiện tại; nếu dữ liệu lớn lên thì nên chuyển sang CSDL.
    /// </summary>
    public class JsonStore<T> where T : class, IEntity
    {
        private readonly string _filePath;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private List<T> _items;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };

        public JsonStore(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// Bản sao dữ liệu mẫu đi kèm mã nguồn. Dữ liệu thật không nằm trong kho mã, nên lần đầu
        /// chạy trên máy mới sẽ được nạp từ đây để hệ thống dùng được ngay.
        /// </summary>
        private string SeedPath
        {
            get
            {
                var dir = Path.GetDirectoryName(_filePath) ?? string.Empty;
                return Path.Combine(dir, "seed", Path.GetFileName(_filePath));
            }
        }

        private void EnsureLoaded()
        {
            if (_items != null) return;

            _lock.EnterWriteLock();
            try
            {
                if (_items != null) return;

                if (!File.Exists(_filePath) && File.Exists(SeedPath))
                {
                    File.Copy(SeedPath, _filePath);
                }

                if (!File.Exists(_filePath))
                {
                    _items = new List<T>();
                    return;
                }

                var json = File.ReadAllText(_filePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _items = new List<T>();
                    return;
                }

                _items = JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>Ghi toàn bộ danh sách xuống file. Ghi ra file tạm rồi thay thế, tránh hỏng file khi ghi dở.</summary>
        private void Persist()
        {
            var json = JsonConvert.SerializeObject(_items, SerializerSettings);
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json, new UTF8Encoding(false));

            if (File.Exists(_filePath))
            {
                var backupPath = _filePath + ".bak";
                File.Replace(tempPath, _filePath, backupPath, true);
            }
            else
            {
                File.Move(tempPath, _filePath);
            }
        }

        /// <summary>
        /// Trả về bản sao của danh sách. Lưu ý: các phần tử vẫn là tham chiếu tới object trong cache,
        /// nên đừng sửa trực tiếp object nhận được — hãy dựng object mới rồi gọi <see cref="Update"/>,
        /// nếu không thay đổi sẽ nằm trong bộ nhớ mà không được ghi xuống file.
        /// </summary>
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

        public T Find(int id)
        {
            EnsureLoaded();
            _lock.EnterReadLock();
            try
            {
                return _items.FirstOrDefault(x => x.Id == id);
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
                item.Id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1;
                _items.Add(item);
                Persist();
                return item;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>Thay thế bản ghi cùng Id. Trả về false nếu không tìm thấy.</summary>
        public bool Update(T item)
        {
            EnsureLoaded();
            _lock.EnterWriteLock();
            try
            {
                var index = _items.FindIndex(x => x.Id == item.Id);
                if (index < 0) return false;

                _items[index] = item;
                Persist();
                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Delete(int id)
        {
            EnsureLoaded();
            _lock.EnterWriteLock();
            try
            {
                var index = _items.FindIndex(x => x.Id == id);
                if (index < 0) return false;

                _items.RemoveAt(index);
                Persist();
                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>Xoá nhiều bản ghi trong một lần ghi file. Trả về số bản ghi đã xoá.</summary>
        public int DeleteWhere(Predicate<T> predicate)
        {
            EnsureLoaded();
            _lock.EnterWriteLock();
            try
            {
                var removed = _items.RemoveAll(predicate);
                if (removed > 0) Persist();
                return removed;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>Sửa hàng loạt rồi ghi file một lần, dùng khi cần đồng bộ nhiều bản ghi liên quan.</summary>
        public void UpdateWhere(Func<T, bool> predicate, Action<T> mutate)
        {
            EnsureLoaded();
            _lock.EnterWriteLock();
            try
            {
                var affected = _items.Where(predicate).ToList();
                if (affected.Count == 0) return;

                foreach (var item in affected) mutate(item);
                Persist();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
