using System;

namespace TTKDGP.ProjectManager.Models
{
    /// <summary>
    /// Mot lan xin ma OTP de dat lai mat khau qua so dien thoai (man "Quen mat khau" cua mobile).
    /// Ma OTP luu dang da bam (PasswordHasher.Hash), khong luu chu ro — doc duoc CSDL truc tiep
    /// (backup, truy cap trai phep) cung khong lay lai duoc ma that.
    /// </summary>
    public class PasswordResetOtp : IEntity
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        /// <summary>So dien thoai da chuan hoa qua SmsClient.NormalizePhones (dang 84xxxxxxxxx).</summary>
        public string Phone { get; set; }

        /// <summary>Ma OTP 6 so, da bam bang PasswordHasher.Hash.</summary>
        public string CodeHash { get; set; }

        /// <summary>So lan nhap sai — vuot nguong thi ban ghi coi nhu het hieu luc, phai xin ma moi.</summary>
        public int AttemptCount { get; set; }

        /// <summary>Da dung de dat lai mat khau thanh cong chua — chan dung lai cung mot ma OTP.</summary>
        public bool IsUsed { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}
