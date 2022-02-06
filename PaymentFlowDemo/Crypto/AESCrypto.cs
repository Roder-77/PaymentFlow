using NLog;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SocialChat.Helpers.Crypto
{
    /// <summary>
    /// AES
    /// </summary>
    public class AESCrypto : ICrypto
    {
        // key size
        private readonly int _keySize = 128;
        // padding
        private readonly PaddingMode _paddingMode = PaddingMode.PKCS7;
        // cipher mode
        private readonly CipherMode _cipherMode = CipherMode.CBC;
        // 初始向量
        private readonly byte[] _iv;
        // 金鑰
        private readonly byte[] _key;
        // 類別 base64, hex
        private readonly string _type = "base64";
        // logger
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public AESCrypto(byte[] iv, byte[] key, string type = "base64")
        {
            _iv = iv;
            _key = key;
            _type = type;
        }

        public AESCrypto(byte[] iv, byte[] key, int keySize, PaddingMode paddingMode, CipherMode cipherMode, string type = "base64")
        {
            _keySize = keySize;
            _paddingMode = paddingMode;
            _cipherMode = cipherMode;
            _iv = iv;
            _key = key;
            _type = type;
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="payload">欲加密資料</param>
        /// <returns></returns>
        public string Encrypt(string payload)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // AES 加密
                    using (var aes = new RijndaelManaged())
                    {
                        aes.Padding = _paddingMode;
                        aes.KeySize = _keySize;
                        aes.Mode = _cipherMode;
                        aes.Key = _key;
                        aes.IV = _iv;

                        // 將欲加密資料轉為byte array
                        var bytesToBeEncrypted = Encoding.UTF8.GetBytes(payload);

                        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                            cs.Close();
                        }

                        var encrypted = ms.ToArray();

                        switch (_type)
                        {
                            case "hex":
                                return BitConverter.ToString(encrypted).Replace("-", "");
                            case "base64":
                            default:
                                return Convert.ToBase64String(encrypted);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, MethodBase.GetCurrentMethod().Name);
                throw;
            }
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="payload">欲解密資料</param>
        /// <returns></returns>
        public string Decrypt(string payload)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // AES 解密
                    using (var aes = new RijndaelManaged())
                    {
                        aes.Padding = _paddingMode;
                        aes.KeySize = _keySize;
                        aes.Mode = _cipherMode;
                        aes.Key = _key;
                        aes.IV = _iv;

                        // 將欲解密資料轉為byte array
                        byte[] bytesToBeDecrypted;
                        switch (_type)
                        {
                            case "hex":
                                bytesToBeDecrypted = HexToByteArray(payload);
                                break;
                            case "base64":
                            default:
                                bytesToBeDecrypted = Convert.FromBase64String(payload);
                                break;
                        }

                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                            cs.Close();
                        }

                        var decrypted = ms.ToArray();

                        return Encoding.UTF8.GetString(decrypted);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, MethodBase.GetCurrentMethod().Name);
                throw;
            }
        }

        private static byte[] HexToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}