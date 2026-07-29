using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>Một trang tính: tên trang, hàng tiêu đề và các dòng dữ liệu.</summary>
    public class SheetData
    {
        public string Name { get; set; }
        public List<string> Headers { get; set; }
        public List<List<string>> Rows { get; set; }

        public SheetData(string name, params string[] headers)
        {
            Name = name;
            Headers = headers.ToList();
            Rows = new List<List<string>>();
        }

        public void Add(params object[] values)
        {
            Rows.Add(values.Select(v => v == null ? string.Empty : Convert.ToString(v)).ToList());
        }
    }

    /// <summary>
    /// Đọc và ghi bảng tính mà KHÔNG thêm thư viện ngoài — đúng nếp của dự án.
    ///
    /// Ghi: ra file .xlsx thật, xem <see cref="XlsxFile"/>.
    ///
    /// Đọc: nhận .xlsx, .csv, và cả SpreadsheetML 2003 (.xls dạng XML) mà bản trước từng phát ra —
    /// file người dùng đã tải về từ lâu vẫn phải dùng được.
    /// </summary>
    public static class SpreadsheetFile
    {
        private const string NsSs = "urn:schemas-microsoft-com:office:spreadsheet";

        /// <summary>Kiểu nội dung để trả file cho trình duyệt.</summary>
        public const string ContentType = XlsxFile.ContentType;

        /// <summary>Đuôi file của bảng tính do hệ thống phát ra.</summary>
        public const string Extension = ".xlsx";

        // ---------------- GHI ----------------

        /// <summary>Dựng file bảng tính nhiều trang, trả về nội dung dạng mảng byte.</summary>
        public static byte[] Build(params SheetData[] sheets)
        {
            return XlsxFile.Build(sheets);
        }

        // ---------------- ĐỌC ----------------

        /// <summary>Một trang tính đã đọc: tên trang và các dòng dữ liệu.</summary>
        public class SheetRecords
        {
            public string Name { get; set; }
            public List<Dictionary<string, string>> Rows { get; set; }

            public SheetRecords()
            {
                Rows = new List<Dictionary<string, string>>();
            }
        }

        /// <summary>
        /// Đọc TẤT CẢ trang tính trong file, giữ đúng thứ tự xuất hiện.
        ///
        /// File .csv chỉ chứa được một bảng nên luôn trả về đúng một trang — nơi gọi cần nhiều
        /// trang phải tự kiểm và báo cho người dùng biết là thiếu.
        /// </summary>
        public static List<SheetRecords> ReadAll(string fileName, Stream stream, out string error)
        {
            error = null;

            var ext = (Path.GetExtension(fileName) ?? string.Empty).ToLowerInvariant();

            byte[] content;
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                content = buffer.ToArray();
            }

            if (content.Length == 0)
            {
                error = "File rỗng.";
                return null;
            }

            try
            {
                // .xlsx là gói ZIP nên luôn mở đầu bằng "PK". Nhận theo nội dung chứ không theo đuôi
                // file: người dùng hay đổi tên .xlsx thành .xls rồi thắc mắc sao không đọc được.
                if (IsZip(content)) return ReadXlsx(content);

                if (ext == ".csv")
                {
                    return new List<SheetRecords>
                    {
                        new SheetRecords { Name = "Sheet1", Rows = ReadCsv(content) }
                    };
                }

                return ReadAllSheets(content, out error);
            }
            catch (Exception ex)
            {
                error = "Không đọc được file: " + ex.Message;
                return null;
            }
        }

        private static bool IsZip(byte[] content)
        {
            return content.Length > 1 && content[0] == 0x50 && content[1] == 0x4B;
        }

        private static List<SheetRecords> ReadXlsx(byte[] content)
        {
            var result = new List<SheetRecords>();

            foreach (var sheet in XlsxFile.Read(content))
            {
                var record = new SheetRecords { Name = sheet.Name };
                if (sheet.Rows.Count > 0)
                {
                    record.Rows = BuildRecords(sheet.Rows[0], sheet.Rows.Skip(1));
                }
                result.Add(record);
            }

            return result;
        }

        private static List<SheetRecords> ReadAllSheets(byte[] content, out string error)
        {
            error = null;

            var doc = XDocument.Parse(Encoding.UTF8.GetString(StripBom(content)));
            XNamespace ss = NsSs;

            var sheets = doc.Descendants(ss + "Worksheet").ToList();
            if (sheets.Count == 0)
            {
                error = "File không đúng định dạng bảng tính. Hãy dùng đúng file mẫu tải về.";
                return null;
            }

            var result = new List<SheetRecords>();

            foreach (var sheet in sheets)
            {
                var nameAttr = sheet.Attribute(ss + "Name");
                var table = sheet.Element(ss + "Table");

                var record = new SheetRecords
                {
                    Name = nameAttr == null ? ("Sheet" + (result.Count + 1)) : nameAttr.Value
                };

                if (table != null)
                {
                    var rows = table.Elements(ss + "Row").Select(ReadXmlRow).ToList();
                    if (rows.Count > 0) record.Rows = BuildRecords(rows[0], rows.Skip(1));
                }

                result.Add(record);
            }

            return result;
        }

        /// <summary>
        /// Đọc trang tính đầu tiên thành danh sách dòng, khoá là tiêu đề cột (đã chuẩn hoá về chữ
        /// thường, bỏ dấu cách). Trả null kèm <paramref name="error"/> nếu không đọc được.
        /// </summary>
        public static List<Dictionary<string, string>> Read(string fileName, Stream stream, out string error)
        {
            error = null;

            var ext = (Path.GetExtension(fileName) ?? string.Empty).ToLowerInvariant();

            byte[] content;
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                content = buffer.ToArray();
            }

            if (content.Length == 0)
            {
                error = "File rỗng.";
                return null;
            }

            try
            {
                if (IsZip(content))
                {
                    var sheets = ReadXlsx(content);
                    return sheets.Count == 0 ? new List<Dictionary<string, string>>() : sheets[0].Rows;
                }

                if (ext == ".csv") return ReadCsv(content);
                return ReadSpreadsheetXml(content, out error);
            }
            catch (Exception ex)
            {
                error = "Không đọc được file: " + ex.Message;
                return null;
            }
        }

        private static List<Dictionary<string, string>> ReadSpreadsheetXml(byte[] content, out string error)
        {
            error = null;

            var doc = XDocument.Parse(Encoding.UTF8.GetString(StripBom(content)));
            XNamespace ss = NsSs;

            var table = doc.Descendants(ss + "Table").FirstOrDefault();
            if (table == null)
            {
                error = "File không đúng định dạng bảng tính. Hãy dùng đúng file mẫu tải về.";
                return null;
            }

            var rows = table.Elements(ss + "Row").Select(ReadXmlRow).ToList();
            if (rows.Count == 0)
            {
                error = "File không có dòng nào.";
                return null;
            }

            return BuildRecords(rows[0], rows.Skip(1));
        }

        /// <summary>
        /// Đọc một hàng, tôn trọng thuộc tính ss:Index. Excel bỏ qua ô trống ở giữa và đánh số
        /// cột cho ô kế tiếp — không xử lý thì mọi giá trị sau ô trống đều lệch cột.
        /// </summary>
        private static List<string> ReadXmlRow(XElement row)
        {
            XNamespace ss = NsSs;
            var values = new List<string>();
            var column = 0;

            foreach (var cell in row.Elements(ss + "Cell"))
            {
                var indexAttr = cell.Attribute(ss + "Index");
                if (indexAttr != null)
                {
                    int index;
                    if (int.TryParse(indexAttr.Value, out index) && index > 0)
                    {
                        while (values.Count < index - 1) values.Add(string.Empty);
                        column = index - 1;
                    }
                }

                while (values.Count < column) values.Add(string.Empty);

                var data = cell.Element(ss + "Data");
                values.Add(data == null ? string.Empty : data.Value.Trim());
                column = values.Count;
            }

            return values;
        }

        private static List<Dictionary<string, string>> ReadCsv(byte[] content)
        {
            var text = Encoding.UTF8.GetString(StripBom(content));
            var rows = ParseCsv(text);
            if (rows.Count == 0) return new List<Dictionary<string, string>>();

            return BuildRecords(rows[0], rows.Skip(1));
        }

        /// <summary>
        /// Tách CSV có hỗ trợ ô đặt trong dấu nháy kép (nội dung có dấu phẩy hoặc xuống dòng).
        /// Tự nhận dấu phân cách là phẩy hay chấm phẩy — Excel bản tiếng Việt xuất ra chấm phẩy.
        /// </summary>
        private static List<List<string>> ParseCsv(string text)
        {
            var firstLine = text.Split('\n')[0];
            var separator = firstLine.Count(c => c == ';') > firstLine.Count(c => c == ',') ? ';' : ',';

            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Hai dấu nháy liền nhau là một dấu nháy thật trong nội dung.
                        if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(c);
                    continue;
                }

                if (c == '"') { inQuotes = true; }
                else if (c == separator) { row.Add(field.ToString().Trim()); field.Clear(); }
                else if (c == '\n')
                {
                    row.Add(field.ToString().Trim());
                    field.Clear();
                    if (row.Any(v => v.Length > 0)) rows.Add(row);
                    row = new List<string>();
                }
                else if (c != '\r') field.Append(c);
            }

            row.Add(field.ToString().Trim());
            if (row.Any(v => v.Length > 0)) rows.Add(row);

            return rows;
        }

        private static List<Dictionary<string, string>> BuildRecords(
            List<string> headers, IEnumerable<List<string>> dataRows)
        {
            var keys = headers.Select(NormalizeKey).ToList();
            var records = new List<Dictionary<string, string>>();

            foreach (var row in dataRows)
            {
                // Dòng trống hoàn toàn thì bỏ, không tính là lỗi.
                if (row.All(string.IsNullOrWhiteSpace)) continue;

                var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < keys.Count; i++)
                {
                    if (string.IsNullOrEmpty(keys[i])) continue;
                    record[keys[i]] = i < row.Count ? row[i] : string.Empty;
                }
                records.Add(record);
            }

            return records;
        }

        /// <summary>Chuẩn hoá tên cột: bỏ dấu cách và hạ chữ thường, để "Tên công việc" khớp "tencongviec".</summary>
        public static string NormalizeKey(string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return string.Empty;
            return new string(header.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        }

        private static byte[] StripBom(byte[] content)
        {
            if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
            {
                return content.Skip(3).ToArray();
            }
            return content;
        }
    }
}
