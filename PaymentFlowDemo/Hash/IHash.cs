namespace SocialChat.Helpers.Hash
{
    public interface IHash
    {
        /// <summary>
        /// 生成雜湊
        /// </summary>
        /// <param name="payload">欲轉換資料</param>
        /// <returns></returns>
        string Generate(string payload);
    }
}