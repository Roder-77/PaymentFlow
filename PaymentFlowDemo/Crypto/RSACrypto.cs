using NLog;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;


namespace SocialChat.Helpers.Crypto
{
    public class RSACrypto : ICrypto
    {
        // 公鑰
        private readonly string _publicKey;
        // 私鑰
        private readonly string _privateKey;
        // 是否使用 OAEP padding
        private readonly bool _isOAEP = false;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public RSACrypto(string publicKey, string privateKey)
        {
            _publicKey = publicKey;
            _privateKey = privateKey;
        }

        public RSACrypto(string publicKey, string privateKey, bool isOAEP)
        {
            _publicKey = publicKey;
            _privateKey = privateKey;
            _isOAEP = isOAEP;
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
                if (string.IsNullOrEmpty(_publicKey))
                    throw new Exception("RSA Encrypt Failed, PublicKey Invalid.");

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(_publicKey);
                    var byteData = Encoding.UTF8.GetBytes(payload);
                    var encryptedData = rsa.Encrypt(byteData, _isOAEP);

                    return Convert.ToBase64String(encryptedData);
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
                if (string.IsNullOrEmpty(_privateKey))
                    throw new Exception("RSA Decrypt Failed, PrivateKey Invalid.");

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(_privateKey);
                    var encryptedData = Convert.FromBase64String(payload);
                    var decryptedData = rsa.Decrypt(encryptedData, _isOAEP);

                    return Encoding.UTF8.GetString(decryptedData);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, MethodBase.GetCurrentMethod().Name);
                throw;
            }
        }

    }
}