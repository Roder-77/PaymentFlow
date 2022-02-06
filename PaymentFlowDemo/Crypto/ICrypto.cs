namespace SocialChat.Helpers.Crypto
{
    /// <summary>
    /// 加密處理介面
    /// </summary>
    public interface ICrypto
    {
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="payload">欲加密資料</param>
        /// <returns></returns>
        string Encrypt(string payload);

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="payload">欲解密資料</param>
        /// <returns></returns>
        string Decrypt(string payload);
    }
}
