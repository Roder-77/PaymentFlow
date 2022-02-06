using NLog;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SocialChat.Helpers.Hash
{
    public class MD5Hash : IHash
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 生成 MD5 雜湊
        /// </summary>
        /// <param name="payload">欲轉換資料</param>
        /// <returns></returns>
        public string Generate(string payload)
        {
            try
            {
                using (var md5 = new MD5CryptoServiceProvider())
                {
                    var bytes = Encoding.UTF8.GetBytes(payload);
                    var hash = md5.ComputeHash(bytes);

                    return BitConverter.ToString(hash)
                                       .Replace("-", string.Empty);
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