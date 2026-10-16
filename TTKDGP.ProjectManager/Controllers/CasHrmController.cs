using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Newtonsoft.Json;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Cung cấp nội dung file JSON danh bạ nhân sự HRM do bot HRM đồng bộ.
    /// Đường dẫn công khai: /cashrm (brewtask.vnptkhanhhoa.vn/cashrm).
    /// </summary>
    public class CasHrmController : Controller
    {
        private static readonly object SyncLock = new object();

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index()
        {
            var filePath = HrmCasAutoLogin.CasHrmJsonFilePath();

            // 1. Nếu file JSON đã tồn tại, đọc và trả về ngay (nhanh, không tốn query CSDL)
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(filePath, Encoding.UTF8);
                    return Content(json, "application/json; charset=utf-8", Encoding.UTF8);
                }
                catch
                {
                    // Nếu lỗi khi đọc file, tiếp tục thử fallback lấy từ CSDL
                }
            }

            // 2. Nếu file chưa có (vừa publish chưa kịp chạy bot /sync), sinh từ CSDL và lưu vào file
            try
            {
                var employees = EmployeeHrmStore.GetAllForExport();
                if (employees != null && employees.Count > 0)
                {
                    lock (SyncLock)
                    {
                        if (!System.IO.File.Exists(filePath))
                        {
                            var exportData = employees.Select(e => new
                            {
                                id = e.EmployeeId,
                                vnpt_ma_nhan_vien = e.EmployeeCode,
                                name = e.FullName,
                                mobile_phone = e.MobilePhone,
                                work_email = e.WorkEmail,
                                department_id = new object[] { e.DepartmentId, e.DepartmentName },
                                department_code = e.DepartmentCode,
                                job_id = new object[] { e.JobId, e.JobName },
                                vitri_congviec = e.ViTriCongViecName,
                                vitri_congviec_code = e.ViTriCongViecCode,
                                birthday = e.Birthday.HasValue ? e.Birthday.Value.ToString("yyyy-MM-dd") : null,
                                gioi_tinh = e.GioiTinh,
                                is_congtacvien = e.IsCongTacVien
                            }).ToList();

                            var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                            var dir = Path.GetDirectoryName(filePath);
                            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                            System.IO.File.WriteAllText(filePath, json, Encoding.UTF8);
                            return Content(json, "application/json; charset=utf-8", Encoding.UTF8);
                        }
                    }
                }
            }
            catch
            {
                // Dự phòng lỗi CSDL
            }

            // 3. Nếu chưa có dữ liệu nào, trả về mảng rỗng
            return Content("[]", "application/json; charset=utf-8", Encoding.UTF8);
        }
    }
}
