using System;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Models.Work
{
    /// <summary>
    /// Phân loại ngày nghỉ lễ:
    /// - AnnualFixed: Cố định hàng năm (dựa trên Ngày và Tháng, áp dụng lặp lại mọi năm)
    /// - Compensatory: Nghỉ bù / Lễ theo năm cụ thể (khai báo theo ngày cụ thể)
    /// - Custom: Ngày nghỉ đặc biệt khác
    /// </summary>
    public enum HolidayKind
    {
        AnnualFixed = 1,
        Compensatory = 2,
        Custom = 3
    }

    /// <summary>
    /// Thực thể Ngày nghỉ lễ trong hệ thống.
    /// </summary>
    public class Holiday : IEntity
    {
        public int Id { get; set; }

        /// <summary>Tên ngày nghỉ lễ (ví dụ: Tết Dương Lịch, Nghỉ bù Quốc khánh 02/09)</summary>
        public string Name { get; set; }

        /// <summary>Loại ngày nghỉ</summary>
        public HolidayKind Kind { get; set; }

        /// <summary>Ngày trong tháng (1 - 31), dùng cho loại cố định hàng năm</summary>
        public int? Day { get; set; }

        /// <summary>Tháng trong năm (1 - 12), dùng cho loại cố định hàng năm</summary>
        public int? Month { get; set; }

        /// <summary>Ngày nghỉ cụ thể, dùng cho loại nghỉ bù hoặc lễ riêng của một năm</summary>
        public DateTime? SpecificDate { get; set; }

        /// <summary>Năm áp dụng (null = áp dụng mọi năm cho lễ cố định, hoặc khớp năm của SpecificDate)</summary>
        public int? Year { get; set; }

        /// <summary>Ghi chú / Căn cứ quyết định nghỉ</summary>
        public string Note { get; set; }

        /// <summary>Trạng thái kích hoạt (true = có tính nghỉ lễ, false = tạm tắt)</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Thời gian tạo bản ghi</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Chuỗi hiển thị ngày nghỉ (ví dụ: "02/09 hàng năm" hoặc "03/09/2026")
        /// </summary>
        public string DisplayDate
        {
            get
            {
                if (Kind == HolidayKind.AnnualFixed && Day.HasValue && Month.HasValue)
                {
                    return string.Format("{0:D2}/{1:D2} hàng năm", Day.Value, Month.Value);
                }
                if (SpecificDate.HasValue)
                {
                    return SpecificDate.Value.ToString("dd/MM/yyyy");
                }
                if (Day.HasValue && Month.HasValue)
                {
                    return string.Format("{0:D2}/{1:D2}", Day.Value, Month.Value);
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Tên phân loại tiếng Việt
        /// </summary>
        public string KindDisplay
        {
            get
            {
                switch (Kind)
                {
                    case HolidayKind.AnnualFixed:
                        return "Cố định hàng năm";
                    case HolidayKind.Compensatory:
                        return "Nghỉ bù / Theo năm";
                    case HolidayKind.Custom:
                        return "Đặc biệt";
                    default:
                        return "Khác";
                }
            }
        }
    }
}
