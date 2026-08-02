using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Bộ nhớ đệm dùng chung cho các kết quả tính toán nặng (tổng hợp theo tháng, bảng số liệu
    /// dashboard...).
    ///
    /// Không phải bộ đệm dữ liệu thô: <see cref="Data.SqlStore{T}"/> đã giữ sẵn toàn bộ bản ghi
    /// trong bộ nhớ. Chỗ tốn thời gian là mỗi lượt mở trang lại duyệt cả bảng để gom nhóm, đếm và
    /// nối dữ liệu — dữ liệu càng nhiều thì càng chậm. Lớp này giữ lại chính kết quả đã gom nhóm
    /// trong một khoảng thời gian ngắn.
    ///
    /// Dùng HttpRuntime.Cache của ASP.NET (tự dọn khi bộ nhớ eo hẹp, tự hết hạn theo TTL) thay vì
    /// tự dựng Dictionary tĩnh — Dictionary tĩnh không bao giờ nhả bộ nhớ và không có hạn dùng.
    /// </summary>
    public static class AppCache
    {
        /// <summary>Thời gian sống mặc định: đủ ngắn để số liệu không lệch quá lâu sau khi có thay đổi.</summary>
        public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Tiền tố chung của mọi khoá do lớp này quản lý, để phân biệt với các mục khác trong
        /// HttpRuntime.Cache và để xoá hàng loạt theo nhóm.
        /// </summary>
        private const string Prefix = "TTKDGP:";

        private static Cache Store
        {
            get { return HttpRuntime.Cache; }
        }

        /// <summary>Ghép các phần thành một khoá đầy đủ, ví dụ Key("mydash", 7, 2026, 8).</summary>
        public static string Key(string group, params object[] parts)
        {
            if (parts == null || parts.Length == 0) return Prefix + group;
            return Prefix + group + ":" + string.Join(":", parts.Select(p => Convert.ToString(p)));
        }

        /// <summary>
        /// Lấy giá trị trong bộ đệm; chưa có (hoặc đã hết hạn) thì gọi <paramref name="build"/> để
        /// tính rồi giữ lại.
        ///
        /// Bộ đệm CHỈ giữ kết quả khác null: null thường là dấu hiệu tính không thành công (SQL
        /// chập chờn), giữ lại thì suốt thời gian còn hạn người dùng đều thấy màn hình trống.
        /// </summary>
        public static T GetOrAdd<T>(string key, Func<T> build, TimeSpan? ttl = null) where T : class
        {
            var cached = Store[key] as T;
            if (cached != null) return cached;

            var value = build();
            if (value == null) return null;

            Store.Insert(key, value, null,
                DateTime.UtcNow.Add(ttl ?? DefaultTtl), Cache.NoSlidingExpiration);

            return value;
        }

        /// <summary>Bỏ một mục khỏi bộ đệm.</summary>
        public static void Remove(string key)
        {
            Store.Remove(key);
        }

        /// <summary>
        /// Bỏ toàn bộ mục của một nhóm — gọi sau khi dữ liệu nguồn thay đổi để lần xem kế tiếp
        /// tính lại từ đầu.
        ///
        /// Phải liệt kê khoá ra danh sách trước rồi mới xoá: bộ liệt kê của Cache không cho phép
        /// vừa duyệt vừa sửa.
        /// </summary>
        public static void RemoveGroup(string group)
        {
            var head = Prefix + group;
            var keys = new List<string>();

            var enumerator = Store.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var key = enumerator.Key as string;
                if (key != null && key.StartsWith(head, StringComparison.Ordinal)) keys.Add(key);
            }

            foreach (var key in keys) Store.Remove(key);
        }
    }
}
