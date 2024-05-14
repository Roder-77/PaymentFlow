using BgSoft.Core.Requests;
using Common.Enums;
using Common.Extensions;
using Common.Helpers;
using DeviceDetectorNET;
using ECPay.Payment.Integration;
using ECPay.SDK.Logistics;
using EinvoiceIntegration.Enum;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Models.DataModels;
using Models.Exceptions;
using Models.Request;
using Models.Response;
using Services.Repositories;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Services
{
    public class EcPayService
    {
        private const string _paymentHashAlgorithm = "SHA256";
        private const string _logisticsHashAlgorithm = "MD5";
        private readonly string _domain;

        private readonly HttpContext _httpContext;
        
        private readonly CallApiService _callApiService;
        private readonly CommonService _commonService;
        private readonly EcPaySettings _ecPaySettings;
        private readonly MailService _mailService;

        private readonly IGenericRepository<Order> _orderRepository;
        private readonly IGenericRepository<OrderUniformInvoice> _orderInvoiceRepository;
        private readonly ILogger<EcPayService> _logger;
        private readonly IWebHostEnvironment _env;

        public EcPayService(
            CallApiService callApiService,
            CommonService commonService,
            MailService mailService,
            IGenericRepository<Order> orderRepository,
            IGenericRepository<OrderUniformInvoice> orderInvoiceRepository,
            ILogger<EcPayService> logger,
            IOptionsSnapshot<EcPaySettings> ecPaySettings,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor)
        {
            _callApiService = callApiService;
            _commonService = commonService;
            _mailService = mailService;
            _orderRepository = orderRepository;
            _orderInvoiceRepository = orderInvoiceRepository;
            _logger = logger;
            _ecPaySettings = ecPaySettings.Value;
            _env = env;
            _httpContext = httpContextAccessor.HttpContext!;
            _domain = $"{_httpContext.Request.Scheme}://{_httpContext.Request.Host}";
        }

        /// <summary>
        /// 設定物流材積
        /// </summary>
        /// <param name="requestDict"></param>
        /// <param name="threeDimensionsTotal"></param>
        private void SetLogisticsSpecification(SortedDictionary<string, string> requestDict, decimal threeDimensionsTotal)
        {
            if (threeDimensionsTotal <= 60)
                requestDict.Add("Specification", "0001");
            else if (threeDimensionsTotal <= 90)
                requestDict.Add("Specification", "0002");
            else if (threeDimensionsTotal <= 120)
                requestDict.Add("Specification", "0003");
            else
                requestDict.Add("Specification", "0004");
        }

        /// <summary>
        /// 新增錯誤訊息
        /// </summary>
        /// <param name="orderSeqNo">訂單流水號</param>
        /// <param name="message">訊息</param>
        /// <returns></returns>
        private async Task AddErrorLog(string orderSeqNo, string? message)
        {
            _logger.LogError(message);
            // Add db log
        }

        /// <summary>
        /// 產生訂單 Log
        /// </summary>
        /// <param name="orderId">訂單代碼</param>
        /// <param name="action">執行動作</param>
        /// <param name="message">訊息</param>
        /// <param name="dateTime">發生時間</param>
        /// <returns></returns>
        private OrderLog GenerateOrderLog(int orderId, LogAction action, string message, DateTime? dateTime = null)
        {
            return new()
            {
                OrderId = orderId,
                Action = action,
                CreateTime = dateTime ?? DateTime.Now,
            };
        }

        /// <summary>
        /// 產生綠界付款頁 Html
        /// </summary>
        /// <param name="orderSeqNo">訂單流水號</param>
        /// <param name="memberId">會員代碼</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">不支援的付款方式</exception>
        public async Task<string> GenerateHtml(string orderSeqNo, int memberId)
        {
            var methodName = nameof(GenerateHtml);
            var order = await _orderRepository.Get(
                x => x.SeqNo == orderSeqNo && x.MemberId == memberId,
                x => x.Include(y => y.Products)
            );

            if (order is null)
                throw new Exception($"{methodName}, 查無此訂單");

            // 更新訂單特店訂單編號
            order.MerchantTradeNo = _commonService.GenerateSeqNo(order.SeqNo, order.MerchantTradeNo);
            await _orderRepository.Update(order);

            var deviceDetector = new DeviceDetector(_httpContext.Request.Headers["User-Agent"]);
            // 付款結果通知返回網址
            var paymentResultUrl = $"{_domain}/Order/EcPayPaymentResult";
            // 取號結果通知返回網址
            var takeNumberResultUrl = $"{_domain}/Order/EcPayTakeNumberResult";
            // 客戶端返回商店網址
            var clientResultUrl = $"{_domain}/Bag/Result/{order.Id}";
            // 訂單付款方式
            var paymentMethod = (PaymentMethod)order.PaymentMethod;
            // 裝置類型
            var deviceType = deviceDetector.IsMobile() ? DeviceType.Mobile : DeviceType.PC;
            var requestDict = new SortedDictionary<string, string>
            {
                // *特店代碼
                { "MerchantID", _ecPaySettings.Payment.MerchantId },
                // *特店訂單編號 (同個訂單每次特店訂單編號不可重複)
                { "MerchantTradeNo", order.MerchantTradeNo },
                // *特店交易時間
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                // 交易類型
                { "PaymentType", "aio" },
                // *交易總金額
                { "TotalAmount", string.Format("{0:0}", order.TotalAmount) },
                // *交易描述 
                { "TradeDesc", "交易描述" },
                // 來源裝置
                { "DeviceSource", deviceType.ToString() },
                // *訂單的商品名稱
                { "ItemName", $"商品{order.Products.Sum(x => x.Quantity)}件" },
                // *付款方式
                { "ChoosePayment", paymentMethod.ToString() },
                // *付款結果通知返回網址
                { "ReturnURL", paymentResultUrl },
                // CheckMacValue 加密類型
                { "EncryptType", "1"},
                // 客制欄位 - 付款方式
                { "CustomField1", paymentMethod.ToString() }
            };

            switch (paymentMethod)
            {
                case PaymentMethod.Credit:
                    // *付款完成前台使用者看到的返回網址
                    requestDict.Add("OrderResultURL", clientResultUrl);
                    requestDict.Add("NeedExtraPaidInfo", "Y");
                    break;
                case PaymentMethod.ATM:
                    // 付款子項目 (寫死第一銀行，請參考: https://developers.ecpay.com.tw/?p=5679)
                    requestDict.Add("ChooseSubPayment", "FIRST");
                    // *訂單取號結果通知
                    requestDict.Add("PaymentInfoURL", takeNumberResultUrl);
                    // *取號完成前台使用者看到的返回網址
                    requestDict.Add("ClientRedirectURL", clientResultUrl);
                    break;
                default:
                    throw new NotImplementedException($"{methodName}, 不支援的付款方式");
            }

            // Compose mac value
            var macValue = ComposeMacValue(requestDict, _ecPaySettings.Payment.HashKey, _ecPaySettings.Payment.HashIV, _paymentHashAlgorithm);

            requestDict.Add("CheckMacValue", macValue);

            // Compose Html
            var htmlBuilder = new StringBuilder();

            htmlBuilder.AppendLine("<html><head><meta charset='utf-8'></head><body>");
            htmlBuilder.AppendLine($"<form name='postdata' id='postdata' action='{_ecPaySettings.PaymentUrl}' method='POST' accept-charset='utf-8'>");

            foreach (var item in requestDict)
            {
                htmlBuilder.AppendLine($"<input type='hidden' name='{item.Key}' value='{item.Value}'>");
            }

            htmlBuilder.AppendLine("</form>");
            htmlBuilder.AppendLine("<script> const theForm = document.forms['postdata'];  if (!theForm) { theForm = document.postdata; } theForm.submit(); </script>");
            htmlBuilder.AppendLine("<html><body>");

            return htmlBuilder.ToString();
        }

        /// <summary>
        /// 組成 mac value
        /// </summary>
        /// <param name="dic">dictionary</param>
        /// <returns></returns>
        private string ComposeMacValue(SortedDictionary<string, string> dic, string hashKey, string hashIV, string hashAlgorithm)
        {
            // Compose query string
            var queryBuilder = new StringBuilder();

            queryBuilder.Append($"HashKey={hashKey}");

            foreach (var item in dic)
            {
                queryBuilder.Append($"&{item.Key}={item.Value}");
            }

            queryBuilder.Append($"&HashIV={hashIV}");

            // Compose mac value
            var urlEncoded = HttpUtility.UrlEncode(queryBuilder.ToString()).ToLower();
            return HashHelper.Generate(hashAlgorithm, Encoding.UTF8.GetBytes(urlEncoded)).ToUpper();
        }

        /// <summary>
        /// 處理綠界付款結果
        /// </summary>
        /// <param name="response">綠界付款回傳資料</param>
        /// <returns></returns>
        public async Task HandlePaymentResult(EcPayPaymentWithExtraResponse response)
        {
            var methodName = nameof(HandlePaymentResult);
            var responseJson = JsonSerializer.Serialize(response);
            var result = response.CustomField1 == PaymentMethod.Credit.ToString() ? response : JsonSerializer.Deserialize<EcPayResponse>(responseJson);

            _logger.LogInformation($"{methodName}, ecpay payment response: {responseJson}");

            if (!ValidMacValue(result, _ecPaySettings.Payment.HashKey, _ecPaySettings.Payment.HashIV, _paymentHashAlgorithm, out var checkMacValue))
            {
                _logger.LogError($"{methodName} CheckMacValue invalid, check mac value: {checkMacValue}");
                return;
            }

            var order = await GetOrder(response.MerchantTradeNo);
            var now = DateTime.Now;

            // 付款失敗紀錄 Log
            if (response.RtnCode != 1)
            {
                order.Logs.Add(new()
                {
                    Action = LogAction.其他,
                    Remark = response.RtnMsg,
                    CreateTime = now
                });

                await _orderRepository.Update(order);
                return;
            }

            //if (order.PaymentMethod == PaymentMethod.Credit)
            //    order.CreditCardNo = response.card4no;

            order.TradeNo = response.TradeNo;
            order.Status = OrderStatus.已付款;
            order.UpdateTime = now;
            //order.PayTime = now;
            order.Logs.Add(new()
            {
                Action = LogAction.其他,
                CreateTime = now
            });

            await _orderRepository.Update(order);
        }

        /// <summary>
        /// 處理綠界取號結果
        /// </summary>
        /// <param name="response">綠界 ATM 取號回傳資料</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task HandleTakeNumberResult(EcpayAtmTakeNumberResponse response)
        {
            var methodName = nameof(HandleTakeNumberResult);

            _logger.LogInformation($"{methodName}, ecpay take number response: {JsonSerializer.Serialize(response)}");

            if (!ValidMacValue(response, _ecPaySettings.Payment.HashKey, _ecPaySettings.Payment.HashIV, _paymentHashAlgorithm, out var checkMacValue))
            {
                _logger.LogError($"{methodName} CheckMacValue invalid, check mac value: {checkMacValue}");
                return;
            }

            var order = await GetOrder(response.MerchantTradeNo);
            var now = DateTime.Now;

            order.TradeNo = response.TradeNo;
            order.UpdateTime = now;

            // 目前只有串 ATM，僅處理 ATM 規則
            if (response.RtnCode != 2)
            {
                order.TradeNo = response.TradeNo;
                await _orderRepository.Update(order);
                throw new Exception($"ecpay ATM RtnCode error, RtnCode: {response.RtnCode}");
            }

            using (var reader = new StreamReader(Path.Combine(_env.WebRootPath, "taiwan_banks.json"), Encoding.Default))
            {
                var banks = JsonSerializer.Deserialize<List<Bank>>(reader.ReadToEnd());

                order.PayDeadline = DateTime.Parse(response.ExpireDate);
                //order.AtmInfo = new OrderAtmInfo
                //{
                //    Code = response.BankCode,
                //    Name = banks.FirstOrDefault(x => x.BankCode == response.BankCode)?.Name ?? "",
                //    VirtualAccount = response.vAccount,
                //    CreateTime = DateTime.Parse(response.TradeDate)
                //};

                await _orderRepository.Update(order);
            }
        }

        /// <summary>
        /// 取得訂單
        /// </summary>
        /// <param name="merchantTradeNo">特店訂單編號</param>
        /// <returns></returns>
        /// <exception cref="Exception">查無此訂單</exception>
        private async Task<Order> GetOrder(string merchantTradeNo)
        {
            var order = await _orderRepository.Get(
                x => x.MerchantTradeNo == merchantTradeNo,
                x => x.Include(y => y.Member)
                      .Include(y => y.Products)
            );

            if (order is null)
                throw new Exception($"{nameof(GetOrder)}, 查無此訂單");

            return order;
        }

        /// <summary>
        /// 轉換綠界反饋內容
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">feedback error</exception>
        private EcPayResponse ConvertEcPayFeedback(string hashKey, string hashIV, string merchantId)
        {
            Hashtable feedback = null;
            var errors = new List<string>();
            using (var payment = new AllInOne())
            {
                payment.HashKey = hashKey;
                payment.HashIV = hashIV;
                payment.MerchantID = merchantId;

                // 取回付款結果
                errors.AddRange(payment.CheckOutFeedback(ref feedback));
            }

            if (errors.Any())
                throw new Exception($"{nameof(ConvertEcPayFeedback)} feedback error: {string.Join(", ", feedback)}");

            var response = new EcPayResponse();
            var responseType = typeof(EcPayResponse);

            foreach (string key in feedback.Keys)
            {
                var property = responseType.GetProperty(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
                property.SetValue(response, feedback[key]);
            }

            return response;
        }

        /// <summary>
        /// 驗證 mac value
        /// </summary>
        /// <param name="response">綠界金流回傳資料</param>
        /// <param name="checkMacValue">checkMacValue</param>
        /// <returns></returns>
        private bool ValidMacValue(EcPayDefaultResponse response, string hashKey, string hashIV, string hashAlgorithm, out string checkMacValue)
        {
            var dic = response
                .GetType()
                .GetProperties()
                .Where(x => x.Name != nameof(response.CheckMacValue) && (x.Name != "TWQRTradeNo" || x.GetValue(response) != null))
                .ToDictionary(x => x.Name, x => x.GetValue(response)?.ToString() ?? "");

            var sortedDic = new SortedDictionary<string, string>(dic);
            checkMacValue = ComposeMacValue(sortedDic, hashKey, hashIV, hashAlgorithm);
            return response.CheckMacValue == checkMacValue;
        }

        /// <summary>
        /// 取得門市地圖網址
        /// </summary>
        /// <param name="logisticsSubType">物流子類型</param>
        /// <returns></returns>
        public string GetCVSMapUrl(LogisticsSubTypes logisticsSubType)
        {
            using (var logistics = new LogisticsProvider())
            {
                var request = _httpContext.Request;

#if DEBUG
                // 測試參數
                logistics.ServiceURL = "https://logistics-stage.ecpay.com.tw/Express/map";
                logistics.HashKey = "XBERn1YOvpM9nfZc";
                logistics.HashIV = "5294y06JbISpM5x9";
                logistics.MerchantID = "2000933";
#else
                // 正式參數
                logistics.ServiceURL = ServiceDomain.CVSMAP;
                logistics.HashKey = _ecPaySettings.Logistics.HashKey;
                logistics.HashIV = _ecPaySettings.Logistics.HashIV;
                logistics.MerchantID = _ecPaySettings.Logistics.MerchantId;
#endif

                // 特店交易編號
                //logistics.Send.MerchantTradeNo = string.Empty;
                // *物流類型
                logistics.Send.LogisticsType = LogisticsTypes.CVS;
                // *物流子類型
                logistics.Send.LogisticsSubType = logisticsSubType;
                // *是否代收貨款
                logistics.Send.IsCollection = IsCollections.NO;
                // *選擇的店鋪資訊返回網址
                logistics.Send.ServerReplyURL = $"{request.Scheme}://{request.Host}/Bag/Detail";

                // 額外資訊，供傳遞時保留資訊會原值回傳
                //logistics.Send.ExtraData = string.Empty;
                // 啟動 log，會在帶入的路徑產生 log 檔案
                //logistics.EnableLogging(string.Empty);

                var response = logistics.GetCvsMapURL();

                if (!response.IsSuccess)
                {
                    _logger.LogError($"{nameof(GetCVSMapUrl)}, 取得 {logisticsSubType} 電子地圖連結失敗 response: {JsonSerializer.Serialize(response)}");
                    return string.Empty;
                }

                return response.Data;
            }
        }

        /// <summary>
        /// 建立物流訂單
        /// </summary>
        /// <param name="order">訂單資訊</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task CreateLogisticsOrder(Order order)
        {
            var methodName = nameof(CreateLogisticsOrder);
            var c2cLogisticsSubTypes = new List<LogisticsSubTypes> { LogisticsSubTypes.FAMIC2C, LogisticsSubTypes.UNIMARTC2C };
            var request = _httpContext.Request;
            var logisticsSubType = (LogisticsSubTypes)order.ShippingMethod;

#if DEBUG
            // 測試參數
            var merchantId = logisticsSubType == LogisticsSubTypes.TCAT ? "2000132" : "2000933";
            var hashKey = logisticsSubType == LogisticsSubTypes.TCAT ? "5294y06JbISpM5x9" : "XBERn1YOvpM9nfZc";
            var hashIV = logisticsSubType == LogisticsSubTypes.TCAT ? "v77hoKGq4kWxNNIS" : "h1ONHk4P4yqbl5LK";
#else
            // 正式參數
            var merchantId = _ecPaySettings.Logistics.MerchantId;
            var hashKey = _ecPaySettings.Logistics.HashKey;
            var hashIV = _ecPaySettings.Logistics.HashIV;
#endif

            var requestDict = new SortedDictionary<string, string>
            {
                // *特店代碼
                //{ "MerchantID", _ecPaySettings.MerchantId },
                { "MerchantID", merchantId },
                // *特店交易編號 (同個訂單每次特店訂單編號不可重複)
                { "MerchantTradeNo", order.SeqNo },
                // *特店交易時間
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                // *物流子類型
                { "LogisticsSubType", logisticsSubType.ToString() },
                // *是否代收貨款
                { "IsCollection", "N" },
                // 商品名稱
                { "GoodsName", $"双美商品{order.Products.Sum(x => x.Quantity)}件" },
                // *商品金額
                { "GoodsAmount", decimal.ToInt32(order.SubTotal).ToString() },
                // *寄件人姓名
                { "SenderName", string.Empty },
                // *收件人姓名
                { "ReceiverName", string.Empty },
                // *收件人手機
                { "ReceiverCellPhone", string.Empty },
                // *物流狀態通知返回網址 
                { "ServerReplyURL", $"{_domain}/Admin/Order/EcPayLogisticsStatusNotification" },
            };

            SetLogisticsSpecification(requestDict, order.ThreeDimensionsTotal);

            if (logisticsSubType == LogisticsSubTypes.TCAT)
            {
                // *物流類型
                requestDict.Add("LogisticsType", LogisticsTypes.HOME.ToString());
                // 寄件人電話
                requestDict.Add("SenderPhone", string.Empty);
                // *寄件人郵遞區號
                requestDict.Add("SenderZipCode", string.Empty);
                // *寄件人地址
                requestDict.Add("SenderAddress", string.Empty);
                // *收件者郵遞區號
                requestDict.Add("ReceiverZipCode", string.Empty);
                // *收件者地址
                requestDict.Add("ReceiverAddress", string.Empty);
                // *預定取件時段 (4: 不限時)
                requestDict.Add("ScheduledPickupTime", "4");
            }
            else if (c2cLogisticsSubTypes.Contains(logisticsSubType))
            {
                // *收件人門市代號
                requestDict.Add("ReceiverStoreID", string.Empty);
                // 寄件人電話
                requestDict.Add("SenderPhone", string.Empty);
                // 寄件人手機
                requestDict.Add("SenderCellPhone", string.Empty);
                // *物流類型
                requestDict.Add("LogisticsType", LogisticsTypes.CVS.ToString());
            }
            else
            {
                throw new NotImplementedException("不支援的配送方式");
            }

            // Compose mac value
#if DEBUG
            var macValue = ComposeMacValue(requestDict, hashKey, hashIV, _logisticsHashAlgorithm);
#else
            var macValue = ComposeMacValue(requestDict, _ecPaySettings.Logistics.HashKey, _ecPaySettings.Logistics.HashIV, _logisticsHashAlgorithm);
#endif

            requestDict.Add("CheckMacValue", macValue);

            var (success, response) = await _callApiService.Post<ECPay.SDK.Logistics.Response<string>>(_ecPaySettings.LogisticsUrl, requestDict);
            var now = DateTime.Now;

            if (!success || response is null || response.Code != "1")
            {
                var errorMessage = $"建立物流訂單失敗 response: {response?.Data}";
                _logger.LogError($"{methodName}, {errorMessage}");
                await _orderRepository.Update(order);
                throw new Exception(errorMessage);
            }

            order.Status = OrderStatus.出貨中;
            order.UpdateTime = now;
            //order.Shipment = new Shipment
            //{
            //    State = ShipmentStatus.已配貨,
            //    LogisticsCode = response.DictionData["AllPayLogisticsID"],
            //    CvsDeliveryNo = response.DictionData["CVSPaymentNo"],
            //    CvsValidationNo = response.DictionData["CVSValidationNo"],
            //    BookingNote = response.DictionData["BookingNote"],
            //    CreateTime = now
            //};

            order.Logs.Add(new()
            {
                Action = LogAction.其他,
                CreateTime = now,
            });

            await _orderRepository.Update(order);
            //await SaveLog(PermissionType.W, LogType.Create, $"建立物流訂單 order id: {order.Id}", now);
        }

        /// <summary>
        /// 處理綠界物流狀態通知
        /// </summary>
        /// <param name="response">綠界物流狀態回傳資料</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task HandleLogisticsStatusNotification(EcpayLogisticsNotificationResponse response)
        {
            var methodName = nameof(HandleLogisticsStatusNotification);

            _logger.LogInformation($"{methodName}, ecpay logistics status notification response: {JsonSerializer.Serialize(response)}");

#if DEBUG
            var hashKey = response.LogisticsSubType == LogisticsSubTypes.TCAT.ToString() ? "5294y06JbISpM5x9" : "XBERn1YOvpM9nfZc";
            var hashIV = response.LogisticsSubType == LogisticsSubTypes.TCAT.ToString() ? "v77hoKGq4kWxNNIS" : "h1ONHk4P4yqbl5LK";
#else
            var hashKey = _ecPaySettings.Logistics.HashKey;
            var hashIV = _ecPaySettings.Logistics.HashIV;
#endif

            if (!ValidMacValue(response, hashKey, hashIV, _logisticsHashAlgorithm, out var checkMacValue))
            {
                _logger.LogError($"{methodName} CheckMacValue invalid, check mac value: {checkMacValue}");
                return;
            }

            if (!Enum.TryParse<LogisticsSubTypes>(response.LogisticsSubType, true, out var logisticsSubType))
                throw new NotImplementedException("不支援的物流子狀態");

            var order = await _orderRepository.Get(
                x => x.MerchantTradeNo == response.MerchantTradeNo,
                x => x.Include(x => x.Member)
                      .Include(x => x.Products)
                    //.Include(x => x.ReceiptPerson)
                    //.Include(x => x.Shipment)
            );

            //if (order is null || order.OrderShipment is null)
            //    throw new Exception("查無對應的出貨單");

            //switch (logisticsSubType)
            //{
            //    case LogisticsSubTypes.TCAT:
            //        if (response.RtnCode == 3001)
            //            SetShipmentState(OrderShipmentState.已出貨);
            //        else if (response.RtnCode == 3003)
            //            SetShipmentState(OrderShipmentState.已送達);

            //        break;
            //    case LogisticsSubTypes.FAMIC2C:
            //        if (response.RtnCode == 3024)
            //            SetShipmentState(OrderShipmentState.已出貨);
            //        else if (response.RtnCode == 3018)
            //            SetShipmentState(OrderShipmentState.已送達);
            //        else if (response.RtnCode == 3022)
            //            SetShipmentState(OrderShipmentState.已取貨);
            //        else if (response.RtnCode == 3020)
            //            SetShipmentState(OrderShipmentState.未取貨);

            //        break;
            //    case LogisticsSubTypes.UNIMARTC2C:
            //        if (response.RtnCode == 2030)
            //            SetShipmentState(OrderShipmentState.已出貨);
            //        else if (response.RtnCode == 2073)
            //            SetShipmentState(OrderShipmentState.已送達);
            //        else if (response.RtnCode == 2067)
            //            SetShipmentState(OrderShipmentState.已取貨);
            //        else if (response.RtnCode == 2074)
            //            SetShipmentState(OrderShipmentState.未取貨);

            //        break;
            //    default:
            //        throw new NotImplementedException("不支援的物流子狀態");
            //}

            //void SetShipmentState(OrderShipmentState shipmentState)
            //{
            //    var now = DateTime.Now;
            //    order.OrderShipment.Status = (int)shipmentState;
            //    order.OrderShipment.ReviseTime = now;

            //    switch (shipmentState)
            //    {
            //        case OrderShipmentState.已出貨:
            //            order.OrderShipment.ShipTime = DateTime.Parse(response.UpdateStatusDate);
            //            order.OrderLogs.Add(new OrderLog
            //            {
            //                State = OrderLogState.已出貨.ToInt(),
            //                CreateTime = now,
            //            });
            //            break;
            //        case OrderShipmentState.已送達:
            //            order.OrderShipment.ArrivalTime = DateTime.Parse(response.UpdateStatusDate);
            //            order.OrderLogs.Add(new OrderLog
            //            {
            //                State = OrderLogState.已送達.ToInt(),
            //                CreateTime = now,
            //            });
            //            break;
            //        case OrderShipmentState.已取貨:
            //            order.OrderShipment.PickUpTime = DateTime.Parse(response.UpdateStatusDate);
            //            order.OrderLogs.Add(new OrderLog
            //            {
            //                State = OrderLogState.已取貨.ToInt(),
            //                CreateTime = now,
            //            });
            //            break;
            //        case OrderShipmentState.未取貨:
            //            order.OrderShipment.CancelTime = DateTime.Parse(response.UpdateStatusDate);
            //            order.Status = OrderState.待退款.ToInt();
            //            order.CancelType = CancelType.系統自動取消.ToInt();
            //            order.CancelReason = CancelReason.未取貨.ToInt();
            //            order.ReviseTime = now;
            //            order.OrderLogs.Add(new OrderLog
            //            {
            //                State = OrderLogState.未取貨.ToInt(),
            //                CreateTime = now,
            //            });
            //            break;
            //        default:
            //            break;
            //    }
            //}
        }

        /// <summary>
        /// 建立逆物流訂單
        /// </summary>
        /// <param name="request">請求資料</param>
        /// <param name="order">訂單資料</param>
        /// <returns></returns>
        /// <exception cref="ForbiddenException">系統無法判斷該地址，請確認地址是否填寫正確</exception>
        public async Task CreateReturnedLogisticsOrder(ReturnOrderRequest request, Order order)
        {
            var methodName = nameof(CreateReturnedLogisticsOrder);

#if DEBUG
            // 測試參數
            var merchantId = "2000132";
            var hashKey = "5294y06JbISpM5x9";
            var hashIV = "v77hoKGq4kWxNNIS";
#else
            // 正式參數
            var merchantId = _ecPaySettings.Logistics.MerchantId;
            var hashKey = _ecPaySettings.Logistics.HashKey;
            var hashIV = _ecPaySettings.Logistics.HashIV;
#endif

            var requestDict = new SortedDictionary<string, string>
            {
                // *特店代碼
                //{ "MerchantID", _ecPaySettings.MerchantId },
                { "MerchantID", merchantId },
                // 綠界科技的物流交易編號
                //{ "AllPayLogisticsID", order.Shipment.LogisticsCode },
                // *物流子類型
                { "LogisticsSubType", LogisticsSubTypes.TCAT.ToString() },
                // *退貨人姓名
                { "SenderName", request.Name },
                // *退貨人手機
                { "SenderCellPhone", request.CellPhone },
                // *退貨人郵遞區號
                { "SenderZipCode", request.PostalCode },
                // *退貨人地址
                { "SenderAddress", request.Address },
                // *收件人姓名
                { "ReceiverName", string.Empty },
                // *收件人手機
                { "ReceiverPhone", string.Empty },
                // *收件人手機
                { "ReceiverCellPhone", string.Empty },
                // *收件人郵遞區號
                { "ReceiverZipCode", string.Empty },
                // *收件人地址
                { "ReceiverAddress", string.Empty },
                // *收件人 email
                { "ReceiverEmail", "service@sunmaxaesthetics.com" },
                // *商品金額
                { "GoodsAmount", decimal.ToInt32(order.SubTotal).ToString() },
                // *預定取件時段
                { "ScheduledPickupTime", "4" },
                // *預定送達時段
                { "ScheduledDeliveryTime", "4" },
                // *物流狀態通知返回網址 
                { "ServerReplyURL", $"{_domain}/Order/EcPayReturnedLogisticsStatusNotification" },
            };

            SetLogisticsSpecification(requestDict, order.ThreeDimensionsTotal);

            // Compose mac value
#if DEBUG
            var macValue = ComposeMacValue(requestDict, hashKey, hashIV, _logisticsHashAlgorithm);
#else
            var macValue = ComposeMacValue(requestDict, _ecPaySettings.Logistics.HashKey, _ecPaySettings.Logistics.HashIV, _logisticsHashAlgorithm);
#endif

            requestDict.Add("CheckMacValue", macValue);

            var now = DateTime.Now;
            var (success, response) = await _callApiService.Post<ECPay.SDK.Logistics.Response<string>>(_ecPaySettings.ReturnedLogisticsUrl, requestDict);

            if (!success || response is null || response.Code != "1")
            {
                var errorMessage = $"建立逆物流訂單失敗 response: {response?.Data}";
                _logger.LogError($"{methodName}, {errorMessage}");
                await _orderRepository.Update(order);

                if (response?.Code == "10500058")
                    throw new ForbiddenException("系統無法判斷該地址，請確認地址是否填寫正確");

                throw new ForbiddenException(errorMessage);
            }

            order.Status = OrderStatus.退貨中;
            //order.ReturnInfo = new()
            //{
            //    State = OrderReturnStatus.待通知物流.ToInt(),
            //    Reason = request.Reason,
            //    Description = request.Description,
            //    RefundAmount = order.TotalAmount,
            //    Coins = order.UseCoins,
            //    CreateTime = now,
            //    OrderReturnPerson = new OrderReturnPerson
            //    {
            //        Name = request.Name,
            //        Gender = request.Gender,
            //        CellPhone = request.CellPhone,
            //        PhoneArea = request.PhoneArea,
            //        PhoneNum = request.PhoneNum,
            //        PhoneSub = request.PhoneSub,
            //        PostalCode = request.PostalCode,
            //        Address = request.Address,
            //        CreateTime = now
            //    },
            //};

            //if (order.PaymentMethod == PaymentMethod.ATM.ToInt())
            //{
            //    order.ReturnInfo.OrderReturnAtmInfo = new OrderReturnAtmInfo
            //    {
            //        Code = request.BankCode,
            //        SubCode = request.BankSubCode,
            //        AccountName = request.BankAccountName,
            //        Account = request.BankAccount
            //    };            
            //}

            await _orderRepository.Update(order);
        }

        /// <summary>
        /// 處理逆物流狀態通知
        /// </summary>
        /// <param name="response">綠界逆物流狀態回傳資料</param>
        /// <returns></returns>
        /// <exception cref="Exception">查無對應的退貨單</exception>
        public async Task HandleReturnedLogisticsStatusNotification(EcpayReturnedLogisticsNotificationResponse response)
        {
            var methodName = nameof(HandleReturnedLogisticsStatusNotification);

            _logger.LogInformation($"{methodName}, ecpay returned logistics status notification response: {JsonSerializer.Serialize(response)}");

#if DEBUG
            var hashKey = "5294y06JbISpM5x9";
            var hashIV = "v77hoKGq4kWxNNIS";
#else
            var hashKey = _ecPaySettings.Logistics.HashKey;
            var hashIV = _ecPaySettings.Logistics.HashIV;
#endif

            if (!ValidMacValue(response, hashKey, hashIV, _logisticsHashAlgorithm, out var checkMacValue))
            {
                _logger.LogError($"{methodName} CheckMacValue invalid, check mac value: {checkMacValue}");
                return;
            }

            var order = await _orderRepository.Get(
                //x => x.Shipment.LogisticsCode == response.AllPayLogisticsID,
                include: x => x.Include(x => x.ReturnInfo)
                //x => x.Include(x => x.Shipment)
            );

            if (order is null || order.ReturnInfo is null)
                throw new Exception("查無對應的退貨單");

            var now = DateTime.Now;

            if (response.RtnCode == 325)
            {
                order.ReturnInfo.Status = OrderReturnStatus.物流收貨中;
                order.ReturnInfo.BookingNote = response.BookingNote;
                //order.ReturnInfo.LogisticsTime = now;
                //order.ReturnInfo.ReviseTime = now;
            }
            else if (response.RtnCode == 5008)
            {
                order.ReturnInfo.Status = OrderReturnStatus.盤點退貨商品;
                //order.ReturnInfo.CheckTime = now;
                //order.ReturnInfo.ReviseTime = now;
            }
            // ToDo 其他狀態
            else
            {
                return;
            }

            await _orderRepository.Update(order);
        }

        /// <summary>
        /// 建立發票
        /// </summary>
        /// <param name="order">訂單資訊</param>
        /// <returns></returns>
        public async Task CreateInvoice(Order order)
        {
            var items = order.Products.Select((item, index) => new EcPayInvoiceIssueRequest.EcPayInvoiceItem
            {
                ItemSeq = index + 1,
                ItemName = item.Name,
                ItemCount = item.Quantity,
                ItemTaxType = "1",
                ItemWord = "件",
                ItemPrice = item.Price,
                ItemAmount = item.Price * item.Quantity
            }).ToList();

            //if (order.CoinDiscount != 0)
            //{
            //    items.Add(new EcPayInvoiceIssueRequest.EcPayInvoiceItem
            //    {
            //        ItemSeq = items.Count + 1,
            //        ItemName = $"優惠券",
            //        ItemCount = 1,
            //        ItemTaxType = "1",
            //        ItemWord = "件",
            //        ItemPrice = -order.CoinDiscount,
            //        ItemAmount = -order.CoinDiscount * 1
            //    });
            //}

            //if (order.ShippingFee != 0)
            //{
            //    items.Add(new EcPayInvoiceIssueRequest.EcPayInvoiceItem
            //    {
            //        ItemSeq = items.Count + 1,
            //        ItemName = $"運費",
            //        ItemCount = 1,
            //        ItemTaxType = "1",
            //        ItemWord = "件",
            //        ItemPrice = order.ShippingFee,
            //        ItemAmount = order.ShippingFee * 1
            //    });
            //}

            var invoiceIssue = new EcPayInvoiceIssueRequest
            {
                MerchantID = _ecPaySettings.Invoice.MerchantId,
                RelateNumber = order.SeqNo,
                CarrierType = order.CarrierType.GetDescription(),
                Print = "0",
                Donation = "0",
                CustomerPhone = string.Empty,
                SalesAmount = decimal.ToInt64(items.Sum(x => x.ItemAmount)),
                SpecialTaxType = (int)SpecialTaxTypeEnum.None,
                vat = "1",
                TaxType = "1",
                InvType = "07",
                Items = items
            };

            switch (order.CarrierType)
            {
                case CarrierType.None:
                    invoiceIssue.CarrierNum = string.Empty;
                    invoiceIssue.CustomerIdentifier = order.UniformNumber;
                    break;
                case CarrierType.NaturalPersonEvidence:
                    invoiceIssue.CarrierNum = order.CitizenDigitalCertificate;
                    break;
                case CarrierType.PhoneBarcode:
                    invoiceIssue.CarrierNum = order.CarrierNumber;
                    break;
                default:
                    break;
            }

            var request = new EcPayDefaultRequest
            {
                PlatformID = "",
                MerchantID = _ecPaySettings.Invoice.MerchantId,
                RqHeader = new EcPayDefaultRequest.EcPayHeader
                {
                    Timestamp = DateTime.Now.ToTimestamp()
                },
                Data = HashHelper.AESEncrypt(_ecPaySettings.Invoice.HashKey, _ecPaySettings.Invoice.HashIV, JsonSerializer.Serialize(invoiceIssue))
            };

            var (success, response) = await _callApiService.Post<EcPayDefaultRequest, EcPayInvoiceDefaultResponse>(_ecPaySettings.B2CInvoiceIssueUrl, request);

            if (!success || response.TransCode != 1)
            {
                await AddErrorLog(order.SeqNo, response.TransMsg);
                return;
            }

            var responseDataJson = HttpUtility.UrlDecode(HashHelper.AESDecrypt(_ecPaySettings.Invoice.HashKey, _ecPaySettings.Invoice.HashIV, response.Data));
            var data = JsonSerializer.Deserialize<EcPayInvoiceDataResponse>(responseDataJson);

            if (data.RtnCode != 1)
            {
                await AddErrorLog(order.SeqNo, data.RtnMsg);
                return;
            }

            var invoiceDate = DateTime.Parse(data.InvoiceDate);
            var sortedDic = new SortedDictionary<string, string>
            {
                { "InvoiceNumber", data.InvoiceNo },
                { "MerchantID", _ecPaySettings.Invoice.MerchantId },
                { "RandomNumber", data.RandomNumber },
                { "StartDate", invoiceDate.ToString("yyyy-MM-dd") },
            };

            await _orderInvoiceRepository.Insert(new()
            {
                OrderId = order.Id,
                Number = data.InvoiceNo,
                RandomNumber = data.RandomNumber,
                StartDate = invoiceDate,
                CheckMacValue = ComposeMacValue(sortedDic, _ecPaySettings.Invoice.HashKey, _ecPaySettings.Invoice.HashIV, "MD5")
            });
        }

        /// <summary>
        /// 作廢發票
        /// </summary>
        /// <returns></returns>
        public async Task CancelInvoice(string orderSeqNo)
        {

            var order = await _orderRepository.Get(
                x => x.SeqNo == orderSeqNo,
                x => x.Include(y => y.UniformInvoice)
                //x => x.Include(y => y.ReturnInfo)
            );

            if (order is null || order.UniformInvoice is null)
                throw new Exception("查無此訂單或發票資訊");

            if (order.Status != OrderStatus.退貨中 || order.ReturnInfo.Status != OrderReturnStatus.待退款)
                throw new Exception("當前狀態不可作廢發票");

            var invoiceInvalid = new EcPayInvoiceInvalidRequest
            {
                MerchantID = _ecPaySettings.Invoice.MerchantId,
                InvoiceNo = order.UniformInvoice.Number,
                InvoiceDate = order.UniformInvoice.StartDate.ToString("yyyy-MM-dd"),
                Reason = "退貨發票作廢"
            };

            var request = new EcPayDefaultRequest
            {
                PlatformID = "",
                MerchantID = _ecPaySettings.Invoice.MerchantId,
                RqHeader = new EcPayDefaultRequest.EcPayHeader
                {
                    Timestamp = DateTime.Now.ToTimestamp()
                },
                Data = HashHelper.AESEncrypt(_ecPaySettings.Invoice.HashKey, _ecPaySettings.Invoice.HashIV, JsonSerializer.Serialize(invoiceInvalid))
            };

            var (success, response) = await _callApiService.Post<EcPayDefaultRequest, EcPayInvoiceDefaultResponse>(_ecPaySettings.B2CInvoiceInvalidUrl, request);

            if (!success || response is null || response.TransCode != 1)
            {
                await AddErrorLog(orderSeqNo, response?.TransMsg);
                return;
            }

            var responseDataJson = HttpUtility.UrlDecode(HashHelper.AESDecrypt(_ecPaySettings.Invoice.HashKey, _ecPaySettings.Invoice.HashIV, response.Data));
            var data = JsonSerializer.Deserialize<EcPayInvoiceDataResponse>(responseDataJson);

            if (data.RtnCode != 1)
            {
                await AddErrorLog(orderSeqNo, data.RtnMsg);
                return;
            }

            order.UniformInvoice.Status = OrderInvoiceStatus.已作廢;
            //order.ReturnInfo.CheckFinishTime = DateTime.Now;
            await _orderRepository.Update(order);
            return;
        }

        /// <summary>
        /// 取得公司名稱 by 統一編號
        /// </summary>
        /// <param name="uniformNumber">統一編號</param>
        /// <returns></returns>
        public async Task<bool> GetCompanyNameByTaxId(string uniformNumber)
        {
            if (string.IsNullOrEmpty(uniformNumber))
                return false;

            _logger.LogInformation($"{nameof(GetCompanyNameByTaxId)}, 統一編號: {uniformNumber}");

            var model = new
            {
                MerchantID = _ecPaySettings.Invoice.MerchantId,
                UnifiedBusinessNo = uniformNumber
            };

            var request = new EcPayDefaultRequest
            {
                PlatformID = "",
                MerchantID = _ecPaySettings.Invoice.MerchantId,
                RqHeader = new EcPayDefaultRequest.EcPayHeader
                {
                    Timestamp = DateTime.Now.ToTimestamp()
                },
                Data = HashHelper.AESEncrypt(_ecPaySettings.Invoice.HashKey, _ecPaySettings.Invoice.HashIV, JsonSerializer.Serialize(model))
            };

            var (success, response) = await _callApiService.Post<EcPayDefaultRequest, EcPayInvoiceDefaultResponse>(_ecPaySettings.GetCompanyNameByTaxIdUrl, request);

            if (!success || response is null || response.TransCode != 1)
                return false;

            var responseDataJson = HttpUtility.UrlDecode(HashHelper.AESDecrypt(_ecPaySettings.Invoice.HashKey, _ecPaySettings.Invoice.HashIV, response.Data));
            var data = JsonSerializer.Deserialize<EcPayGetCompanyNameResponse>(responseDataJson);

            return data.RtnCode == 1 && !string.IsNullOrWhiteSpace(data.CompanyName);
        }

        /// <summary>
        /// 檢查手機條碼是否存在
        /// </summary>
        /// <param name="phoneCarrier">手機條碼</param>
        /// <returns></returns>
        public async Task<bool> CheckBarcode(string phoneCarrier)
        {
            if (string.IsNullOrEmpty(phoneCarrier))
                return false;

            _logger.LogInformation($"{nameof(CheckBarcode)}, 手機條碼: {phoneCarrier}");

            var model = new
            {
                MerchantID = _ecPaySettings.Invoice.MerchantId,
                BarCode = phoneCarrier
            };

            var request = new EcPayDefaultRequest
            {
                PlatformID = "",
                MerchantID = _ecPaySettings.Invoice.MerchantId,
                RqHeader = new EcPayDefaultRequest.EcPayHeader
                {
                    Timestamp = DateTime.Now.ToTimestamp()
                },
                Data = HashHelper.AESEncrypt(_ecPaySettings.Invoice.HashKey, _ecPaySettings.Invoice.HashIV, JsonSerializer.Serialize(model))
            };

            var (success, response) = await _callApiService.Post<EcPayDefaultRequest, EcPayInvoiceDefaultResponse>(_ecPaySettings.CheckBarcodeUrl, request);

            if (!success || response is null || response.TransCode != 1)
                return false;

            var responseDataJson = HttpUtility.UrlDecode(HashHelper.AESDecrypt(_ecPaySettings.Invoice.HashKey, _ecPaySettings.Invoice.HashIV, response.Data));
            var data = JsonSerializer.Deserialize<EcPayCheckBarcodeResponse>(responseDataJson);

            return data?.RtnCode == 1 && data.IsExist.Equals("Y", StringComparison.OrdinalIgnoreCase);
        }
    }
}