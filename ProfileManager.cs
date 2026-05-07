using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace 云端管理
{
    // 每台服务器的数据模型
    public class SshProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // 唯一ID
        public string Name { get; set; }
        public string Host { get; set; }
        public string Port { get; set; } = "22";
        public string Username { get; set; }

        // AuthType 可以是 "Password" 或 "Key"
        public string AuthType { get; set; }

        // 如果是密码登录，这里存密码；如果是密钥登录，这里直接存【私钥文件的完整文本内容】
        public string SecretData { get; set; }
    }

    public static class ProfileManager
    {
        private static readonly string DataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.dat");

        // 保存配置（加密并写入文件）
        public static void SaveProfiles(List<SshProfile> profiles, string masterPassword)
        {
            // 1. 将列表序列化为 JSON 字符串
            string json = JsonConvert.SerializeObject(profiles);

            // 2. 加密 JSON
            string encryptedData = CryptoHelper.Encrypt(json, masterPassword);

            // 3. 写入文件
            File.WriteAllText(DataFilePath, encryptedData);
        }

        // 读取配置（读取文件并解密）
        public static List<SshProfile> LoadProfiles(string masterPassword)
        {
            if (!File.Exists(DataFilePath)) return new List<SshProfile>();

            try
            {
                string encryptedData = File.ReadAllText(DataFilePath);
                string json = CryptoHelper.Decrypt(encryptedData, masterPassword);
                return JsonConvert.DeserializeObject<List<SshProfile>>(json);
            }
            catch (CryptographicException)
            {
                throw new Exception("主密码错误或文件已损坏！");
            }
        }
    }
}