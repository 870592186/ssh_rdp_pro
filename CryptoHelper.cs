using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace 云端管理
{
    public static class CryptoHelper
    {
        // 固定的盐值（Salt），用于增加密码破解难度
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("MySshManagerSalt_2026");

        // 从主密码生成 256 位 (32字节) AES 密钥
        private static byte[] GetKey(string password)
        {
            using (var keyDerivation = new Rfc2898DeriveBytes(password, Salt, 100000, HashAlgorithmName.SHA256))
            {
                return keyDerivation.GetBytes(32);
            }
        }

        // 加密字符串
        public static string Encrypt(string plainText, string masterPassword)
        {
            byte[] key = GetKey(masterPassword);
            byte[] iv = new byte[16]; // 初始化向量

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                // 随机生成 IV
                RandomNumberGenerator.Fill(iv);
                aes.IV = iv;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // 先把 IV 写入头部，解密时需要用到
                    memoryStream.Write(iv, 0, iv.Length);

                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(plainText);
                    }

                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        // 解密字符串
        public static string Decrypt(string cipherText, string masterPassword)
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            byte[] key = GetKey(masterPassword);
            byte[] iv = new byte[16];

            // 提取头部的前16个字节作为 IV
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (MemoryStream memoryStream = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader streamReader = new StreamReader(cryptoStream))
                {
                    return streamReader.ReadToEnd(); // 返回解密后的原始 JSON
                }
            }
        }
    }
}