using System;
using System.Text;
using System.Security.Cryptography;
using IBrary.Managers;

namespace Managers
{
    static class UserManager
    {
        private static readonly byte[] secretKey = Encoding.UTF8.GetBytes("09.25.24.2024/25"); // 16 bytes key for AES-128

        public static string GetUsernameFromKey(string key)
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
        
        /*public static string GenerateKey(string username)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(username); // Convert string to bytes

            using (Aes aes = Aes.Create())
            {
                aes.Key = secretKey; 
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7; // Fills remaining bytes if username is shorter than block size

  
                aes.GenerateIV(); // Generate a new random IV for each encryption
                byte[] iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                { 
                    byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length); // Encrypt the username 

                    // Prepend IV to encrypted data
                    byte[] combined = new byte[iv.Length + encrypted.Length]; // 16 bytes IV + 16 bytes encrypted username
                    Array.Copy(iv, 0, combined, 0, iv.Length); // First 16 bytes are the IV
                    Array.Copy(encrypted, 0, combined, iv.Length, encrypted.Length); // Remaining bytes are the encrypted username

                    return Convert.ToBase64String(combined); // Encode 32 bytes to 44-character Base64 string
                }
            }
        }*/
        public static bool IsAdmin()
        {
            return SettingsManager.CurrentSettings.Username.Contains("admin");
        }
    }
}

