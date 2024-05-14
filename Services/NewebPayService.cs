using Common.Extensions;
using Common.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Models.DataModels;
using Models.Exceptions;
using Models.Response;
using Services.Repositories;
using System.Text;
using System.Text.Json;

namespace Services
{
    public class NewebPayService
    {
        private readonly string _domain;

        private readonly HttpContext _httpContext;

        private readonly CallApiService _callApiService;
        private readonly CommonService _commonService;
        private readonly MailService _mailService;
        private readonly NewebPaySettings _newebPaySettings;

        private readonly IGenericRepository<Order> _orderRepository;
        private readonly IGenericRepository<OrderUniformInvoice> _orderInvoiceRepository;
        private readonly ILogger<EcPayService> _logger;
        private readonly IWebHostEnvironment _env;

        public NewebPayService(
            CallApiService callApiService,
            CommonService commonService,
            MailService mailService,
            IGenericRepository<Order> orderRepository,
            IGenericRepository<OrderUniformInvoice> orderInvoiceRepository,
            ILogger<EcPayService> logger,
            IOptionsSnapshot<NewebPaySettings> newebPayOption,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor)
        {
            _callApiService = callApiService;
            _commonService = commonService;
            _mailService = mailService;
            _orderRepository = orderRepository;
            _orderInvoiceRepository = orderInvoiceRepository;
            _logger = logger;
            _newebPaySettings = newebPayOption.Value;
            _env = env;
            _httpContext = httpContextAccessor.HttpContext!;
            _domain = $"{_httpContext.Request.Scheme}://{_httpContext.Request.Host}";
        }

        /// <summary>
        /// 組成藍新金流 Html
        /// </summary>
        /// <param name="orderSeqNo">訂單編號</param>
        /// <param name="tradeVersionNo">交易版本編號</param>
        /// <returns></returns>
        public async Task<string> ComposeNewebPayHtml(string orderSeqNo, string tradeVersionNo)
        {
            // 訂單資料
            var order = await _orderRepository.Get(
                x => x.SeqNo == orderSeqNo,
                x => x.Include(y => y.Products)
            );

            if (order is null)
                throw new Exception("");

            // 商品資訊
            var itemDesc = $"商品共{order.Products.Count()}件";
            // 串接程式版本
            var version = "2.0";
            // 訂單 Domain
            var notifyUrl = $"{_domain}/Order/NewebPayCallback";
            var returnUrl = $"{_domain}/Order/NewebPayReturn";

            var bodyDic = new Dictionary<string, string>
            {
                // 商店代號
                { "MerchantID", _newebPaySettings.MerchantId },
                // 回傳格式
                { "RespondType", "JSON" },
                // 時間戳記
                { "TimeStamp", DateTime.Now.ToTimestamp().ToString() },
                // 串接程式版本
                { "Version", version },
                // 商店訂單編號
                { "MerchantOrderNo", $"{orderSeqNo}_{tradeVersionNo}" },
                // 訂單金額
                { "Amt", string.Format("{0:0}", order.TotalAmount) },
                // 商品資訊
                { "ItemDesc", itemDesc },
                // 支付完成，返回商店網址
                { "ReturnURL", returnUrl },
                // 支付通知網址
                { "NotifyURL", notifyUrl },
                // 付款人電子信箱
                { "Email", "" },
                // 藍新金流會員，1 = 須要登入藍新金流會員、0 = 不須登入藍新金流會員
                { "LoginType", "0" },
                // 信用卡一次付清，1 = 啟用、0 或者未有此參數=不啟用
                { "CREDIT", "1" }
            };

            // 交易資訊，Item1：交易資料 AES 加密、Item2：交易資料 SHA256 Hash
            var trade = EncryptTransactionInfo();
            var requestDic = new Dictionary<string, string>
            {
                // 商店代號
                { "MerchantID", _newebPaySettings.MerchantId },
                // 交易資料 AES 加密
                { "TradeInfo", trade.tradeInfo },
                // 交易資料 SHA256 Hash
                { "TradeSha", trade.tradeSha },
                // 串接程式版本
                { "Version", version }
            };

            // 回傳組好的 html
            return ComposeHtml();

            // 加密交易資訊，Item1：交易資料 AES 加密、Item2：交易資料 SHA256 Hash
            (string tradeInfo, string tradeSha) EncryptTransactionInfo()
            {
                var queryBuilder = new StringBuilder();

                foreach (var item in bodyDic)
                    queryBuilder.Append($"&{item.Key}={item.Value}");

                // 將開頭 '&' 字元拔除
                queryBuilder.Remove(0, 1);
                // AES 加密
                var tradeInfo = string.Format("{0:X2}", HashHelper.AESEncrypt(_newebPaySettings.HashKey, _newebPaySettings.HashIV, queryBuilder.ToString(), 256)).ToLower();
                var hashPayload = new StringBuilder();

                hashPayload.Append($"HashKey={_newebPaySettings.HashKey}");
                hashPayload.Append($"&{tradeInfo}");
                hashPayload.Append($"&HashIV={_newebPaySettings.HashIV}");

                // SHA256 Hash
                var tradeSha = HashHelper.Generate("SHA256", Encoding.UTF8.GetBytes(hashPayload.ToString())).ToUpper();

                return (tradeInfo, tradeSha);
            }

            // 組成 Html
            string ComposeHtml()
            {
                var htmlBuilder = new StringBuilder();

                htmlBuilder.AppendLine("<html><head><meta charset='utf-8'></head><body>");
                htmlBuilder.AppendLine($"<form id='newebPayForm' method='POST' action='{_newebPaySettings.PaymentUrl}'>");

                foreach (var item in requestDic)
                {
                    htmlBuilder.AppendLine($"<input type='hidden' name='{item.Key}' value='{item.Value}'>");
                }

                htmlBuilder.AppendLine("</form>");
                htmlBuilder.AppendLine("<script>const newebPayForm = document.getElementById('newebPayForm'); if(newebPayForm) newebPayForm.submit();</script>");
                htmlBuilder.AppendLine("<body><html>");

                return htmlBuilder.ToString();
            }
        }

        /// <summary>
        /// 藍新金流 Callback
        /// </summary>
        /// <param name="request">回傳資料</param>
        /// <returns></returns>
        public async Task NewebPayCallback(NewebPayResponse request)
        {
            _logger.LogDebug($"藍新金流 Return: {JsonSerializer.Serialize(request)}");

            // TradeInfo 解密
            var decryptPayload = HashHelper.AESDecrypt(_newebPaySettings.HashKey, _newebPaySettings.HashIV, request.TradeInfo, 256);
            var tradeInfo = JsonSerializer.Deserialize<NewebPayTradeInfo<BaseNewebPayTradeInfoResult>>(decryptPayload);

            if (tradeInfo is null)
                throw new ForbiddenException("");

            _logger.LogInformation($"藍新金流 Return Status = {tradeInfo.Status}, Message = {tradeInfo.Message}, MerchantOrderNo = {tradeInfo.Result.MerchantOrderNo}");

            var orderSeqNo = tradeInfo.Result.MerchantOrderNo.Split('_')[0];

            if (tradeInfo.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                // 處理交易成功
            }
            else
            {
                // Log 紀錄交易失敗
            }
        }

        /// <summary>
        /// 藍新金流Return
        /// </summary>
        /// <param name="request">回傳資料</param>
        /// <returns></returns>
        public async Task<Order> NewebPayReturn(NewebPayResponse request)
        {
            // TradeInfo 解密
            var decryptPayload = HashHelper.AESDecrypt(_newebPaySettings.HashKey, _newebPaySettings.HashIV, request.TradeInfo, 256);
            var tradeInfo = JsonSerializer.Deserialize<NewebPayTradeInfo<BaseNewebPayTradeInfoResult>>(decryptPayload);

            if (tradeInfo is null)
                throw new ForbiddenException("");

            var orderSeqNo = tradeInfo.Result.MerchantOrderNo.Split('_')[0];
            var order = await _orderRepository.Get(x => x.SeqNo == orderSeqNo);

            if (order is null)
                throw new NotFoundException("");

            return order;
        }
    }
}
