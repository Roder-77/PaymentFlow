using NLog;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SocialChat.Helpers.Hash
{
    public class SHA256Hash : IHash
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 生成SHA256雜湊
        /// </summary>
        /// <param name="payload">欲轉換資料</param>
        /// <returns></returns>
        public string Generate(string payload)
        {
            try
            {
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    var messageBytes = Encoding.UTF8.GetBytes(payload);
                    var hashMessage = sha256Hash.ComputeHash(messageBytes);

                    var builder = new StringBuilder();

                    foreach (var msg in hashMessage)
                    {
                        builder.Append(string.Format("{0:x2}", msg));
                    }

                    return builder.ToString();
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