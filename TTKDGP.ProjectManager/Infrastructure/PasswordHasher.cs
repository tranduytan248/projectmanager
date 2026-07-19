using System;
using System.Security.Cryptography;

namespace TTKDGP.ProjectManager.Infrastructure
{
    /// <summary>
    /// Băm mật khẩu bằng PBKDF2-SHA256 với muối ngẫu nhiên riêng cho từng người dùng.
    /// Chuỗi lưu trữ có dạng: iterations.salt_base64.key_base64
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iterations = 100000;
        private const int SaltSize = 32;
        private const int KeySize = 32;

        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException("password");

            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var key = DeriveKey(password, salt, Iterations, KeySize);

            return string.Format("{0}.{1}.{2}",
                Iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;

            var parts = storedHash.Split('.');
            if (parts.Length != 3) return false;

            int iterations;
            if (!int.TryParse(parts[0], out iterations) || iterations <= 0) return false;

            byte[] salt, expectedKey;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedKey = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actualKey = DeriveKey(password, salt, iterations, expectedKey.Length);
            return FixedTimeEquals(actualKey, expectedKey);
        }

        private static byte[] DeriveKey(string password, byte[] salt, int iterations, int keySize)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(keySize);
            }
        }

        /// <summary>So sánh không phụ thuộc thời gian, tránh rò rỉ thông tin qua thời gian xử lý.</summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
