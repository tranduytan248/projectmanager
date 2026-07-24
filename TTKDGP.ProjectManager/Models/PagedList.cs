using System;
using System.Collections.Generic;
using System.Linq;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Thông tin phân trang, đủ để vẽ thanh số trang. Tách riêng khỏi <see cref="PagedList{T}"/>
    /// để phần hiển thị thanh số trang dùng chung được cho mọi loại dữ liệu.
    /// </summary>
    public class PagerInfo
    {
        /// <summary>Trang hiện tại, đếm từ 1.</summary>
        public int Page { get; set; }

        public int PageSize { get; set; }

        /// <summary>Tổng số bản ghi SAU khi lọc.</summary>
        public int TotalItems { get; set; }

        public int TotalPages
        {
            get { return PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize)); }
        }

        public bool HasPrevious { get { return Page > 1; } }
        public bool HasNext { get { return Page < TotalPages; } }

        /// <summary>Số thứ tự bản ghi đầu tiên của trang này, đếm từ 1. 0 khi không có gì.</summary>
        public int FirstItem { get { return TotalItems == 0 ? 0 : (Page - 1) * PageSize + 1; } }

        /// <summary>Số thứ tự bản ghi cuối cùng của trang này.</summary>
        public int LastItem { get { return Math.Min(Page * PageSize, TotalItems); } }

        /// <summary>
        /// Dãy số trang nên vẽ: luôn có trang đầu và trang cuối, cộng thêm vài trang quanh trang
        /// hiện tại. Giá trị 0 nghĩa là chỗ ngắt quãng (hiển thị dấu …), để 16 trang hay 200
        /// trang thì thanh số trang vẫn dài như nhau.
        /// </summary>
        public List<int> PageWindow(int radius = 2)
        {
            var pages = new List<int>();
            var last = TotalPages;

            for (var i = 1; i <= last; i++)
            {
                var near = i >= Page - radius && i <= Page + radius;
                if (i != 1 && i != last && !near) continue;

                if (pages.Count > 0 && i - pages[pages.Count - 1] > 1) pages.Add(0);
                pages.Add(i);
            }

            return pages;
        }
    }

    /// <summary>Một trang dữ liệu kèm thông tin phân trang.</summary>
    public class PagedList<T>
    {
        public List<T> Items { get; set; }
        public PagerInfo Pager { get; set; }

        public PagedList()
        {
            Items = new List<T>();
            Pager = new PagerInfo { Page = 1, PageSize = DefaultPageSize };
        }

        public const int DefaultPageSize = 25;

        /// <summary>
        /// Cắt một trang từ danh sách đã có sẵn trong bộ nhớ. Dùng cho những màn hình mà dữ liệu
        /// vốn đã được nạp và lọc hết trong bộ nhớ (dự án, thành viên...).
        /// </summary>
        public static PagedList<T> From(IEnumerable<T> source, int? page, int pageSize = DefaultPageSize)
        {
            var all = source as IList<T> ?? (source ?? Enumerable.Empty<T>()).ToList();

            var pager = new PagerInfo
            {
                PageSize = pageSize <= 0 ? DefaultPageSize : pageSize,
                TotalItems = all.Count,
                Page = 1
            };

            pager.Page = Clamp(page, pager.TotalPages);

            return new PagedList<T>
            {
                Pager = pager,
                Items = all.Skip((pager.Page - 1) * pager.PageSize).Take(pager.PageSize).ToList()
            };
        }

        /// <summary>
        /// Dựng từ dữ liệu đã được tầng CSDL cắt sẵn (OFFSET/FETCH) — không nạp cả bảng lên
        /// bộ nhớ chỉ để bỏ đi.
        /// </summary>
        public static PagedList<T> FromSlice(List<T> items, int page, int pageSize, int totalItems)
        {
            return new PagedList<T>
            {
                Items = items ?? new List<T>(),
                Pager = new PagerInfo { Page = page, PageSize = pageSize, TotalItems = totalItems }
            };
        }

        /// <summary>
        /// Ép số trang về khoảng hợp lệ. Người dùng sửa tay ?page=999 trên thanh địa chỉ thì
        /// nhận trang cuối chứ không phải trang trắng.
        /// </summary>
        public static int Clamp(int? page, int totalPages)
        {
            var value = page.HasValue ? page.Value : 1;
            if (value < 1) return 1;
            return value > totalPages ? totalPages : value;
        }
    }
}
