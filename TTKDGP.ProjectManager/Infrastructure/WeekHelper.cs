using System;
using System.Collections.Generic;
using System.Globalization;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Tính tuần theo chuẩn ISO 8601: tuần bắt đầu từ Thứ Hai, tuần 1 là tuần chứa ngày 4/1.
    /// .NET Framework 4.8 không có sẵn lớp ISOWeek nên tự cài đặt ở đây.
    /// </summary>
    public static class WeekHelper
    {
        /// <summary>Số tuần ISO của một ngày (1..53).</summary>
        public static int GetWeek(DateTime date)
        {
            // Dời ngày đầu tuần về Thứ Năm rồi lấy tuần theo lịch — cách chuẩn để khớp ISO 8601.
            var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }

            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Năm ISO của một ngày. Vài ngày đầu tháng 1 có thể thuộc tuần cuối của năm trước,
        /// và vài ngày cuối tháng 12 có thể thuộc tuần 1 của năm sau.
        /// </summary>
        public static int GetYear(DateTime date)
        {
            var week = GetWeek(date);

            if (date.Month == 12 && week == 1) return date.Year + 1;
            if (date.Month == 1 && week >= 52) return date.Year - 1;
            return date.Year;
        }

        /// <summary>Thứ Hai đầu tiên của một tuần ISO.</summary>
        public static DateTime FirstDayOfWeek(int year, int week)
        {
            var jan4 = new DateTime(year, 1, 4);
            var dow = (int)jan4.DayOfWeek;
            var isoDow = dow == 0 ? 7 : dow;          // Chủ Nhật = 7 theo ISO
            var week1Monday = jan4.AddDays(1 - isoDow);
            return week1Monday.AddDays((week - 1) * 7);
        }

        public static DateTime LastDayOfWeek(int year, int week)
        {
            return FirstDayOfWeek(year, week).AddDays(6);
        }

        /// <summary>Số tuần của một năm ISO: 52 hoặc 53.</summary>
        public static int WeeksInYear(int year)
        {
            var jan1 = new DateTime(year, 1, 1);
            var isLeap = DateTime.IsLeapYear(year);

            if (jan1.DayOfWeek == DayOfWeek.Thursday) return 53;
            if (isLeap && jan1.DayOfWeek == DayOfWeek.Wednesday) return 53;
            return 52;
        }

        /// <summary>Nhãn hiển thị của một tuần, ví dụ "Tuần 30 (20/07 – 26/07)".</summary>
        public static string Label(int year, int week)
        {
            var from = FirstDayOfWeek(year, week);
            var to = LastDayOfWeek(year, week);
            return string.Format("Tuần {0} ({1:dd/MM} – {2:dd/MM})", week, from, to);
        }

        /// <summary>Nhãn đầy đủ kèm năm, dùng trên timeline.</summary>
        public static string LabelWithYear(int year, int week)
        {
            var from = FirstDayOfWeek(year, week);
            var to = LastDayOfWeek(year, week);
            return string.Format("Tuần {0}/{1} ({2:dd/MM} – {3:dd/MM/yyyy})", week, year, from, to);
        }

        /// <summary>Danh sách tuần của một năm để đổ dropdown.</summary>
        public static List<KeyValuePair<int, string>> WeekOptions(int year)
        {
            var options = new List<KeyValuePair<int, string>>();
            var total = WeeksInYear(year);

            for (var w = 1; w <= total; w++)
            {
                options.Add(new KeyValuePair<int, string>(w, Label(year, w)));
            }

            return options;
        }

        /// <summary>Các năm cho dropdown: quanh năm hiện tại, cộng thêm những năm đã có dữ liệu.</summary>
        public static List<int> YearOptions(IEnumerable<int> yearsInData)
        {
            var current = GetYear(DateTime.Today);
            var years = new SortedSet<int>();

            for (var y = current - 2; y <= current + 1; y++) years.Add(y);
            if (yearsInData != null)
            {
                foreach (var y in yearsInData) years.Add(y);
            }

            var list = new List<int>(years);
            list.Reverse();   // Năm mới nhất lên đầu
            return list;
        }

        /// <summary>Tuần ISO hiện tại.</summary>
        public static int CurrentWeek { get { return GetWeek(DateTime.Today); } }

        /// <summary>Năm ISO hiện tại.</summary>
        public static int CurrentYear { get { return GetYear(DateTime.Today); } }
    }
}
