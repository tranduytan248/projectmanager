using System.Collections.Generic;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Bản tin đối tác gửi tới để nhờ gửi SMS. Bốn tham số, trong đó khoá bí mật được truyền
    /// thẳng lên và đối chiếu với khoá của hệ thống khai ở màn Hệ thống tích hợp.
    /// </summary>
    public class SmsApiRequest
    {
        /// <summary>(1) Mã hệ thống tích hợp — cột Mã ở màn Hệ thống tích hợp. Bắt buộc.</summary>
        public string PartnerCode { get; set; }

        /// <summary>(2) Danh sách số nhận, ngăn nhau bởi dấu phẩy. Bắt buộc.</summary>
        public string PhoneNums { get; set; }

        /// <summary>(3) Nội dung tin nhắn. Bắt buộc.</summary>
        public string SmsContents { get; set; }

        /// <summary>(4) Khoá bí mật — cột Khoá bí mật của chính hệ thống đó. Bắt buộc.</summary>
        public string SecretKey { get; set; }
    }

    /// <summary>Phản hồi của API gửi SMS. Mã "00" là thành công; khác là lỗi (xem Message).</summary>
    public class SmsApiResponse
    {
        public string Code { get; set; }
        public string Message { get; set; }

        /// <summary>Số máy đã chuyển cho tổng đài, đã chuẩn hoá về dạng 84xxxxxxxxx.</summary>
        public List<string> Data { get; set; }

        /// <summary>Số lượng máy nhận, tương ứng số phần tử của Data.</summary>
        public int TotalPhones { get; set; }

        /// <summary>Các số bị loại vì sai định dạng. Không có thì bỏ qua trong JSON.</summary>
        public List<string> InvalidPhones { get; set; }

        /// <summary>Mã kết quả dùng chung.</summary>
        public static class Codes
        {
            public const string Success = "00";
            public const string InvalidKey = "01";
            public const string PartnerNotAllowed = "02";
            public const string BadRequest = "03";
            public const string GatewayError = "04";
            public const string ServerError = "99";
        }

        public static SmsApiResponse Error(string code, string message)
        {
            return new SmsApiResponse
            {
                Code = code,
                Message = message,
                Data = new List<string>()
            };
        }
    }
}
