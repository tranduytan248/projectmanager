using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Ghi và đọc file .xlsx thật, không dùng thư viện ngoài — đúng nếp của dự án.
    ///
    /// .xlsx là một gói ZIP chứa vài file XML. Ở đây chỉ dựng đúng phần tối thiểu Excel cần:
    /// bảng kê kiểu nội dung, hai file quan hệ, workbook, styles và mỗi trang tính một file.
    ///
    /// Toàn bộ ô ghi ra dưới dạng chuỗi lồng trong ô (inlineStr) và cột được đặt định dạng "văn
    /// bản". Làm vậy vì file này là FILE MẪU để người dùng điền tay: nếu để định dạng chung, Excel
    /// sẽ tự nuốt "01/07/2026" thành một con số nội bộ và mã nhân viên "0012" thành "12".
    /// </summary>
    public static class XlsxFile
    {
        public const string ContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private static readonly XNamespace Main =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelNs =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PkgRelNs =
            "http://schemas.openxmlformats.org/package/2006/relationships";
        private static readonly XNamespace ContentTypesNs =
            "http://schemas.openxmlformats.org/package/2006/content-types";

        /// <summary>
        /// Tiền tố của thuộc tính Type trong file quan hệ. Phải ghép chuỗi bằng tay: đổ thẳng một
        /// XName vào thuộc tính sẽ ra dạng "{namespace}tên", và Excel từ chối mở gói.
        /// </summary>
        private const string RelType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/";

        /// <summary>Chỉ số kiểu ô trong styles.xml bên dưới.</summary>
        private const int StyleNormal = 0;
        private const int StyleHeader = 1;
        private const int StyleText = 2;

        // ---------------- GHI ----------------

        public static byte[] Build(params SheetData[] sheets)
        {
            if (sheets == null || sheets.Length == 0) sheets = new[] { new SheetData("Sheet1") };

            using (var buffer = new MemoryStream())
            {
                // Để ngỏ luồng sau khi đóng gói, nếu không ToArray() sẽ ném lỗi.
                using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
                {
                    AddEntry(zip, "[Content_Types].xml", ContentTypesXml(sheets.Length));
                    AddEntry(zip, "_rels/.rels", RootRelsXml());
                    AddEntry(zip, "xl/workbook.xml", WorkbookXml(sheets));
                    AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml(sheets.Length));
                    AddEntry(zip, "xl/styles.xml", StylesXml());

                    for (var i = 0; i < sheets.Length; i++)
                    {
                        AddEntry(zip, string.Format("xl/worksheets/sheet{0}.xml", i + 1),
                            SheetXml(sheets[i]));
                    }
                }

                return buffer.ToArray();
            }
        }

        private static void AddEntry(ZipArchive zip, string path, XDocument doc)
        {
            // Mỗi phần trong gói phải mở đầu bằng khai báo XML, nếu không một số bản Excel coi là
            // gói hỏng và đòi sửa file trước khi mở.
            doc.Declaration = new XDeclaration("1.0", "UTF-8", "yes");

            var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                doc.Save(writer, SaveOptions.DisableFormatting);
            }
        }

        private static XDocument ContentTypesXml(int sheetCount)
        {
            var root = new XElement(ContentTypesNs + "Types",
                new XElement(ContentTypesNs + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType",
                        "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(ContentTypesNs + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                new XElement(ContentTypesNs + "Override",
                    new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute("ContentType",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(ContentTypesNs + "Override",
                    new XAttribute("PartName", "/xl/styles.xml"),
                    new XAttribute("ContentType",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")));

            for (var i = 1; i <= sheetCount; i++)
            {
                root.Add(new XElement(ContentTypesNs + "Override",
                    new XAttribute("PartName", string.Format("/xl/worksheets/sheet{0}.xml", i)),
                    new XAttribute("ContentType",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
            }

            return new XDocument(root);
        }

        private static XDocument RootRelsXml()
        {
            return new XDocument(
                new XElement(PkgRelNs + "Relationships",
                    new XElement(PkgRelNs + "Relationship",
                        new XAttribute("Id", "rId1"),
                        new XAttribute("Type", RelType + "officeDocument"),
                        new XAttribute("Target", "xl/workbook.xml"))));
        }

        private static XDocument WorkbookXml(SheetData[] sheets)
        {
            var list = new XElement(Main + "sheets");

            for (var i = 0; i < sheets.Length; i++)
            {
                list.Add(new XElement(Main + "sheet",
                    new XAttribute("name", SafeSheetName(sheets[i].Name, i)),
                    new XAttribute("sheetId", i + 1),
                    new XAttribute(RelNs + "id", "rId" + (i + 1))));
            }

            return new XDocument(
                new XElement(Main + "workbook",
                    new XAttribute(XNamespace.Xmlns + "r", RelNs.NamespaceName),
                    list));
        }

        private static XDocument WorkbookRelsXml(int sheetCount)
        {
            var root = new XElement(PkgRelNs + "Relationships");

            for (var i = 1; i <= sheetCount; i++)
            {
                root.Add(new XElement(PkgRelNs + "Relationship",
                    new XAttribute("Id", "rId" + i),
                    new XAttribute("Type", RelType + "worksheet"),
                    new XAttribute("Target", string.Format("worksheets/sheet{0}.xml", i))));
            }

            root.Add(new XElement(PkgRelNs + "Relationship",
                new XAttribute("Id", "rId" + (sheetCount + 1)),
                new XAttribute("Type", RelType + "styles"),
                new XAttribute("Target", "styles.xml")));

            return new XDocument(root);
        }

        /// <summary>
        /// Bảng kiểu tối thiểu. Thứ tự các mục trong cellXfs chính là chỉ số dùng ở thuộc tính s
        /// của từng ô, xem <see cref="StyleHeader"/> và <see cref="StyleText"/>.
        /// </summary>
        private static XDocument StylesXml()
        {
            return new XDocument(
                new XElement(Main + "styleSheet",
                    new XElement(Main + "numFmts",
                        new XAttribute("count", 1),
                        new XElement(Main + "numFmt",
                            new XAttribute("numFmtId", 164),
                            new XAttribute("formatCode", "@"))),
                    new XElement(Main + "fonts",
                        new XAttribute("count", 2),
                        new XElement(Main + "font",
                            new XElement(Main + "sz", new XAttribute("val", 11)),
                            new XElement(Main + "name", new XAttribute("val", "Calibri"))),
                        new XElement(Main + "font",
                            new XElement(Main + "b"),
                            new XElement(Main + "sz", new XAttribute("val", 11)),
                            new XElement(Main + "name", new XAttribute("val", "Calibri")))),
                    new XElement(Main + "fills",
                        new XAttribute("count", 3),
                        new XElement(Main + "fill",
                            new XElement(Main + "patternFill", new XAttribute("patternType", "none"))),
                        new XElement(Main + "fill",
                            new XElement(Main + "patternFill", new XAttribute("patternType", "gray125"))),
                        new XElement(Main + "fill",
                            new XElement(Main + "patternFill",
                                new XAttribute("patternType", "solid"),
                                new XElement(Main + "fgColor", new XAttribute("rgb", "FFDCE6F1")),
                                new XElement(Main + "bgColor", new XAttribute("indexed", 64))))),
                    new XElement(Main + "borders",
                        new XAttribute("count", 1),
                        new XElement(Main + "border",
                            new XElement(Main + "left"), new XElement(Main + "right"),
                            new XElement(Main + "top"), new XElement(Main + "bottom"),
                            new XElement(Main + "diagonal"))),
                    new XElement(Main + "cellStyleXfs",
                        new XAttribute("count", 1),
                        Xf(0, 0, 0)),
                    new XElement(Main + "cellXfs",
                        new XAttribute("count", 3),
                        Xf(0, 0, 0),                                        // 0 — thường
                        Xf(0, 1, 2, applyFont: true, applyFill: true),      // 1 — tiêu đề
                        Xf(164, 0, 0, applyNumberFormat: true))));          // 2 — văn bản
        }

        private static XElement Xf(int numFmtId, int fontId, int fillId,
            bool applyFont = false, bool applyFill = false, bool applyNumberFormat = false)
        {
            var xf = new XElement(Main + "xf",
                new XAttribute("numFmtId", numFmtId),
                new XAttribute("fontId", fontId),
                new XAttribute("fillId", fillId),
                new XAttribute("borderId", 0),
                new XAttribute("xfId", 0));

            if (applyFont) xf.Add(new XAttribute("applyFont", 1));
            if (applyFill) xf.Add(new XAttribute("applyFill", 1));
            if (applyNumberFormat) xf.Add(new XAttribute("applyNumberFormat", 1));

            return xf;
        }

        private static XDocument SheetXml(SheetData sheet)
        {
            var data = new XElement(Main + "sheetData");
            var rowIndex = 1;

            data.Add(BuildRow(rowIndex++, sheet.Headers, StyleHeader));
            foreach (var row in sheet.Rows) data.Add(BuildRow(rowIndex++, row, StyleText));

            var columns = Math.Max(sheet.Headers.Count, 1);
            var worksheet = new XElement(Main + "worksheet",
                new XElement(Main + "cols",
                    new XElement(Main + "col",
                        new XAttribute("min", 1),
                        new XAttribute("max", columns),
                        new XAttribute("width", 22),
                        new XAttribute("customWidth", 1),
                        new XAttribute("style", StyleText))),
                data);

            return new XDocument(worksheet);
        }

        private static XElement BuildRow(int rowIndex, List<string> values, int styleIndex)
        {
            var row = new XElement(Main + "row", new XAttribute("r", rowIndex));

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i] ?? string.Empty;

                var cell = new XElement(Main + "c",
                    new XAttribute("r", ColumnName(i) + rowIndex),
                    new XAttribute("s", styleIndex));

                // Ô rỗng chỉ cần giữ chỗ; ghi thêm <is> rỗng làm Excel kêu file hỏng.
                if (value.Length > 0)
                {
                    cell.Add(new XAttribute("t", "inlineStr"),
                        new XElement(Main + "is",
                            new XElement(Main + "t",
                                new XAttribute(XNamespace.Xml + "space", "preserve"), value)));
                }

                row.Add(cell);
            }

            return row;
        }

        /// <summary>Số cột (bắt đầu từ 0) thành tên cột kiểu Excel: 0 → A, 26 → AA.</summary>
        private static string ColumnName(int index)
        {
            var name = string.Empty;
            var n = index + 1;

            while (n > 0)
            {
                var remainder = (n - 1) % 26;
                name = (char)('A' + remainder) + name;
                n = (n - 1) / 26;
            }

            return name;
        }

        private static string SafeSheetName(string name, int position)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Sheet" + (position + 1);

            var cleaned = new string(name.Where(c => ":\\/?*[]".IndexOf(c) < 0).ToArray()).Trim();
            if (cleaned.Length == 0) return "Sheet" + (position + 1);
            return cleaned.Length > 31 ? cleaned.Substring(0, 31) : cleaned;
        }

        // ---------------- ĐỌC ----------------

        /// <summary>Nội dung một trang tính đã đọc: tên trang và các dòng ô dạng chuỗi.</summary>
        public class Sheet
        {
            public string Name { get; set; }
            public List<List<string>> Rows { get; set; }

            public Sheet()
            {
                Rows = new List<List<string>>();
            }
        }

        /// <summary>
        /// Đọc mọi trang tính trong file .xlsx, giữ đúng thứ tự trong workbook.
        /// Ném lỗi nếu file hỏng — nơi gọi tự bắt và diễn giải cho người dùng.
        /// </summary>
        public static List<Sheet> Read(byte[] content)
        {
            using (var buffer = new MemoryStream(content, false))
            using (var zip = new ZipArchive(buffer, ZipArchiveMode.Read))
            {
                var shared = ReadSharedStrings(zip);
                var dateStyles = ReadDateStyles(zip);
                var targets = ReadWorkbookRels(zip);

                var workbook = ReadXml(zip, "xl/workbook.xml");
                if (workbook == null) throw new InvalidDataException("Không tìm thấy workbook.xml.");

                var result = new List<Sheet>();
                var position = 0;

                foreach (var node in workbook.Descendants(Main + "sheet"))
                {
                    position++;

                    var nameAttr = node.Attribute("name");
                    var idAttr = node.Attribute(RelNs + "id");

                    string target = null;
                    if (idAttr != null) targets.TryGetValue(idAttr.Value, out target);
                    if (string.IsNullOrEmpty(target))
                    {
                        target = string.Format("worksheets/sheet{0}.xml", position);
                    }

                    var sheet = new Sheet
                    {
                        Name = nameAttr == null ? ("Sheet" + position) : nameAttr.Value
                    };

                    var doc = ReadXml(zip, CombinePart("xl", target));
                    if (doc != null) sheet.Rows = ReadRows(doc, shared, dateStyles);

                    result.Add(sheet);
                }

                return result;
            }
        }

        private static List<List<string>> ReadRows(XDocument sheetDoc, List<string> shared,
            HashSet<int> dateStyles)
        {
            var rows = new List<List<string>>();

            foreach (var rowNode in sheetDoc.Descendants(Main + "row"))
            {
                var values = new List<string>();

                foreach (var cell in rowNode.Elements(Main + "c"))
                {
                    // Excel bỏ hẳn ô trống khỏi file; thiếu bù theo tham chiếu ô thì mọi giá trị
                    // phía sau đều lệch cột.
                    var refAttr = cell.Attribute("r");
                    if (refAttr != null)
                    {
                        var column = ColumnIndex(refAttr.Value);
                        while (values.Count < column) values.Add(string.Empty);
                    }

                    values.Add(CellText(cell, shared, dateStyles));
                }

                rows.Add(values);
            }

            return rows;
        }

        private static string CellText(XElement cell, List<string> shared, HashSet<int> dateStyles)
        {
            var type = cell.Attribute("t");
            var kind = type == null ? "n" : type.Value;

            if (kind == "inlineStr")
            {
                var inline = cell.Element(Main + "is");
                return inline == null ? string.Empty : PlainText(inline).Trim();
            }

            var valueNode = cell.Element(Main + "v");
            if (valueNode == null) return string.Empty;
            var raw = valueNode.Value;

            if (kind == "s")
            {
                int index;
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out index)
                    && index >= 0 && index < shared.Count)
                {
                    return shared[index].Trim();
                }
                return string.Empty;
            }

            if (kind == "str") return raw.Trim();
            if (kind == "b") return raw == "1" ? "TRUE" : "FALSE";

            // Ô ngày tháng được Excel lưu là SỐ; không đổi lại thì người dùng thấy "46204" thay vì
            // ngày họ vừa gõ, và mọi dòng đều báo sai định dạng.
            var styleAttr = cell.Attribute("s");
            if (styleAttr != null && dateStyles.Count > 0)
            {
                int styleIndex;
                double serial;
                if (int.TryParse(styleAttr.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out styleIndex)
                    && dateStyles.Contains(styleIndex)
                    && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out serial)
                    && serial > 0 && serial < 2958466)
                {
                    return DateTime.FromOADate(serial).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
            }

            return raw.Trim();
        }

        /// <summary>Gộp mọi đoạn chữ trong một ô — chuỗi có định dạng bị Excel cắt thành nhiều đoạn.</summary>
        private static string PlainText(XElement container)
        {
            return string.Concat(container.Descendants(Main + "t").Select(t => t.Value));
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var doc = ReadXml(zip, "xl/sharedStrings.xml");
            if (doc == null) return new List<string>();

            return doc.Descendants(Main + "si").Select(PlainText).ToList();
        }

        /// <summary>
        /// Tập chỉ số kiểu ô đang dùng định dạng ngày tháng. Gồm các mã dựng sẵn của Excel và các
        /// mã tự đặt có chứa ký tự ngày/tháng/năm.
        /// </summary>
        private static HashSet<int> ReadDateStyles(ZipArchive zip)
        {
            var result = new HashSet<int>();

            var doc = ReadXml(zip, "xl/styles.xml");
            if (doc == null) return result;

            var builtIn = new HashSet<int>(new[] { 14, 15, 16, 17, 18, 19, 20, 21, 22, 45, 46, 47 });
            var custom = new HashSet<int>();

            foreach (var fmt in doc.Descendants(Main + "numFmt"))
            {
                var idAttr = fmt.Attribute("numFmtId");
                var codeAttr = fmt.Attribute("formatCode");
                if (idAttr == null || codeAttr == null) continue;

                int id;
                if (!int.TryParse(idAttr.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)) continue;

                var code = codeAttr.Value;
                // Bỏ phần trong dấu nháy để chữ trong nhãn không bị nhầm là ký tự định dạng.
                var outside = string.Join(string.Empty, code.Split('"').Where((s, i) => i % 2 == 0));
                if (outside.IndexOfAny(new[] { 'y', 'Y', 'd', 'D' }) >= 0) custom.Add(id);
            }

            var cellXfs = doc.Descendants(Main + "cellXfs").FirstOrDefault();
            if (cellXfs == null) return result;

            var index = 0;
            foreach (var xf in cellXfs.Elements(Main + "xf"))
            {
                var idAttr = xf.Attribute("numFmtId");
                int id;
                if (idAttr != null
                    && int.TryParse(idAttr.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                    && (builtIn.Contains(id) || custom.Contains(id)))
                {
                    result.Add(index);
                }
                index++;
            }

            return result;
        }

        private static Dictionary<string, string> ReadWorkbookRels(ZipArchive zip)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var doc = ReadXml(zip, "xl/_rels/workbook.xml.rels");
            if (doc == null) return map;

            foreach (var rel in doc.Descendants(PkgRelNs + "Relationship"))
            {
                var id = rel.Attribute("Id");
                var target = rel.Attribute("Target");
                if (id != null && target != null) map[id.Value] = target.Value;
            }

            return map;
        }

        /// <summary>Ghép đường dẫn phần tử; Target có thể là tương đối hoặc bắt đầu bằng "/".</summary>
        private static string CombinePart(string baseFolder, string target)
        {
            var cleaned = target.Replace('\\', '/');
            if (cleaned.StartsWith("/")) return cleaned.TrimStart('/');
            return baseFolder + "/" + cleaned;
        }

        private static XDocument ReadXml(ZipArchive zip, string path)
        {
            // Tên phần tử trong gói phân biệt hoa thường theo chuẩn, nhưng một số công cụ ghi khác
            // kiểu chữ; dò thêm một lượt không phân biệt hoa thường cho chắc.
            var entry = zip.GetEntry(path)
                ?? zip.Entries.FirstOrDefault(e =>
                    string.Equals(e.FullName, path, StringComparison.OrdinalIgnoreCase));
            if (entry == null) return null;

            using (var stream = entry.Open())
            {
                return XDocument.Load(stream);
            }
        }

        private static int ColumnIndex(string cellRef)
        {
            var index = 0;

            foreach (var c in cellRef)
            {
                if (c >= 'A' && c <= 'Z') index = index * 26 + (c - 'A' + 1);
                else if (c >= 'a' && c <= 'z') index = index * 26 + (c - 'a' + 1);
                else break;
            }

            return index - 1;
        }
    }
}
