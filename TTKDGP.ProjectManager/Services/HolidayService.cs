using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Models.Work;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Nghiệp vụ quản lý Ngày nghỉ lễ (Cố định hàng năm và Nghỉ bù / Tự khai báo theo năm).
    /// </summary>
    public static class HolidayService
    {
        private static readonly object _initLock = new object();
        private static bool _initialized = false;

        /// <summary>
        /// Khởi tạo dữ liệu mẫu các ngày lễ cố định cơ bản nếu bảng còn trống.
        /// </summary>
        private static void EnsureSeeded()
        {
            if (_initialized) return;

            lock (_initLock)
            {
                if (_initialized) return;

                var existing = Repository.Holidays.All();
                if (existing == null || existing.Count == 0)
                {
                    var seedList = new List<Holiday>
                    {
                        new Holiday
                        {
                            Name = "Tết Dương Lịch",
                            Kind = HolidayKind.AnnualFixed,
                            Day = 1,
                            Month = 1,
                            Year = null,
                            Note = "Nghỉ 1 ngày (01/01 hàng năm)",
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Holiday
                        {
                            Name = "Ngày Giải phóng miền Nam",
                            Kind = HolidayKind.AnnualFixed,
                            Day = 30,
                            Month = 4,
                            Year = null,
                            Note = "Nghỉ 1 ngày (30/04 hàng năm)",
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Holiday
                        {
                            Name = "Ngày Quốc tế Lao động",
                            Kind = HolidayKind.AnnualFixed,
                            Day = 1,
                            Month = 5,
                            Year = null,
                            Note = "Nghỉ 1 ngày (01/05 hàng năm)",
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Holiday
                        {
                            Name = "Ngày Quốc khánh",
                            Kind = HolidayKind.AnnualFixed,
                            Day = 2,
                            Month = 9,
                            Year = null,
                            Note = "Nghỉ 1 ngày (02/09 hàng năm)",
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        }
                    };

                    foreach (var h in seedList)
                    {
                        Repository.Holidays.Insert(h);
                    }
                }

                _initialized = true;
            }
        }

        /// <summary>
        /// Lấy tất cả ngày nghỉ lễ trong hệ thống.
        /// </summary>
        public static List<Holiday> All()
        {
            EnsureSeeded();
            return Repository.Holidays.All()
                .OrderBy(h => h.Month ?? (h.SpecificDate.HasValue ? h.SpecificDate.Value.Month : 12))
                .ThenBy(h => h.Day ?? (h.SpecificDate.HasValue ? h.SpecificDate.Value.Day : 31))
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách ngày nghỉ lễ áp dụng cho một năm cụ thể.
        /// Bao gồm các ngày cố định hàng năm và các ngày nghỉ bù/cụ thể của năm đó.
        /// </summary>
        public static List<Holiday> ForYear(int year)
        {
            EnsureSeeded();
            return Repository.Holidays.All()
                .Where(h => h.IsActive &&
                    (
                        (h.Kind == HolidayKind.AnnualFixed && (h.Year == null || h.Year == year)) ||
                        (h.SpecificDate.HasValue && h.SpecificDate.Value.Year == year) ||
                        (h.Year.HasValue && h.Year == year)
                    ))
                .OrderBy(h => h.Month ?? (h.SpecificDate.HasValue ? h.SpecificDate.Value.Month : 12))
                .ThenBy(h => h.Day ?? (h.SpecificDate.HasValue ? h.SpecificDate.Value.Day : 31))
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách ngày nghỉ lễ rơi vào một tháng trong năm.
        /// </summary>
        public static List<Holiday> InMonth(int year, int month)
        {
            EnsureSeeded();
            return Repository.Holidays.All()
                .Where(h => h.IsActive &&
                    (
                        (h.Kind == HolidayKind.AnnualFixed && h.Month == month && (h.Year == null || h.Year == year)) ||
                        (h.SpecificDate.HasValue && h.SpecificDate.Value.Year == year && h.SpecificDate.Value.Month == month) ||
                        (h.Month == month && h.Year == year)
                    ))
                .ToList();
        }

        /// <summary>
        /// Kiểm tra một ngày cụ thể có phải là ngày nghỉ lễ không.
        /// </summary>
        public static bool IsHoliday(DateTime date)
        {
            EnsureSeeded();
            var targetDate = date.Date;
            var year = targetDate.Year;
            var month = targetDate.Month;
            var day = targetDate.Day;

            var holidays = Repository.Holidays.All();
            if (holidays == null || holidays.Count == 0) return false;

            return holidays.Any(h => h.IsActive &&
                (
                    // Khớp ngày lễ cố định hàng năm
                    (h.Kind == HolidayKind.AnnualFixed && h.Day == day && h.Month == month && (h.Year == null || h.Year == year)) ||
                    // Khớp ngày lễ cụ thể / nghỉ bù
                    (h.SpecificDate.HasValue && h.SpecificDate.Value.Date == targetDate) ||
                    // Khớp ngày/tháng/năm
                    (h.Day == day && h.Month == month && h.Year == year)
                ));
        }

        /// <summary>
        /// Lấy thông tin ngày lễ theo Id.
        /// </summary>
        public static Holiday Find(int id)
        {
            EnsureSeeded();
            return Repository.Holidays.Find(id);
        }

        /// <summary>
        /// Thêm mới hoặc cập nhật ngày nghỉ lễ.
        /// </summary>
        public static void Save(Holiday item)
        {
            if (item == null) return;

            // Chuẩn hóa dữ liệu theo loại
            if (item.Kind == HolidayKind.AnnualFixed)
            {
                if (item.SpecificDate.HasValue)
                {
                    item.Day = item.SpecificDate.Value.Day;
                    item.Month = item.SpecificDate.Value.Month;
                }
            }
            else if (item.Kind == HolidayKind.Compensatory)
            {
                if (item.SpecificDate.HasValue)
                {
                    item.Day = item.SpecificDate.Value.Day;
                    item.Month = item.SpecificDate.Value.Month;
                    item.Year = item.SpecificDate.Value.Year;
                }
            }

            if (item.Id > 0)
            {
                Repository.Holidays.Update(item);
            }
            else
            {
                item.CreatedAt = DateTime.Now;
                Repository.Holidays.Insert(item);
            }
        }

        /// <summary>
        /// Xoá ngày nghỉ lễ.
        /// </summary>
        public static void Delete(int id)
        {
            Repository.Holidays.Delete(id);
        }

        /// <summary>
        /// Bật/tắt trạng thái kích hoạt ngày nghỉ lễ.
        /// </summary>
        public static bool ToggleActive(int id)
        {
            var item = Repository.Holidays.Find(id);
            if (item == null) return false;

            item.IsActive = !item.IsActive;
            Repository.Holidays.Update(item);
            return item.IsActive;
        }
    }
}
