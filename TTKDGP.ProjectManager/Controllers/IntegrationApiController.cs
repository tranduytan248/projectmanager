using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// API cho hệ thống đối tác lấy dữ liệu HRM (nhân sự, đơn vị, chức danh). Không đăng nhập;
    /// xác thực bằng checksum SHA-256 tính từ các trường bản tin cộng khoá bí mật của hệ thống
    /// (cấu hình ở màn Hệ thống tích hợp). Gọi bằng POST, thân là JSON.
    ///
    /// Endpoint: POST /api/hrm
    /// </summary>
    public class IntegrationApiController : Controller
    {
        /// <summary>Số dòng mỗi trang mặc định và trần tối đa, để một lần gọi không kéo cả bảng.</summary>
        private const int DefaultPageSize = 100;
        private const int MaxPageSize = 500;

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Hrm()
        {
            HrmApiRequest req = null;
            try
            {
                req = ReadJsonBody();

                if (req == null
                    || string.IsNullOrWhiteSpace(req.PartnerCode)
                    || string.IsNullOrWhiteSpace(req.RequestId)
                    || string.IsNullOrWhiteSpace(req.RequestTime)
                    || string.IsNullOrWhiteSpace(req.Resource)
                    || string.IsNullOrWhiteSpace(req.Checksum))
                {
                    return Json(HrmApiResponse.Error(HrmApiResponse.Codes.BadRequest,
                        "Thiếu trường bắt buộc (partnerCode, requestId, requestTime, resource, checksum).", req));
                }

                // Tìm hệ thống theo mã; phải đang hoạt động mới được gọi.
                var system = Repository.IntegrationSystems.FirstOrDefault(x =>
                    string.Equals(x.Code, req.PartnerCode.Trim(), StringComparison.OrdinalIgnoreCase));

                if (system == null || !system.IsActive)
                {
                    return Json(HrmApiResponse.Error(HrmApiResponse.Codes.PartnerNotAllowed,
                        "Hệ thống không tồn tại hoặc đang bị khoá.", req));
                }

                // Xác thực checksum: tính lại từ các trường bản tin + khoá bí mật, so với giá trị gửi lên.
                if (!IntegrationKey.Verify(req.Checksum, system.PrivateKey, req.ChecksumFields()))
                {
                    return Json(HrmApiResponse.Error(HrmApiResponse.Codes.InvalidChecksum,
                        "Checksum không hợp lệ.", req));
                }

                var page = req.Page.HasValue && req.Page.Value > 0 ? req.Page.Value : 1;
                var pageSize = req.PageSize.HasValue && req.PageSize.Value > 0 ? req.PageSize.Value : DefaultPageSize;
                if (pageSize > MaxPageSize) pageSize = MaxPageSize;

                var response = BuildData(req, system, page, pageSize);
                if (response == null)
                {
                    return Json(HrmApiResponse.Error(HrmApiResponse.Codes.BadRequest,
                        "Loại dữ liệu (resource) không hợp lệ. Chỉ nhận: employees, workplaces, positions.", req));
                }

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(HrmApiResponse.Error(HrmApiResponse.Codes.ServerError,
                    "Lỗi hệ thống: " + ex.Message, req));
            }
        }

        /// <summary>Lấy đúng một trang dữ liệu theo resource, chuyển sang bản ghi gọn cho đối tác.</summary>
        private HrmApiResponse BuildData(HrmApiRequest req, IntegrationSystem system, int page, int pageSize)
        {
            var resource = req.Resource.Trim().ToLowerInvariant();

            int totalItems, totalPages, actualPage;
            object data;

            switch (resource)
            {
                case "employees":
                    {
                        var paged = HrEmployeeStore.Page(page, pageSize, req.Keyword);
                        data = paged.Items.Select(e => new
                        {
                            code = e.Code,
                            fullName = e.FullName,
                            email = e.Email,
                            phoneNumber = e.PhoneNumber,
                            gender = e.Gender,
                            departmentCode = e.DepartmentCode,
                            department = e.DepartmentName,
                            positionCode = e.PositionCode,
                            position = e.PositionName
                        }).ToList();
                        totalItems = paged.Pager.TotalItems;
                        totalPages = paged.Pager.TotalPages;
                        actualPage = paged.Pager.Page;
                        break;
                    }

                case "workplaces":
                    {
                        var paged = HrWorkplaceStore.Page(page, pageSize, req.Keyword);
                        data = paged.Items.Select(w => new
                        {
                            wpId = w.WpId,
                            code = w.WpCode,
                            name = w.WpName,
                            parentId = w.WpParent,
                            level = w.Level,
                            employeeCount = w.EmployeeCount
                        }).ToList();
                        totalItems = paged.Pager.TotalItems;
                        totalPages = paged.Pager.TotalPages;
                        actualPage = paged.Pager.Page;
                        break;
                    }

                case "positions":
                    {
                        var paged = HrPositionStore.Page(page, pageSize, req.Keyword);
                        data = paged.Items.Select(p => new
                        {
                            posId = p.PosId,
                            code = p.PosCode,
                            name = p.PosName,
                            employeeCount = p.EmployeeCount
                        }).ToList();
                        totalItems = paged.Pager.TotalItems;
                        totalPages = paged.Pager.TotalPages;
                        actualPage = paged.Pager.Page;
                        break;
                    }

                default:
                    return null;
            }

            var response = new HrmApiResponse
            {
                Code = HrmApiResponse.Codes.Success,
                Message = "Thành công.",
                RequestId = req.RequestId,
                Resource = resource,
                Page = actualPage,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Data = data
            };

            // Ký phản hồi để đối tác kiểm tính toàn vẹn phần khung (mã, số lượng, phân trang).
            response.Checksum = IntegrationKey.Sign(system.PrivateKey,
                response.Code, response.Message, response.RequestId, response.Resource,
                response.Page.ToString(), response.PageSize.ToString(), response.TotalItems.ToString());

            return response;
        }

        /// <summary>
        /// API cho hệ thống đối tác lấy dữ liệu danh bạ CAS VNPT (HRM): nhân sự, đơn vị, chức
        /// danh lấy qua lệnh "/signin" trên bot Telegram (xem <see cref="HrmCasAutoLogin"/> và
        /// <see cref="HrmDirectorySync"/>). HOÀN TOÀN KHÁC nguồn dữ liệu của <see cref="Hrm"/> ở
        /// trên (đó là dữ liệu đồng bộ từ GoConnect) — dùng chung cơ chế checksum và bảng hệ
        /// thống tích hợp, chỉ khác <c>resource</c> và tầng dữ liệu bên dưới.
        ///
        /// Endpoint: POST /api/hrmcas
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult HrmCas()
        {
            HrmApiRequest req = null;
            try
            {
                req = ReadJsonBody();

                if (req == null
                    || string.IsNullOrWhiteSpace(req.PartnerCode)
                    || string.IsNullOrWhiteSpace(req.RequestId)
                    || string.IsNullOrWhiteSpace(req.RequestTime)
                    || string.IsNullOrWhiteSpace(req.Resource)
                    || string.IsNullOrWhiteSpace(req.Checksum))
                {
                    return Json(HrmApiResponse.Error(HrmApiResponse.Codes.BadRequest,
                        "Thiếu trường bắt buộc (partnerCode, requestId, requestTime, resource, checksum).", req));
                }

                var system = Repository.IntegrationSystems.FirstOrDefault(x =>
                    string.Equals(x.Code, req.PartnerCode.Trim(), StringComparison.OrdinalIgnoreCase));

                if (system == null || !system.IsActive)
                {
                    return Json(HrmApiResponse.Error(HrmApiResponse.Codes.PartnerNotAllowed,
                        "Hệ thống không tồn tại hoặc đang bị khoá.", req));
                }

                if (!IntegrationKey.Verify(req.Checksum, system.PrivateKey, req.ChecksumFields()))
                {
                    return Json(HrmApiResponse.Error(HrmApiResponse.Codes.InvalidChecksum,
                        "Checksum không hợp lệ.", req));
                }

                var page = req.Page.HasValue && req.Page.Value > 0 ? req.Page.Value : 1;
                var pageSize = req.PageSize.HasValue && req.PageSize.Value > 0 ? req.PageSize.Value : DefaultPageSize;
                if (pageSize > MaxPageSize) pageSize = MaxPageSize;

                var response = BuildDataCas(req, system, page, pageSize);
                if (response == null)
                {
                    return Json(HrmApiResponse.Error(HrmApiResponse.Codes.BadRequest,
                        "Loại dữ liệu (resource) không hợp lệ. Chỉ nhận: employees, departments, jobs.", req));
                }

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(HrmApiResponse.Error(HrmApiResponse.Codes.ServerError,
                    "Lỗi hệ thống: " + ex.Message, req));
            }
        }

        /// <summary>Lấy đúng một trang dữ liệu CAS HRM theo resource, chuyển sang bản ghi gọn cho đối tác.</summary>
        private HrmApiResponse BuildDataCas(HrmApiRequest req, IntegrationSystem system, int page, int pageSize)
        {
            var resource = req.Resource.Trim().ToLowerInvariant();

            int totalItems, totalPages, actualPage;
            object data;

            switch (resource)
            {
                case "employees":
                    {
                        var paged = EmployeeHrmStore.Page(page, pageSize, req.Keyword);

                        // department_hrm.department_name của EmployeeHrmStore.Page chỉ là tên cấp
                        // sâu nhất — ghép thành đường dẫn đầy đủ từ gốc, giống màn HrmDirectory.
                        var deptById = DepartmentHrmStore.All().ToDictionary(d => d.DepartmentId);

                        data = paged.Items.Select(e => new
                        {
                            code = e.EmployeeCode,
                            fullName = e.FullName,
                            phoneNumber = e.MobilePhone,
                            email = e.WorkEmail,
                            gender = e.GioiTinh,
                            birthday = e.Birthday.HasValue ? e.Birthday.Value.ToString("yyyy-MM-dd") : null,
                            departmentId = e.DepartmentId,
                            departmentCode = e.DepartmentCode,
                            department = DepartmentHrmStore.FullPath(e.DepartmentId, deptById) ?? e.DepartmentName,
                            jobCode = e.JobCode,
                            job = e.JobName,
                            workPositionCode = e.ViTriCongViecCode,
                            workPosition = e.ViTriCongViecName,
                            isCollaborator = e.IsCongTacVien
                        }).ToList();
                        totalItems = paged.Pager.TotalItems;
                        totalPages = paged.Pager.TotalPages;
                        actualPage = paged.Pager.Page;
                        break;
                    }

                case "departments":
                    {
                        var all = DepartmentHrmStore.All();
                        var counts = EmployeeHrmStore.CountsByDepartment();
                        var slice = PageSlice(all.OrderBy(d => d.DepartmentName, StringComparer.CurrentCulture).ToList(),
                            page, pageSize, out totalItems, out totalPages, out actualPage);

                        data = slice.Select(d => new
                        {
                            id = d.DepartmentId,
                            code = d.DepartmentCode,
                            name = d.DepartmentName,
                            parentId = d.DepartmentParentId,
                            parentCode = d.DepartmentParentCode,
                            employeeCount = counts.ContainsKey(d.DepartmentId) ? counts[d.DepartmentId] : 0
                        }).ToList();
                        break;
                    }

                case "jobs":
                    {
                        var all = JobHrmStore.All();
                        var counts = EmployeeHrmStore.CountsByJob();
                        var slice = PageSlice(all.OrderBy(j => j.JobName, StringComparer.CurrentCulture).ToList(),
                            page, pageSize, out totalItems, out totalPages, out actualPage);

                        data = slice.Select(j => new
                        {
                            id = j.JobId,
                            code = j.JobCode,
                            name = j.JobName,
                            employeeCount = counts.ContainsKey(j.JobId) ? counts[j.JobId] : 0
                        }).ToList();
                        break;
                    }

                default:
                    return null;
            }

            var response = new HrmApiResponse
            {
                Code = HrmApiResponse.Codes.Success,
                Message = "Thành công.",
                RequestId = req.RequestId,
                Resource = resource,
                Page = actualPage,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Data = data
            };

            // Ký phản hồi để đối tác kiểm tính toàn vẹn phần khung (mã, số lượng, phân trang).
            response.Checksum = IntegrationKey.Sign(system.PrivateKey,
                response.Code, response.Message, response.RequestId, response.Resource,
                response.Page.ToString(), response.PageSize.ToString(), response.TotalItems.ToString());

            return response;
        }

        /// <summary>Cắt một trang từ danh sách đã nạp hết trong bộ nhớ — đơn vị/chức danh CAS chỉ
        /// vài trăm dòng, không đáng phải phân trang bằng SQL riêng.</summary>
        private static List<TItem> PageSlice<TItem>(
            List<TItem> all, int page, int pageSize, out int totalItems, out int totalPages, out int actualPage)
        {
            totalItems = all.Count;
            totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            actualPage = page < 1 ? 1 : (page > totalPages ? totalPages : page);

            return all.Skip((actualPage - 1) * pageSize).Take(pageSize).ToList();
        }

        /// <summary>Đọc thân request dạng JSON và chuyển thành đối tượng yêu cầu.</summary>
        private HrmApiRequest ReadJsonBody()
        {
            Request.InputStream.Position = 0;
            string body;
            using (var reader = new StreamReader(Request.InputStream, Encoding.UTF8))
            {
                body = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(body)) return null;
            return JsonConvert.DeserializeObject<HrmApiRequest>(body);
        }

        /// <summary>
        /// Trả JSON bằng Newtonsoft (camelCase), thay cho JsonResult mặc định của MVC, để định dạng
        /// khoá nhất quán và không bị chặn khi trả mảng.
        /// </summary>
        private ActionResult Json(HrmApiResponse response)
        {
            var json = JsonConvert.SerializeObject(response, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            });

            return Content(json, "application/json; charset=utf-8");
        }
    }
}
