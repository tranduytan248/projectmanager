using System;
using System.Linq;
using System.Threading;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>Ket qua mot lan xin ma OTP — [Message] luon co gia tri du [Ok] hay khong, de
    /// controller tra thang cho nguoi dung.</summary>
    public class RequestOtpResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
    }

    /// <summary>Ket qua mot lan xac thuc OTP + dat lai mat khau.</summary>
    public class ResetPasswordResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Man "Quen mat khau" cua mobile: nhap so dien thoai -&gt; nhan OTP qua SMS -&gt; nhap OTP dung
    /// -&gt; he thong sinh mat khau moi, luu lai va gui qua SMS toi dung so do. KHONG tiet lo so dien
    /// thoai co gan tai khoan hay khong (giong nguyen tac AccountController.Login/AuthApiController.Login):
    /// moi buoc chan spam deu tinh theo SO DIEN THOAI DA CHUAN HOA, khong theo user, de hanh vi
    /// giong het nhau du so do co ton tai trong he thong hay khong.
    /// </summary>
    public static class PasswordResetService
    {
        private const int OtpExpiryMinutes = 5;
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
        private const int MaxRequestsPerDay = 5;
        private const int MaxVerifyAttempts = 5;

        private const string GenericSentMessage =
            "Nếu số điện thoại đã đăng ký, mã OTP sẽ được gửi tới bạn trong giây lát.";
        private const string GenericOtpError = "Mã OTP không đúng hoặc đã hết hạn.";

        public static RequestOtpResult RequestOtp(string phoneRaw)
        {
            System.Collections.Generic.List<string> invalid;
            var phones = SmsClient.NormalizePhones(phoneRaw, out invalid);
            if (phones.Count != 1)
            {
                return new RequestOtpResult { Ok = false, Message = "Số điện thoại không hợp lệ." };
            }
            var phone = phones[0];

            var recent = Repository.PasswordResetOtps.All()
                .Where(o => o.Phone == phone)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            var last = recent.FirstOrDefault();
            if (last != null && DateTime.Now - last.CreatedAt < ResendCooldown)
            {
                var waitSeconds = (int)Math.Ceiling((ResendCooldown - (DateTime.Now - last.CreatedAt)).TotalSeconds);
                return new RequestOtpResult
                {
                    Ok = false,
                    Message = string.Format("Vui lòng đợi {0} giây trước khi gửi lại.", waitSeconds)
                };
            }

            var todayCount = recent.Count(o => o.CreatedAt.Date == DateTime.Today);
            if (todayCount >= MaxRequestsPerDay)
            {
                return new RequestOtpResult
                {
                    Ok = false,
                    Message = "Bạn đã yêu cầu mã quá nhiều lần hôm nay. Vui lòng thử lại sau."
                };
            }

            // Moi lan xin ma moi thi cac ma cu (neu con hieu luc) coi nhu vo hieu — tranh con nhieu
            // ma cung song song gay nham lan khi xac thuc.
            foreach (var o in recent.Where(o => !o.IsUsed && o.ExpiresAt > DateTime.Now))
            {
                o.IsUsed = true;
                Repository.PasswordResetOtps.Update(o);
            }

            var user = Repository.Users.FirstOrDefault(u =>
                u.IsActive && MatchesPhone(u.Phone, phone));

            // QUAN TRONG: LUON ghi mot ban ghi theo doi cho so dien thoai nay, du co khop user hay
            // khong. Truoc day chi Insert khi khop user — vi cooldown/tran ngay o tren doc theo
            // dung bang nay, so CHUA dang ky se khong bao gio bi chan, tao ra mot "oracle" doan
            // chac chan qua khac biet ma loi/HTTP status giua lan goi thu nhat va thu hai. Voi so
            // khong khop, van sinh OTP that (khong bao gio gui di) chi de UserId = 0 — VerifyAndReset
            // da san co kiem tra Repository.Users.Find(otp.UserId) == null nen ban ghi "gia" nay
            // khong the dung de dat lai mat khau cua ai.
            var code = SecretGenerator.GenerateOtp();

            Repository.PasswordResetOtps.Insert(new PasswordResetOtp
            {
                UserId = user != null ? user.Id : 0,
                Phone = phone,
                CodeHash = PasswordHasher.Hash(code),
                AttemptCount = 0,
                IsUsed = false,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(OtpExpiryMinutes)
            });

            if (user != null)
            {
                var message = string.Format(
                    "Ma OTP dat lai mat khau cua ban la: {0}. Ma co hieu luc trong {1} phut. " +
                    "Khong chia se ma nay cho bat ky ai.",
                    code, OtpExpiryMinutes);

                // Gui nen (khong doi tong dai tra loi) de thoi gian phan hoi cua endpoint anonymous
                // nay KHONG khac biet giua "co user" va "khong co user" — neu goi dong bo o day thi
                // do tre goi ra tong dai SMS ngoai (hang tram ms toi vai giay) se lam lo viec so co
                // gan tai khoan hay khong qua thoi gian phan hoi, du noi dung JSON tra ve giong het
                // nhau. Loi gui (neu co) khong con doc duoc o day nen khong anh huong nguoi goi —
                // chap nhan duoc vi day la kenh phu, giong cach EmailTemplateService.Send dang lam.
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { SmsClient.Send(new[] { phone }, message); }
                    catch (Exception) { /* kenh phu, khong lam hong thao tac chinh */ }
                });
            }

            return new RequestOtpResult { Ok = true, Message = GenericSentMessage };
        }

        public static ResetPasswordResult VerifyAndReset(string phoneRaw, string code)
        {
            System.Collections.Generic.List<string> invalid;
            var phones = SmsClient.NormalizePhones(phoneRaw, out invalid);
            if (phones.Count != 1)
            {
                return new ResetPasswordResult { Ok = false, Error = "Số điện thoại không hợp lệ." };
            }
            var phone = phones[0];

            var otp = Repository.PasswordResetOtps.All()
                .Where(o => o.Phone == phone && !o.IsUsed && o.ExpiresAt > DateTime.Now)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            if (otp == null || otp.AttemptCount >= MaxVerifyAttempts)
            {
                return new ResetPasswordResult { Ok = false, Error = GenericOtpError };
            }

            if (string.IsNullOrWhiteSpace(code) || !PasswordHasher.Verify(code, otp.CodeHash))
            {
                otp.AttemptCount++;
                Repository.PasswordResetOtps.Update(otp);
                return new ResetPasswordResult { Ok = false, Error = GenericOtpError };
            }

            var user = Repository.Users.Find(otp.UserId);
            if (user == null || !user.IsActive)
            {
                otp.IsUsed = true;
                Repository.PasswordResetOtps.Update(otp);
                return new ResetPasswordResult { Ok = false, Error = GenericOtpError };
            }

            var newPassword = SecretGenerator.GeneratePassword();
            user.PasswordHash = PasswordHasher.Hash(newPassword);
            Repository.Users.Update(user);

            otp.IsUsed = true;
            Repository.PasswordResetOtps.Update(otp);

            var message = string.Format(
                "Mat khau moi cua tai khoan {0} la: {1}. Vui long dang nhap va doi mat khau ngay.",
                user.UserName, newPassword);

            // Gui nen — cung ly do nhu ben RequestOtp, kenh phu khong duoc chan phan hoi chinh.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { SmsClient.Send(new[] { phone }, message); }
                catch (Exception) { /* kenh phu, khong lam hong thao tac chinh */ }
            });

            return new ResetPasswordResult { Ok = true };
        }

        /// <summary>So dien thoai tren ho so User co the chua chuan hoa (nhap tay/nguon HRM) — so
        /// sanh bang cach chuan hoa lai TRUOC khi so, dung Y HET phep chuan hoa ben SmsClient.</summary>
        private static bool MatchesPhone(string userPhone, string normalizedTarget)
        {
            if (string.IsNullOrWhiteSpace(userPhone)) return false;

            System.Collections.Generic.List<string> invalid;
            var normalized = SmsClient.NormalizePhones(userPhone, out invalid);
            return normalized.Count == 1 && normalized[0] == normalizedTarget;
        }
    }
}
