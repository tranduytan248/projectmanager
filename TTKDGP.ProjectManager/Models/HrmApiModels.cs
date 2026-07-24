using System.Collections.Generic;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Bản tin đối tác gửi tới để lấy dữ liệu HRM. Thứ tự các trường ở đây cũng chính là thứ tự
    /// dùng để tạo chuỗi tính checksum (xem <see cref="HrmApiRequest.ChecksumFields"/>).
    /// </summary>
    public class HrmApiRequest
    {
        /// <summary>(1) Mã hệ thống tích hợp — khớp với mã trong màn Hệ thống tích hợp. Bắt buộc.</summary>
        public string PartnerCode { get; set; }

        /// <summary>(2) Mã yêu cầu do bên gửi tạo, duy nhất mỗi lần gọi. Bắt buộc.</summary>
        public string RequestId { get; set; }

        /// <summary>(3) Thời gian gửi, định dạng yyyyMMddHHmmss. Bắt buộc.</summary>
        public string RequestTime { get; set; }

        /// <summary>(4) Loại dữ liệu cần lấy: employees | workplaces | positions. Bắt buộc.</summary>
        public string Resource { get; set; }

        /// <summary>(5) Trang, đếm từ 1. Không bắt buộc (mặc định 1).</summary>
        public int? Page { get; set; }

        /// <summary>(6) Số dòng mỗi trang. Không bắt buộc (mặc định 100, tối đa 500).</summary>
        public int? PageSize { get; set; }

        /// <summary>(7) Từ khoá lọc. Không bắt buộc.</summary>
        public string Keyword { get; set; }

        /// <summary>Checksum SHA-256 của các trường (1..7) nối bằng "|" cộng khoá bí mật ở cuối.</summary>
        public string Checksum { get; set; }

        /// <summary>
        /// Các trường tạo checksum, ĐÚNG thứ tự. Trường không bắt buộc mà rỗng thì là chuỗi rỗng.
        /// Bên tính checksum ghép mảng này bằng "|" rồi thêm khoá bí mật ở cuối trước khi băm.
        /// </summary>
        public string[] ChecksumFields()
        {
            return new[]
            {
                PartnerCode ?? string.Empty,
                RequestId ?? string.Empty,
                RequestTime ?? string.Empty,
                Resource ?? string.Empty,
                Page.HasValue ? Page.Value.ToString() : string.Empty,
                PageSize.HasValue ? PageSize.Value.ToString() : string.Empty,
                Keyword ?? string.Empty
            };
        }
    }

    /// <summary>Phản hồi của API HRM. Mã "00" là thành công; khác là lỗi (xem Message).</summary>
    public class HrmApiResponse
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string RequestId { get; set; }
        public string Resource { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        /// <summary>Danh sách bản ghi của trang. Rỗng nếu lỗi.</summary>
        public object Data { get; set; }

        /// <summary>
        /// Checksum của phản hồi để bên nhận kiểm tính toàn vẹn: SHA-256 của
        /// (code|message|requestId|resource|page|pageSize|totalItems) cộng khoá bí mật ở cuối.
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>Mã kết quả dùng chung.</summary>
        public static class Codes
        {
            public const string Success = "00";
            public const string InvalidChecksum = "01";
            public const string PartnerNotAllowed = "02";
            public const string BadRequest = "03";
            public const string ServerError = "99";
        }

        public static HrmApiResponse Error(string code, string message, HrmApiRequest req)
        {
            return new HrmApiResponse
            {
                Code = code,
                Message = message,
                RequestId = req != null ? req.RequestId : null,
                Resource = req != null ? req.Resource : null,
                Data = new List<object>()
            };
        }
    }
}
