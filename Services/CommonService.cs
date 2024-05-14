using Microsoft.Extensions.Logging;
using Models;
using Models.DataModels;
using Services.Extensions;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Services
{
    public class CommonService
    {
        private readonly ILogger<CommonService> _logger;

        public CommonService(ILogger<CommonService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 產生流水號
        /// previousSeqNo 未傳入，則從 1 算起
        /// </summary>
        /// <param name="prefix">前綴</param>
        /// <param name="previousSeqNo">前一筆流水號</param>
        /// <param name="totalPadWidth">總補位長度</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GenerateSeqNo(string prefix, string? previousSeqNo = null, int totalPadWidth = 4)
        {
            var paddingChar = '0';

            // 無前一筆流水號，直接從頭算起
            if (string.IsNullOrWhiteSpace(previousSeqNo))
                return prefix + "1".PadLeft(totalPadWidth, paddingChar);

            if (!int.TryParse(previousSeqNo[prefix.Length..], out var no))
                throw new Exception($"{nameof(GenerateSeqNo)}, 無法解析前一筆流水號 previous seqNo: {previousSeqNo}");

            return prefix + (no + 1).ToString().PadLeft(totalPadWidth, paddingChar);
        }
    }
}
