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
    /// riêng cho từng cấp cha. Giải pháp: gom hết các cặp (id, đường dẫn tên) gặp được trong
    /// dữ liệu, rồi với mỗi đơn vị, đơn vị cha suy ra bằng cách CẮT BỚT ĐOẠN CUỐI của đường
    /// dẫn rồi TRA LẠI xem đường dẫn đó có khớp id nào khác đã gặp không — chỉ khớp được khi
    /// có ít nhất một nhân sự thuộc trực tiếp đơn vị cha đó (dữ liệu thật cho thấy điều này
    /// đúng: cả "Trung tâm Hạ tầng" lẫn tổ con "Tổ Tổng hợp" của nó đều có nhân sự riêng).
    /// "Viễn thông Khánh Hòa" (gốc, cấp Viễn thông tỉnh) không có id thật trong API nên được
    /// gán cứng DepartmentId = 0, DepartmentParentId = null theo đúng yêu cầu nghiệp vụ.
    /// </summary>
    public static class HrmDirectorySync
    {
        public const int RootDepartmentId = 0;
        public const string RootDepartmentName = "Viễn thông Khánh Hòa";

        private static readonly string[] PathSeparator = { " / " };

        public static void SaveRecords(JArray records, Action<string> notify)
        {
            if (records == null || records.Count == 0) return;

            int unresolvedParents;
            var departments = BuildDepartments(records, out unresolvedParents);
            var jobs = BuildJobs(records);
            var employees = BuildEmployees(records);

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

            var warn = unresolvedParents > 0
                ? string.Format(
                    " ⚠️ {0} đơn vị chưa suy được đơn vị cha (không có ai trực thuộc trực tiếp đơn vị cha đó trong dữ liệu lần này).",
                    unresolvedParents)
                : string.Empty;

            notify(string.Format(
                "💾 Đã lưu vào CSDL: {0} đơn vị, {1} chức danh, {2} nhân sự.{3}",
                deptSaved, jobSaved, empSaved, warn));
        }

        private static List<DepartmentHrm> BuildDepartments(JArray records, out int unresolvedParents)
        {
            // id đơn vị -> đường dẫn tên đầy đủ ("Viễn thông Khánh Hòa / Trung tâm Hạ tầng").
            var pathById = new Dictionary<int, string>();
            foreach (var record in records)
            {
                var dept = ReadRef(record["department_id"]);
                if (dept == null) continue;
                pathById[dept.Item1] = dept.Item2.Trim();
            }

            // Chiều ngược lại để tra "đường dẫn cha có id nào không". Nếu (hiếm) hai id trùng
            // tên đường dẫn thì giữ id gặp trước — không nên xảy ra với dữ liệu thật.
            var idByPath = new Dictionary<string, int>(StringComparer.Ordinal) { { RootDepartmentName, RootDepartmentId } };
            foreach (var kv in pathById)
            {
                if (!idByPath.ContainsKey(kv.Value)) idByPath[kv.Value] = kv.Key;
            }

            var result = new List<DepartmentHrm>
            {
                new DepartmentHrm { DepartmentId = RootDepartmentId, DepartmentName = RootDepartmentName, DepartmentParentId = null }
            };

            var unresolved = 0;
            foreach (var kv in pathById)
            {
                var segments = SplitPath(kv.Value);
                if (segments.Length == 0) continue;

                var name = segments[segments.Length - 1];
                int? parentId = null;

                if (segments.Length > 1)
                {
                    var parentPath = string.Join(" / ", segments.Take(segments.Length - 1));
                    int foundId;
                    if (idByPath.TryGetValue(parentPath, out foundId)) parentId = foundId;
                    else unresolved++;
                }

                result.Add(new DepartmentHrm { DepartmentId = kv.Key, DepartmentName = name, DepartmentParentId = parentId });
            }

            unresolvedParents = unresolved;
            return result;
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
