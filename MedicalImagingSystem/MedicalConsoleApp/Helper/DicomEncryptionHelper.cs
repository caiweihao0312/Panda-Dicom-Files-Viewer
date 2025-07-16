using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace MedicalConsoleApp.Helper
{
    public static class DicomEncryptionHelper
    {
        private static readonly string EncryptionKey = "YourEncryptionKey123"; // 替换为安全的密钥

        /// <summary>
        /// 加密数据
        /// </summary>
        public static byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16]; // 初始化向量为 0
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            return PerformCryptography(data, encryptor);
        }

        /// <summary>
        /// 解密数据
        /// </summary>
        public static byte[] Decrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16]; // 初始化向量为 0
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            return PerformCryptography(data, decryptor);
        }

        /// <summary>
        /// 执行加密或解密操作
        /// </summary>
        private static byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
        {
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);
            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();
            return memoryStream.ToArray();
        }
    }
}
