using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace EnigmaNet.Utils
{
    public class SecurityUtils
    {
        static byte[] GetKey(string key)
        {
            return System.Text.ASCIIEncoding.UTF8.GetBytes(key);
        }
        static byte[] GetIV(string key)
        {
            return System.Text.ASCIIEncoding.UTF8.GetBytes(key);
        }

        /// <summary>
        /// 字节串转换成16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        static string ToHexString(byte[] bytes)
        {
            var ret = new StringBuilder();
            foreach (byte b in bytes)
            {
                ret.AppendFormat("{0:X2}", b);
            }
            return ret.ToString();
        }

        /// <summary>
        /// 16进制字符串转换成字节串
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        static byte[] FromHexString(string text)
        {
            byte[] textBytes = new byte[text.Length / 2];
            for (int x = 0; x < text.Length / 2; x++)
            {
                int i = (Convert.ToInt32(text.Substring(x * 2, 2), 16));
                textBytes[x] = (byte)i;
            }
            return textBytes;
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="text"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string DesEncrypt(string text, string key)
        {
            return DesEncrypt(text, key, Encoding.UTF8);
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="text"></param>
        /// <param name="key"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string DesEncrypt(string text, string key, Encoding encoding)
        {
            // var algorithm = new DESCryptoServiceProvider()
            // {
            //     Key = GetKey(key),
            //     IV = GetIV(key)
            // };

            using var algorithm = Aes.Create();
            algorithm.Key = GetKey(key);
            algorithm.IV = GetIV(key);

            var textBytes = encoding.GetBytes(text);

            var resultBytes = algorithm.CreateEncryptor()
                .TransformFinalBlock(textBytes, 0, textBytes.Length);

            return ToHexString(resultBytes);
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="text"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string DesDecrypt(string text, string key)
        {
            return DesDecrypt(text, key, Encoding.UTF8);
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="text"></param>
        /// <param name="key"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string DesDecrypt(string text, string key, Encoding encoding)
        {
            // var algorithm = new DESCryptoServiceProvider()
            // {
            //     Key = GetKey(key),
            //     IV = GetIV(key)
            // };

            using var algorithm = Aes.Create();
            algorithm.Key = GetKey(key);
            algorithm.IV = GetIV(key);

            var textBytes = FromHexString(text);

            var resultBytes = algorithm.CreateDecryptor()
                .TransformFinalBlock(textBytes, 0, textBytes.Length);

            return encoding.GetString(resultBytes);
        }

        public static string Md5(string text)
        {
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                var resultString = BitConverter.ToString(result);
                return resultString.Replace("-", string.Empty);
            }
        }

        public static string Sha256(string text)
        {
            // using (SHA256 sha = new SHA256Managed())
            // {

            using var sha = SHA256.Create();

            var result = sha.ComputeHash(Encoding.UTF8.GetBytes(text));

            var resultString = BitConverter.ToString(result);

            return resultString.Replace("-", string.Empty);
            // }
        }

        public static string Sha1(string text)
        {
            // using (SHA1 sha = new SHA1Managed())
            // {

            using var sha = SHA1.Create();

            var result = sha.ComputeHash(Encoding.UTF8.GetBytes(text));

            var resultString = BitConverter.ToString(result);

            return resultString.Replace("-", string.Empty);
            // }
        }

        public static string AesEncrypt(string text, string key, string iv)
        {
            var bytes = Encoding.UTF8.GetBytes(text);

            var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(iv);

            var transform = aes.CreateEncryptor();

            var cipherBytes = transform.TransformFinalBlock(bytes, 0, bytes.Length);

            return Convert.ToBase64String(cipherBytes);
        }

        public static string AesDecrypt(string text, string key, string iv)
        {
            var bytes = Convert.FromBase64String(text);

            var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(iv);

            var transform = aes.CreateDecryptor();

            var plainBytes = transform.TransformFinalBlock(bytes, 0, bytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
