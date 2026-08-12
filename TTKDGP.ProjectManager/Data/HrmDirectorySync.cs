using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Data
{
    /// <summary>
    /// Chuyển mảng "records" thô từ API danh bạ HRM (vnpt.hr.danhba.view, xem
    /// HrmCasAutoLogin.TryFetchDanhBaAsync) thành DepartmentHrm/JobHrm/EmployeeHrm rồi lưu
    /// vào ba bảng department_hrm/job_hrm/employee_hrm.
    ///
    /// Điểm khó nhất là suy cây đơn vị: mỗi bản ghi nhân sự chỉ cho id của đơn vị SÂU NHẤT
    /// (vd 18942, "Viễn thông Khánh Hòa / Trung tâm Hạ tầng / Tổ Tổng hợp") — không có id
    /// riêng cho từng cấp cha. Giải pháp hai bước:
    ///
    ///   1. Gom hết các cặp (id, đường dẫn tên) gặp được trong dữ liệu — đây là các đơn vị
    ///      có nhân sự trực tiếp, id THẬT do API trả.
    ///   2. Với MỌI đường dẫn xuất hiện (kể cả các đoạn CHA chỉ được nhắc tới gián tiếp, ví dụ
    ///      "Trung tâm Kinh doanh Giải pháp" không ai trực thuộc trực tiếp mà chỉ các phòng/ban
    ///      con của nó mới có nhân sự) — đơn vị nào CHƯA có id thật thì gán id GIẢ, đánh số từ
    ///      <see cref="SyntheticDepartmentIdBase"/> + 1 trở đi, theo thứ tự cấp nông tới sâu rồi
    ///      bảng chữ cái (ổn định qua các lần đồng bộ). Nhờ vậy cây luôn nối liền trọn vẹn, không
    ///      còn đơn vị "mồ côi" như trước.
    ///
    /// "Viễn thông Khánh Hòa" (gốc, cấp Viễn thông tỉnh) không có id thật trong API nên được
    /// gán cứng DepartmentId = 0, DepartmentParentId = null theo đúng yêu cầu nghiệp vụ.
    /// </summary>
    public static class HrmDirectorySync
    {
        public const int RootDepartmentId = 0;
        public const string RootDepartmentName = "Viễn thông Khánh Hòa";

        /// <summary>Id giả bắt đầu từ đây + 1 (100001, 100002, ...) cho những đơn vị cấp trung
        /// gian không có id thật trong API — xem tóm tắt ở đầu lớp. Chọn 100000 vì id thật quan
        /// sát được trên dữ liệu hiện tại chỉ vài chục nghìn, còn xa mới chạm mốc này.</summary>
        private const int SyntheticDepartmentIdBase = 100000;

        private static readonly string[] PathSeparator = { " / " };

        public static void SaveRecords(JArray records, Action<string> notify)
        {
            if (records == null || records.Count == 0) return;

            List<string> unresolvedPaths;
            var departments = BuildDepartments(records, out unresolvedPaths);
            var jobs = BuildJobs(records);
            var employees = BuildEmployees(records);

            int matchedDepartments;
            MapCodesFromWorkplaces(departments, employees, out matchedDepartments);

            // Xoá sạch cả ba bảng trước khi nạp lại — danh bạ HRM là ảnh chụp hiện trạng, người
            // đã nghỉ/đơn vị đã giải thể không nên tồn lại từ lần đồng bộ trước. Xoá con trước
            // cha (dù chưa có ràng buộc FK) để nếu sau này thêm FK thì thứ tự vẫn đúng.
            EmployeeHrmStore.DeleteAll();
            JobHrmStore.DeleteAll();
            DepartmentHrmStore.DeleteAll();

            var deptSaved = DepartmentHrmStore.SaveMany(departments);
            var jobSaved = JobHrmStore.SaveMany(jobs);
            var empSaved = EmployeeHrmStore.SaveMany(employees);

            if (notify == null) return;

            var warn = unresolvedPaths.Count > 0
                ? string.Format(
                    " ⚠️ {0} đơn vị chưa suy được đơn vị cha (không có ai trực thuộc trực tiếp đơn vị cha đó trong dữ liệu lần này).",
                    unresolvedPaths.Count)
                : string.Empty;

            notify(string.Format(
                "💾 Đã lưu vào CSDL: {0} đơn vị, {1} chức danh, {2} nhân sự.{3}\n" +
                "🔗 Khớp mã đơn vị theo GoConnect: {4}/{0} đơn vị.",
                deptSaved, jobSaved, empSaved, warn, matchedDepartments));

            if (unresolvedPaths.Count == 0) return;

            var lines = new List<string>
            {
                string.Format("📋 Danh sách {0} đơn vị chưa suy được đơn vị cha, nhờ xem lại:", unresolvedPaths.Count)
            };
            var i = 0;
            foreach (var path in unresolvedPaths.OrderBy(p => p, StringComparer.Ordinal))
            {
                i++;
                lines.Add(string.Format("{0}. {1}", i, System.Web.HttpUtility.HtmlEncode(path)));
            }
            notify(string.Join("\n", lines));
        }

        /// <summary>
        /// Suy <see cref="DepartmentHrm.DepartmentCode"/>/<see cref="DepartmentHrm.DepartmentParentCode"/>
        /// bằng cách khớp TÊN đơn vị với <c>HrWorkplaces</c> (đơn vị đồng bộ từ GoConnect, đã có
        /// sẵn mã chuẩn và cây cha-con đúng). Đây là nguồn ĐỘC LẬP với cây nội bộ CAS
        /// (<see cref="DepartmentHrm.DepartmentParentId"/>) nên vẫn ra được mã cha kể cả với
        /// những đơn vị mà cây nội bộ chưa suy được cha (xem <paramref name="departments"/> có
        /// <c>DepartmentParentId</c> null nhưng không phải gốc).
        ///
        /// Trùng tên hiếm gặp thì giữ đơn vị GoConnect gặp trước — dữ liệu thật của một tỉnh
        /// không có hai đơn vị cùng tên.
        /// </summary>
        private static void MapCodesFromWorkplaces(
            List<DepartmentHrm> departments, List<EmployeeHrm> employees, out int matchedCount)
        {
            var workplaces = HrWorkplaceStore.All();

            var byName = new Dictionary<string, HrWorkplace>(StringComparer.OrdinalIgnoreCase);
            foreach (var w in workplaces)
            {
                if (w == null || string.IsNullOrWhiteSpace(w.WpName)) continue;
                var key = w.WpName.Trim();
                if (!byName.ContainsKey(key)) byName[key] = w;
            }

            var byId = new Dictionary<string, HrWorkplace>(StringComparer.OrdinalIgnoreCase);
            foreach (var w in workplaces)
            {
                if (w == null || string.IsNullOrWhiteSpace(w.WpId)) continue;
                var key = w.WpId.Trim();
                if (!byId.ContainsKey(key)) byId[key] = w;
            }

            var codeByDepartmentId = new Dictionary<int, string>();
            var matched = 0;

            foreach (var d in departments)
            {
                if (string.IsNullOrWhiteSpace(d.DepartmentName)) continue;

                HrWorkplace match;
                if (!byName.TryGetValue(d.DepartmentName.Trim(), out match)) continue;

                matched++;
                d.DepartmentCode = match.WpCode;
                codeByDepartmentId[d.DepartmentId] = match.WpCode;

                if (!string.IsNullOrWhiteSpace(match.WpParent))
                {
                    HrWorkplace parentWp;
                    if (byId.TryGetValue(match.WpParent.Trim(), out parentWp)) d.DepartmentParentCode = parentWp.WpCode;
                }
            }

            foreach (var e in employees)
            {
                if (!e.DepartmentId.HasValue) continue;

                string code;
                if (codeByDepartmentId.TryGetValue(e.DepartmentId.Value, out code)) e.DepartmentCode = code;
            }

            matchedCount = matched;
        }

        /// <summary>Đường dẫn tên đầy đủ ("Viễn thông Khánh Hòa / Trung tâm Hạ tầng / Tổ Y") của
        /// những đơn vị KHÔNG suy được đơn vị cha — chỉ còn xảy ra với đường dẫn rỗng/hỏng, giữ
        /// lại để báo ra ngoài phòng khi dữ liệu nguồn có bất thường.</summary>
        private static List<DepartmentHrm> BuildDepartments(JArray records, out List<string> unresolvedPaths)
        {
            // id THẬT (do API trả) -> đường dẫn tên đầy đủ ("Viễn thông Khánh Hòa / Trung tâm Hạ tầng").
            var pathById = new Dictionary<int, string>();
            foreach (var record in records)
            {
                var dept = ReadRef(record["department_id"]);
                if (dept == null) continue;
                pathById[dept.Item1] = dept.Item2.Trim();
            }

            // Chiều ngược lại để tra "đường dẫn này có id thật nào không". Nếu (hiếm) hai id trùng
            // tên đường dẫn thì giữ id gặp trước — không nên xảy ra với dữ liệu thật.
            var idByPath = new Dictionary<string, int>(StringComparer.Ordinal) { { RootDepartmentName, RootDepartmentId } };
            foreach (var kv in pathById)
            {
                if (!idByPath.ContainsKey(kv.Value)) idByPath[kv.Value] = kv.Key;
            }

            // MỌI đường dẫn xuất hiện, kể cả các đoạn CHA chỉ được nhắc tới gián tiếp (không ai
            // trực thuộc trực tiếp) — phải liệt kê đủ tổ tiên thì cây mới nối liền trọn vẹn.
            var allPaths = new HashSet<string>(StringComparer.Ordinal) { RootDepartmentName };
            foreach (var path in pathById.Values)
            {
                foreach (var ancestor in AncestorPathsIncludingSelf(path)) allPaths.Add(ancestor);
            }

            // Đường dẫn chưa có id thật thì gán id GIẢ — xử lý từ cấp NÔNG tới SÂU (rồi bảng chữ
            // cái để ổn định qua các lần đồng bộ) để lúc gán, đường dẫn cha luôn đã có id sẵn.
            var missingPaths = allPaths
                .Where(p => !idByPath.ContainsKey(p))
                .OrderBy(p => SplitPath(p).Length)
                .ThenBy(p => p, StringComparer.Ordinal)
                .ToList();

            var syntheticId = SyntheticDepartmentIdBase;
            foreach (var path in missingPaths) idByPath[path] = ++syntheticId;

            var result = new List<DepartmentHrm>
            {
                new DepartmentHrm { DepartmentId = RootDepartmentId, DepartmentName = RootDepartmentName, DepartmentParentId = null }
            };

            var unresolved = new List<string>();
            foreach (var path in allPaths)
            {
                if (string.Equals(path, RootDepartmentName, StringComparison.Ordinal)) continue;

                var segments = SplitPath(path);
                if (segments.Length == 0) continue;

                var name = segments[segments.Length - 1];
                var parentPath = segments.Length > 1
                    ? string.Join(" / ", segments.Take(segments.Length - 1))
                    : RootDepartmentName;

                int parentId;
                int? resolvedParentId = null;
                if (idByPath.TryGetValue(parentPath, out parentId)) resolvedParentId = parentId;
                else unresolved.Add(path);

                result.Add(new DepartmentHrm { DepartmentId = idByPath[path], DepartmentName = name, DepartmentParentId = resolvedParentId });
            }

            unresolvedPaths = unresolved;
            return result;
        }

        /// <summary>Mọi đường dẫn tổ tiên của <paramref name="path"/>, TÍNH CẢ chính nó, từ dài
        /// nhất tới ngắn nhất — vd "A / B / C" ra "A / B / C", "A / B", "A".</summary>
        private static IEnumerable<string> AncestorPathsIncludingSelf(string path)
        {
            var segments = SplitPath(path);
            for (var len = segments.Length; len >= 1; len--)
            {
                yield return string.Join(" / ", segments.Take(len));
            }
        }

        private static List<JobHrm> BuildJobs(JArray records)
        {
            var byId = new Dictionary<int, JobHrm>();

            foreach (var record in records)
            {
                var job = ReadRef(record["job_id"]);
                if (job == null || byId.ContainsKey(job.Item1)) continue;

                string code;
                var name = SplitCodeName(job.Item2, out code);
                byId[job.Item1] = new JobHrm { JobId = job.Item1, JobCode = code, JobName = name };
            }

            return byId.Values.ToList();
        }

        private static List<EmployeeHrm> BuildEmployees(JArray records)
        {
            var result = new List<EmployeeHrm>();

            foreach (var record in records)
            {
                var idToken = record["id"];
                if (idToken == null || idToken.Type != JTokenType.Integer) continue;

                var dept = ReadRef(record["department_id"]);
                var job = ReadRef(record["job_id"]);
                var vtcv = ReadRef(record["vitri_congviec"]);
                var vtcvCode = AsString(record["vitri_congviec_code"]);

                string vtcvName = null;
                if (vtcv != null)
                {
                    vtcvName = vtcv.Item2.Trim();
                    if (!string.IsNullOrEmpty(vtcvCode))
                    {
                        var prefix = vtcvCode.Trim() + " - ";
                        if (vtcvName.StartsWith(prefix, StringComparison.Ordinal))
                            vtcvName = vtcvName.Substring(prefix.Length).Trim();
                    }
                }

                result.Add(new EmployeeHrm
                {
                    EmployeeId = idToken.Value<int>(),
                    EmployeeCode = AsString(record["vnpt_ma_nhan_vien"]),
                    FullName = AsString(record["name"]),
                    MobilePhone = AsString(record["mobile_phone"]),
                    WorkEmail = AsString(record["work_email"]),
                    DepartmentId = dept != null ? dept.Item1 : (int?)null,
                    JobId = job != null ? job.Item1 : (int?)null,
                    ViTriCongViecId = vtcv != null ? vtcv.Item1 : (int?)null,
                    ViTriCongViecName = vtcvName,
                    ViTriCongViecCode = vtcvCode,
                    Birthday = AsDate(record["birthday"]),
                    GioiTinh = AsString(record["gioi_tinh"]),
                    IsCongTacVien = record["is_congtacvien"] != null && record["is_congtacvien"].Type == JTokenType.Boolean
                        && record["is_congtacvien"].Value<bool>()
                });
            }

            return result;
        }

        private static string[] SplitPath(string path)
        {
            return path.Split(PathSeparator, StringSplitOptions.None)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();
        }

        /// <summary>Tách "MÃ - Tên" thành mã (ra tham số) và tên (giá trị trả về). Không tách
        /// được thì mã để trống, tên giữ nguyên cả chuỗi.</summary>
        private static string SplitCodeName(string raw, out string code)
        {
            var text = (raw ?? string.Empty).Trim();
            var idx = text.IndexOf(" - ", StringComparison.Ordinal);
            if (idx <= 0)
            {
                code = null;
                return text;
            }

            code = text.Substring(0, idx).Trim();
            return text.Substring(idx + 3).Trim();
        }

        /// <summary>Odoo trả many2one dạng [id, "tên"], hoặc false khi trống. Trả null khi trống.</summary>
        private static Tuple<int, string> ReadRef(JToken token)
        {
            var arr = token as JArray;
            if (arr == null || arr.Count < 2) return null;
            return Tuple.Create(arr[0].Value<int>(), arr[1].Value<string>() ?? string.Empty);
        }

        private static string AsString(JToken token)
        {
            return token != null && token.Type == JTokenType.String ? token.Value<string>() : null;
        }

        private static DateTime? AsDate(JToken token)
        {
            if (token == null || token.Type != JTokenType.String) return null;
            DateTime parsed;
            return DateTime.TryParse(token.Value<string>(), out parsed) ? parsed : (DateTime?)null;
        }
    }
}
