using System;
using System.Text;
using System.Security.Cryptography;
using IBrary.Managers;

namespace Managers
{
    static class UserManager
    {
        private static readonly byte[] secretKey = Encoding.UTF8.GetBytes("09.25.24.2024/25"); // 16 chars = 16 bytes


        /*public static string GenerateKey(string username)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(username);

            using (Aes aes = Aes.Create())
            {
                aes.Key = secretKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Generate a new random IV for each encryption
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                {
                    byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    // Prepend IV to encrypted data
                    byte[] combined = new byte[iv.Length + encrypted.Length];
                    Array.Copy(iv, 0, combined, 0, iv.Length);
                    Array.Copy(encrypted, 0, combined, iv.Length, encrypted.Length);

                    // Convert to Base64 string
                    return Convert.ToBase64String(combined);
                }
            }
        }*/

        public static string GetUsernameFromKey(string key)
        {
            byte[] combined = Convert.FromBase64String(key);

            using (Aes aes = Aes.Create())
            {
                aes.Key = secretKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV from combined data
                byte[] iv = new byte[16]; // AES block size is 16 bytes
                byte[] encrypted = new byte[combined.Length - iv.Length];

                Array.Copy(combined, 0, iv, 0, iv.Length);
                Array.Copy(combined, iv.Length, encrypted, 0, encrypted.Length);

                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    byte[] decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                    return Encoding.UTF8.GetString(decrypted);
                }
            }
        }
        public static bool isAdmin()
        {
            return SettingsManager.CurrentSettings.Username.Contains("admin");
        }
    }
}

