using System;
using System.Security.Cryptography;
using System.Text;

namespace IBrary.Managers
{
    class UserManager
    {
        private static readonly byte[] secretKey = Encoding.UTF8.GetBytes("09.25.24.2024/25"); // 16 bytes key for AES-128

        public static string GetUsernameFromKey(string key)
        {
            try
            {
                byte[] combined = Convert.FromBase64String(key); // Decode the Base64 string

                using (Aes aes = Aes.Create())
                {
                    aes.Key = secretKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7; // Fills remaining bytes if username is shorter than block size

                    // Extract IV from combined data
                    byte[] iv = new byte[16]; // AES block size is 16 bytes
                    byte[] encrypted = new byte[combined.Length - iv.Length];

                    Array.Copy(combined, 0, iv, 0, iv.Length); // First 16 bytes extracted as IV
                    Array.Copy(combined, iv.Length, encrypted, 0, encrypted.Length); // Remaining bytes are the encrypted username

                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length); // Decrypt the username (without padding)
                        return Encoding.UTF8.GetString(decrypted); // Convert decrypted bytes to string
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

