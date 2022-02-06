using NLog;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SocialChat.Helpers.Hash
{
    public class HMACSHA256Hash : IHash
    {
        private readonly string _secretKey;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public HMACSHA256Hash(string secretKey)
        {
            _secretKey = secretKey;
        }

        /// <summary>
        /// 生成HMACSHA256雜湊
        /// </summary>
        /// <param name="payload">欲轉換資料</param>
        /// <returns></returns>
        public string Generate(string payload)
        {
            try
            {
                var encoding = new UTF8Encoding();
                var keyByte = encoding.GetBytes(_secretKey);
                var messageBytes = encoding.GetBytes(payload);

                using (var hmacSHA256 = new HMACSHA256(keyByte))
                {
                    var hashMessage = hmacSHA256.ComputeHash(messageBytes);
                    return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
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