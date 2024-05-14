using System.Security.Cryptography;
using System.Text;

#nullable disable

namespace Common.Helpers
{
    public class HashHelper
    {
        /// <summary>
        /// Generate a data hash
        /// Algorithm support: https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptoconfig?view=net-6.0
        /// </summary>
        /// <param name="hashAlgorithm">Hash algorithm</param>
        /// <param name="data">The data for calculating the hash</param>
        /// <param name="trimByteCount">The number of bytes, which will be used in the hash algorithm; leave 0 to use all array</param>
        /// <returns>Data hash</returns>
        public static string Generate(string hashAlgorithm, byte[] data, int trimByteCount = 0)
        {
            if (string.IsNullOrWhiteSpace(hashAlgorithm))
                throw new ArgumentNullException(nameof(hashAlgorithm));

            var algorithm = (HashAlgorithm)CryptoConfig.CreateFromName(hashAlgorithm);

            if (algorithm == null)
                throw new ArgumentException("Unrecognized hash name");

            if (trimByteCount > 0 && data.Length > trimByteCount)
            {
                var newData = new byte[trimByteCount];
                Array.Copy(data, newData, trimByteCount);

                return dataToHashString(newData);
            }

            return dataToHashString(data);

            string dataToHashString(byte[] data) => BitConverter.ToString(algorithm.ComputeHash(data)).Replace("-", string.Empty);
        }

        private static Aes CreateAesProvider(string key, string iv, int keySize)
        {
            var aes = Aes.Create();
            aes.KeySize = keySize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(iv);

            return aes;
        }

        public static string AESEncrypt(string key, string iv, string payload, int keySize = 128)
        {
            // 設定加密參數
            var aes = CreateAesProvider(key, iv, keySize);
            // 創建加密器
            var encryptor = aes.CreateEncryptor();
            // 將明文轉換為字節數組
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            // 加密字節數組
            var encryptedBytes = encryptor.TransformFinalBlock(payloadBytes, 0, payloadBytes.Length);

            // 將加密後的字節數組轉換為字符串
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string AESDecrypt(string key, string iv, string payload, int keySize = 128)
        {
            // 設定加密參數
            var aes = CreateAesProvider(key, iv, keySize);
            // 創建解密器
            var decryptor = aes.CreateDecryptor();
            // 將明文轉換為字節數組
            var payloadBytes = Convert.FromBase64String(payload);
            // 解密字節數組
            var decryptedBytes = decryptor.TransformFinalBlock(payloadBytes, 0, payloadBytes.Length);

            // 將解密後的字節數組轉換為字符串
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
