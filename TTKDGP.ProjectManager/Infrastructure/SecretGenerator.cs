using System.Security.Cryptography;
using System.Text;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Sinh chuoi ngau nhien dung cho bi mat ngan han (ma OTP, mat khau tam) — luon dung
    /// RandomNumberGenerator (an toan mat ma), KHONG dung System.Random (du doan duoc, khong hop
    /// cho du lieu nhay cam). Dung chung khuon voi PasswordHasher.
    /// </summary>
    public static class SecretGenerator
    {
        /// <summary>Bang ky tu cho mat khau ngau nhien: bo 0/O/o va 1/l/I vi de nham khi doc tu tin
        /// nhan roi go lai bang tay; khong co ky tu dac biet vi ly do tuong tu.</summary>
        private const string PasswordAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz";

        /// <summary>Ma OTP 6 chu so (000000-999999, giu nguyen so 0 dau).</summary>
        public static string GenerateOtp()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var value = System.BitConverter.ToUInt32(bytes, 0) % 1000000;
                return value.ToString("D6");
            }
        }

        /// <summary>Mat khau ngau nhien danh cho "Quen mat khau" — nguoi dung se doc tu SMS roi go
        /// lai, nen do dai vua phai va bang ky tu de doc/go.</summary>
        public static string GeneratePassword(int length = 10)
        {
            var result = new StringBuilder(length);

            // Do dai bang chu khong chia het 256 — lay thang byte % length se thien vi nhe cho
            // cac ky tu dau bang. Loai bo (rejection sampling) cac byte >= boi so lon nhat cua
            // do dai bang duoi 256, de moi ky tu co xac suat duoc chon bang nhau tuyet doi.
            var alphabetLength = PasswordAlphabet.Length;
            var limit = (256 / alphabetLength) * alphabetLength;

            using (var rng = RandomNumberGenerator.Create())
            {
                var buffer = new byte[1];
                while (result.Length < length)
                {
                    rng.GetBytes(buffer);
                    if (buffer[0] >= limit) continue;
                    result.Append(PasswordAlphabet[buffer[0] % alphabetLength]);
                }
            }

            return result.ToString();
        }
    }
}
