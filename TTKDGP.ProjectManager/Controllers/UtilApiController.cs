using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// API tiện ích cho hệ thống đối tác. Không đăng nhập; xác thực bằng cặp mã hệ thống và khoá
    /// bí mật khai ở màn Hệ thống tích hợp — đối tác truyền thẳng khoá lên, hệ thống đối chiếu.
    ///
    /// Endpoint: GET /api/Util/SendSms
    /// </summary>
    public class UtilApiController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        // Nội dung tin nhắn là văn bản tự do, có thể chứa "<" hoặc "&#" (ví dụ "Chu y <b> hoac gia &#8364;").
        // Bộ kiểm tra đầu vào của ASP.NET coi đó là mã độc và ném lỗi TRƯỚC khi vào thân hàm, khiến đối tác
        // nhận trang HTML 500 thay vì JSON. Tắt kiểm tra ở đây được vì nội dung không bao giờ được kết xuất
        // ra HTML — nó chỉ đi tiếp sang tổng đài dưới dạng đã URL-encode.
        [ValidateInput(false)]
        public ActionResult SendSms(string phoneNums, string smsContents, string partnerCode, string secretKey)
        {
            var req = new SmsApiRequest
            {
                PartnerCode = partnerCode,
                PhoneNums = phoneNums,
                SmsContents = smsContents,
                SecretKey = secretKey
            };

            try
            {
                if (string.IsNullOrWhiteSpace(req.PartnerCode)
                    || string.IsNullOrWhiteSpace(req.PhoneNums)
                    || string.IsNullOrWhiteSpace(req.SmsContents)
                    || string.IsNullOrWhiteSpace(req.SecretKey))
                {
                    return Json(SmsApiResponse.Error(SmsApiResponse.Codes.BadRequest,
                        "Thiếu tham số bắt buộc (partnerCode, phoneNums, smsContents, secretKey)."));
                }

                // Tìm hệ thống theo mã; phải đang hoạt động mới được gọi.
                var system = Repository.IntegrationSystems.FirstOrDefault(x =>
                    string.Equals(x.Code, req.PartnerCode.Trim(), StringComparison.OrdinalIgnoreCase));

                if (system == null || !system.IsActive)
                {
                    return Json(SmsApiResponse.Error(SmsApiResponse.Codes.PartnerNotAllowed,
                        "Hệ thống không tồn tại hoặc đang bị khoá."));
                }

                // Khoá gửi lên phải khớp khoá bí mật của chính hệ thống đó.
                if (!IntegrationKey.Matches(req.SecretKey, system.PrivateKey))
                {
                    return Json(SmsApiResponse.Error(SmsApiResponse.Codes.InvalidKey,
                        "Khoá bí mật không đúng."));
                }

                if (!AppSettings.Sms.IsConfigured)
                {
                    return Json(SmsApiResponse.Error(SmsApiResponse.Codes.GatewayError,
                        "Tính năng gửi SMS đang tắt hoặc chưa cấu hình tổng đài."));
                }

                List<string> invalid;
                var phones = SmsClient.NormalizePhones(req.PhoneNums, out invalid);

                if (phones.Count == 0)
                {
                    return Json(SmsApiResponse.Error(SmsApiResponse.Codes.BadRequest,
                        "Không có số điện thoại nào hợp lệ. Số phải dạng 09xxxxxxxx hoặc 84xxxxxxxxx."));
                }

                if (phones.Count > AppSettings.Sms.MaxPhones)
                {
                    return Json(SmsApiResponse.Error(SmsApiResponse.Codes.BadRequest, string.Format(
                        "Một lần gọi chỉ gửi tối đa {0} số, bản tin có {1} số.",
                        AppSettings.Sms.MaxPhones, phones.Count)));
                }

                var sent = SmsClient.Send(phones, req.SmsContents);
                if (!sent.Ok)
                {
                    return Json(SmsApiResponse.Error(SmsApiResponse.Codes.GatewayError,
                        "Tổng đài từ chối: " + sent.Error));
                }

                return Json(new SmsApiResponse
                {
                    Code = SmsApiResponse.Codes.Success,
                    Message = string.IsNullOrWhiteSpace(sent.Message) ? "Gửi tin nhắn thành công" : sent.Message,
                    Data = phones,
                    TotalPhones = phones.Count,
                    InvalidPhones = invalid.Count > 0 ? invalid : null
                });
            }
            catch (Exception ex)
            {
                return Json(SmsApiResponse.Error(SmsApiResponse.Codes.ServerError, "Lỗi hệ thống: " + ex.Message));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult GetServerErrors(string key)
        {
            if (key != "pmncpt2026") return HttpNotFound();
            var logPath = ErrorLog.LogFile();
            if (!System.IO.File.Exists(logPath)) return Content("Log file does not exist yet.");
            var lines = System.IO.File.ReadAllLines(logPath, System.Text.Encoding.UTF8);
            var recent = lines.Skip(Math.Max(0, lines.Length - 100)).ToArray();
            return Content(string.Join(Environment.NewLine, recent), "text/plain; charset=utf-8");
        }

        /// <summary>
        /// Trả JSON bằng Newtonsoft (camelCase), thay cho JsonResult mặc định của MVC, để định dạng
        /// khoá nhất quán và không bị chặn khi trả mảng.
        /// </summary>
        private ActionResult Json(SmsApiResponse response)
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
