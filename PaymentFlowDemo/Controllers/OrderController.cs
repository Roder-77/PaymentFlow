using ECPay.Payment.Integration;
using Newtonsoft.Json;
using NLog;
using PaymentFlowDemo.Enums;
using SocialChat.Helpers.Crypto;
using SocialChat.Helpers.Hash;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SocialChat.Areas.Customer.Controllers
{
    public class OrderController : Controller
    {
        static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        static readonly Logger _ecPayLog = LogManager.GetLogger("ECPayLog");

        public ActionResult Index()
        {
            return View();
        }

        #region 藍新金流

        /// <summary>
        /// 藍新金流
        /// </summary>
        /// <param name="orderId">訂單 Id</param>
        /// <param name="paymentTypeId">支付方式 Id</param>
        /// <returns></returns>
        public ActionResult NewebPay(string orderId, string paymentTypeId)
        {
            try
            {
                _logger.Info($"{MethodBase.GetCurrentMethod().Name} started, orderId: {orderId}, paymentTypeId: {paymentTypeId}");

                // 取得付款方式
                var paymentType = GetPaymentType(paymentTypeId);
                // 藍新金流 Html
                var newebPayHtml = ComposeNewebPayHtml(orderId, "");

                _logger.Info($"{MethodBase.GetCurrentMethod().Name} ended, orderId: {orderId}, paymentTypeId: {paymentTypeId}");

                return Content(newebPayHtml);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, MethodBase.GetCurrentMethod().Name);

                // 導到錯誤提醒頁面
                return RedirectToAction("", "");
            }
        }

        /// <summary>
        /// 組成藍新金流 Html
        /// </summary>
        /// <param name="orderId">訂單編號</param>
        /// <param name="tradeVersionNo">交易版本編號</param>
        /// <returns></returns>
        public string ComposeNewebPayHtml(string orderId, string tradeVersionNo)
        {
            try
            {
                // 取得藍新金流相關參數
                var parameters = GetNewebPayParameters();
                // 訂單資料
                var order = GetOrder(orderId);
                // 商品資訊
                var itemDesc = "";
                // 商店代號
                var merchantId = "";
                // 串接程式版本
                var version = "";
                // 訂單Domain
                var domain = "";
                var notifyUrl = $"{domain}/Order/{nameof(OrderController.NewebPayCallback)}";
                var returnUrl = $"{domain}/Order/{nameof(OrderController.NewebPayReturn)}";

                var bodyDic = new Dictionary<string, string>
                {
                    // 商店代號
                    { "MerchantID", merchantId },
                    // 回傳格式
                    { "RespondType", "JSON" },
                    // 時間戳記
                    { "TimeStamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                    // 串接程式版本
                    { "Version", version },
                    // 商店訂單編號
                    { "MerchantOrderNo", $"{orderId}_{tradeVersionNo}" },
                    // 訂單金額
                    { "Amt", decimal.ToInt32(Convert.ToDecimal(order.curAmount)).ToString() },
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
                    { "MerchantID", merchantId }, // 商店代號
                    { "TradeInfo", trade.Item1 }, // 交易資料 AES 加密
                    { "TradeSha", trade.Item2 },  // 交易資料 SHA256 Hash
                    { "Version", version }        // 串接程式版本
                };

                // 回傳組好的 html
                return ComposeHtml();

                // 加密交易資訊，Item1：交易資料 AES 加密、Item2：交易資料 SHA256 Hash
                Tuple<string, string> EncryptTransactionInfo()
                {
                    var iv = parameters.Where(w => w.strPaymentParamCode == "HashIV").FirstOrDefault()?.strPaymentParamValue;
                    var key = parameters.Where(w => w.strPaymentParamCode == "HashKey").FirstOrDefault()?.strPaymentParamValue;

                    // query string
                    var queryBuilder = new StringBuilder();

                    foreach (var item in bodyDic)
                    {
                        queryBuilder.Append($"&{item.Key}={item.Value}");
                    }

                    // 將開頭 '&' 字元拔除
                    queryBuilder.Remove(0, 1);
                    // AES 加密
                    var aes = new AESCrypto(Encoding.UTF8.GetBytes(iv), Encoding.UTF8.GetBytes(key), 256, PaddingMode.PKCS7, CipherMode.CBC, "hex");
                    var tradeInfo = aes.Encrypt(queryBuilder.ToString()).ToLower();
                    // SHA256 Hash
                    var sha256 = new SHA256Hash();
                    var hashPayload = new StringBuilder();

                    hashPayload.Append($"HashKey={key}");
                    hashPayload.Append($"&{tradeInfo}");
                    hashPayload.Append($"&HashIV={iv}");

                    var tradeSha = sha256.Generate(hashPayload.ToString()).ToUpper();

                    return Tuple.Create(tradeInfo, tradeSha);
                }

                // 組成 Html
                string ComposeHtml()
                {
                    var htmlBuilder = new StringBuilder();

                    htmlBuilder.Append("<html><body>").AppendLine();
                    htmlBuilder.Append($"<form id='newebPayForm' method='POST' action='{parameters.Where(w => w.strPaymentParamCode == "SubmitURL").FirstOrDefault()?.strPaymentParamValue}'>").AppendLine();

                    foreach (var item in requestDic)
                    {
                        htmlBuilder.Append($"<input type='hidden' name='{item.Key}' value='{item.Value}'>").AppendLine();
                    }

                    htmlBuilder.Append("</form>").AppendLine();
                    htmlBuilder.Append("<script>const newebPayForm = document.getElementById('newebPayForm'); if(newebPayForm) newebPayForm.submit();</script>");
                    htmlBuilder.Append("<body><html>");

                    return htmlBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, MethodBase.GetCurrentMethod().Name);
                throw;
            }
        }

        /// <summary>
        /// 藍新金流 Callback
        /// </summary>
        /// <param name="request">回傳資料</param>
        /// <returns></returns>
        public ActionResult NewebPayCallback(NewebCallbackModel request)
        {
            try
            {
                _logger.Debug($"藍新金流Return: {JsonConvert.SerializeObject(request)}");

                // 取得藍新金流相關參數
                var parameters = GetNewebPayParameters();
                var aesKey = "HashKey";
                var aesIV = "HashIV";

                // 將TradeInfo解密
                var aesService = new AESCrypto(Encoding.UTF8.GetBytes(aesIV), Encoding.UTF8.GetBytes(aesKey), 256, PaddingMode.PKCS7, CipherMode.CBC, "hex");
                var tradeInfo = JsonConvert.DeserializeObject<NewebTradeInfoModel>(aesService.Decrypt(request.TradeInfo));

                _logger.Info($"藍新金流Return: Status = {tradeInfo.Status}, Message = {tradeInfo.Message}, MerchantOrderNo = {tradeInfo.Result.MerchantOrderNo}");

                var orderId = tradeInfo.Result.MerchantOrderNo.Split('_')[0];

                if (tradeInfo.Status == "SUCCESS")
                {
                    // 記錄交易成功，另開 Thread 處理避免超時藍新重複發送
                    Task.Run(async () => await PaySucceed(orderId, PaymentType.Blue, tradeInfo.Message));
                }
                else
                {
                    // Log 紀錄交易失敗
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"藍新金流接收回傳結果失敗 {MethodBase.GetCurrentMethod().Name}");
            }

            return Content(string.Empty);
        }

        /// <summary>
        /// 藍新金流Return
        /// </summary>
        /// <param name="request">回傳資料</param>
        /// <returns></returns>
        public ActionResult NewebPayReturn(NewebCallbackModel request)
        {
            try
            {

                // 取得藍新金流相關參數
                var parameters = GetNewebPayParameters();
                var aesKey = "HashKey";
                var aesIV = "HashIV";

                // 將TradeInfo解密
                var aesService = new AESCrypto(Encoding.UTF8.GetBytes(aesIV), Encoding.UTF8.GetBytes(aesKey), 256, PaddingMode.PKCS7, CipherMode.CBC, "hex");
                var tradeInfo = JsonConvert.DeserializeObject<NewebTradeInfoModel>(aesService.Decrypt(request.TradeInfo));

                var orderId = tradeInfo.Result.MerchantOrderNo.Split('_')[0];

                // 取得訂單資料
                var order = GetOrder(orderId);

                // 訂單詳情
                return RedirectToAction("", "");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, MethodBase.GetCurrentMethod().Name);

                // 導到錯誤提醒頁面
                return RedirectToAction("", "");
            }
        }
        #endregion

        #region 綠界
        /// <summary>
        /// 綠界支付
        /// 產生訂單
        /// </summary>
        /// <param name="orderId">訂單Id</param>
        /// <param name="paymentTypeId">付款方式Id</param>
        /// <param name="installment">刷卡分期 期數</param>
        /// <returns></returns>
        public ActionResult ECPay(string orderId, string paymentTypeId, int installment = 1)
        {
            try
            {
                _logger.Info($"ECPay started, orderId: {orderId}, paymentTypeId: {paymentTypeId}");

                // 取得付款方式
                var paymentType = GetPaymentType(paymentTypeId);

                // 取得參數 (HashKey、HashIV...等)
                var parameters = GetECPayParameters();

                // 取得綠界支付 html
                var html = GetECPayHtml(parameters, orderId, paymentType, installment.ToString());

                // 因綠界SDK在海外支付產生日期時出現格式錯誤無法支付，先以綠界提供之原始碼產生支付html內容

                //var orderRequest = new AllInOne
                //{
                //    // 廠商編號
                //    MerchantID = parameters.Where(w => w.strPaymentParamCode == "MerchantID").FirstOrDefault()?.strPaymentParamValue,
                //    // HashKey
                //    HashKey = parameters.Where(w => w.strPaymentParamCode == "HashKey").FirstOrDefault()?.strPaymentParamValue,
                //    // HashIV
                //    HashIV = parameters.Where(w => w.strPaymentParamCode == "HashIV").FirstOrDefault()?.strPaymentParamValue,
                //    // 網址
                //    ServiceURL = parameters.Where(w => w.strPaymentParamCode == "SubmitURL").FirstOrDefault()?.strPaymentParamValue,
                //    // HttpMethod
                //    ServiceMethod = HttpMethod.HttpPOST
                //};

                //// 訂單Domain
                //var domain = ServiceProcesser.Get_strSysParameter("WebConfig", "Domain", "Line");
                //// 訂單資料
                //var order = CustomerOrderHelper.GetOrderheader(orderId);
                //var orderDetail = CustomerOrderHelper.GetTROrderDetail(orderId);
                //// 寫入第三方訂單資料
                //var requestOrderNo = CustomerOrderHelper.InsertThirdPartyOrder(orderId, paymentType, strPaymentTypeName, installment.ToString());

                //// API notify URL
                //// 綠界 分期付款
                //if (isInstallment)
                //{
                //    orderRequest.Send.ReturnURL = $"{domain}/{nameof(Customer)}/{nameof(OrderController).GetControllerName()}/{nameof(OrderController.ECPayInstallmentNotify)}";
                //}
                //// 綠界 一次付清
                //else
                //{
                //    orderRequest.Send.ReturnURL = $"{domain}/{nameof(Customer)}/{nameof(OrderController).GetControllerName()}/{nameof(OrderController.ECPayNotify)}";
                //}

                //// 結束支付後導頁網址
                //orderRequest.Send.OrderResultURL = $"{domain}/{nameof(Customer)}/{nameof(MemberCenterController).GetControllerName()}/{nameof(MemberCenterController.OrderDetail_Mobile)}?orderId={orderId}&memberId={order.strMemberID}";
                //// 客製化欄位1 TR_OrderHeader_strOrderID
                //orderRequest.Send.CustomField1 = order.strOrderID;
                //// 交易編號
                //orderRequest.Send.MerchantTradeNo = requestOrderNo;
                //// 交易時間
                //orderRequest.Send.MerchantTradeDate = DateTime.Now;
                //// 交易總金額 需用Int
                //orderRequest.Send.TotalAmount = decimal.ToInt32(Convert.ToDecimal(order.curAmount));
                //// 交易描述
                //orderRequest.Send.TradeDesc = HttpUtility.UrlDecode("交易描述");
                //// 付款方式
                //orderRequest.Send.ChoosePayment = PaymentMethod.Credit;
                //// 裝置類型
                //orderRequest.Send.DeviceSource = DeviceType.Mobile;
                //// 商品
                //var intQty = orderDetail.Sum(s => Convert.ToInt32(s.intQty));

                //// 分期付款 期數 有需要才設定
                //if (isInstallment)
                //{
                //    orderRequest.SendExtend.CreditInstallment = installment.ToString();
                //}

                //orderRequest.Send.Items.Add(
                //    orderDetail.Select(s => new Item
                //    {
                //        Name = string.Format(Resources.SocialChat.PaymentItemList, s.strProductName_CH, intQty),
                //        Price = Decimal.ToInt32(Convert.ToDecimal(order.curAmount)),
                //        Quantity = 1,
                //        Currency = "元"
                //    }).FirstOrDefault()
                //);
                //var html = "";

                //var errorList = orderRequest.CheckOutString(ref html);

                //// 有錯值寫入log
                //if (errorList.Count() > 0)
                //{
                //    _logger.Error($"ECPay CheckOutString error: {JsonConvert.SerializeObject(errorList)}");
                //    throw new Exception($"{JsonConvert.SerializeObject(errorList)}");
                //}

                _logger.Info($"ECPay ended, orderId: {orderId}, paymentTypeId: {paymentTypeId}");

                return Content(html);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, MethodBase.GetCurrentMethod().Name);

                // 導到錯誤提醒頁面
                return RedirectToAction("", "");
            }
        }

        /// <summary>
        /// 取得綠界支付 html
        /// </summary>
        /// <param name="parameters">系統參數清單</param>
        /// <param name="orderId">訂單ID</param>
        /// <param name="paymentType">付款方式</param>
        /// <param name="installment">分期數</param>
        /// <returns></returns>
        private string GetECPayHtml(IEnumerable<ThirdPartyParameter> parameters, string orderId, PaymentType paymentType, string installment)
        {
            // 訂單資料
            var order = GetOrder(orderId);
            var requestDict = new SortedDictionary<string, string>
            {
                { "MerchantID", "" }, // 特店編號
                { "MerchantTradeNo", "" }, // 特店交易編號
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") }, // 特店交易時間
                { "PaymentType", "aio" },
                { "CustomField1", "" }, // 客製欄位 1，自行定義
                { "CustomField2", "" }, // 客製欄位 2，自行定義
                { "TotalAmount", "" }, // 總金額
                { "TradeDesc", HttpUtility.UrlDecode("交易描述") }, // 交易描述
                { "DeviceSource", DeviceType.Mobile.ToString() }, // 來源裝置
                { "ItemName", "" }, // 商品名稱
            };

            var domain = "";
            // 客戶端返回網址 (訂單詳情)
            var clientReturnUrl = "";
            // 訂單失效時間 (自行定義)
            var expiredTime = GetOrderExpiredTime();

            // 針對各付款方式做參數新增
            switch (paymentType)
            {
                // 一次付清
                case PaymentType.ECPay:
                    requestDict.Add("ChoosePayment", PaymentMethod.Credit.ToString());
                    requestDict.Add("OrderResultURL", clientReturnUrl);
                    requestDict.Add("ReturnURL", $"{domain}/{nameof(Customer)}/Order/{nameof(OrderController.ECPayNotify)}");
                    break;
                // 分期付款
                case PaymentType.ECPayInstallment:
                    requestDict.Add("ChoosePayment", PaymentMethod.Credit.ToString());
                    requestDict.Add("CreditInstallment", installment);
                    requestDict.Add("OrderResultURL", clientReturnUrl);
                    requestDict.Add("ReturnURL", $"{domain}/{nameof(Customer)}/Order/{nameof(OrderController.ECPayInstallmentNotify)}");
                    break;
                // 虛擬ATM
                case PaymentType.ECPayATM:
                    requestDict.Add("ChoosePayment", PaymentMethod.ATM.ToString());
                    // 單位為 天
                    requestDict.Add("ExpireDate", ((int)TimeSpan.FromMinutes(expiredTime).TotalDays).ToString());
                    requestDict.Add("PaymentInfoURL", $"{domain}/{nameof(Customer)}/Order/{nameof(OrderController.ECPayPaymentInfo)}");
                    requestDict.Add("ClientRedirectURL", clientReturnUrl);
                    requestDict.Add("ReturnURL", $"{domain}/{nameof(Customer)}/Order/{nameof(OrderController.ECPayNotify)}");
                    break;
                // 超商代碼
                case PaymentType.ECPayCVS:
                    requestDict.Add("ChoosePayment", PaymentMethod.CVS.ToString());
                    requestDict.Add("StoreExpireDate", expiredTime.ToString());
                    // 只加入不為空的描述
                    foreach (var desc in parameters.Where(x => !string.IsNullOrEmpty(x.strPaymentParamValue) && x.strPaymentParamCode.Contains("Desc")).ToList())
                    {
                        requestDict.Add(desc.strPaymentParamCode, desc.strPaymentParamValue);
                    }
                    requestDict.Add("PaymentInfoURL", $"{domain}/{nameof(Customer)}/Order/{nameof(OrderController.ECPayPaymentInfo)}");
                    requestDict.Add("ClientRedirectURL", clientReturnUrl);
                    requestDict.Add("ReturnURL", $"{domain}/{nameof(Customer)}/Order/{nameof(OrderController.ECPayNotify)}");
                    break;
            }

            // query string
            var queryBuilder = new StringBuilder();

            queryBuilder.Append($"HashKey={parameters.Where(w => w.strPaymentParamCode == "HashKey").FirstOrDefault()?.strPaymentParamValue}");

            foreach (var item in requestDict)
            {
                queryBuilder.Append($"&{item.Key}={item.Value}");
            }

            // hash 資料
            queryBuilder.Append($"&HashIV={parameters.Where(w => w.strPaymentParamCode == "HashIV").FirstOrDefault()?.strPaymentParamValue}");

            var urlEncoded = HttpUtility.UrlEncode(queryBuilder.ToString()).ToLower();

            var sha256 = new SHA256CryptoServiceProvider();
            var data = sha256.ComputeHash(Encoding.UTF8.GetBytes(urlEncoded));

            var hashedBuilder = new StringBuilder();

            for (var i = 0; i < data.Length; i++)
            {
                hashedBuilder.Append(data[i].ToString("X2"));
            }

            var macValue = hashedBuilder.ToString();
            requestDict.Add("CheckMacValue", macValue);

            _logger.Info($"GetECPayHtml, query: {queryBuilder.ToString()}&CheckMacValue={macValue}");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // 支付頁html
            var htmlBuilder = new StringBuilder();

            htmlBuilder.Append("<html><body>").AppendLine();
            htmlBuilder.Append($"<form name='postdata'  id='postdata' action='{parameters.Where(w => w.strPaymentParamCode == "SubmitURL").FirstOrDefault()?.strPaymentParamValue}' method='POST'>").AppendLine();

            foreach (var item in requestDict)
            {
                htmlBuilder.Append($"<input type='hidden' name='{item.Key}' value='{item.Value}'>").AppendLine();
            }

            htmlBuilder.Append("</form>").AppendLine();
            htmlBuilder.Append("<script> var theForm = document.forms['postdata'];  if (!theForm) { theForm = document.postdata; } theForm.submit(); </script>").AppendLine();
            htmlBuilder.Append("<html><body>").AppendLine();

            return htmlBuilder.ToString();
        }

        /// <summary>
        /// 接收綠界 ATM/CVS 取號結果消息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> ECPayPaymentInfo()
        {
            try
            {
                _logger.Info($"{MethodBase.GetCurrentMethod().Name} start");

                var feedBack = new Hashtable();
                var errorList = new List<string>();

                using (var notify = new AllInOne())
                {
                    notify.HashKey = "HashKey";
                    notify.HashIV = "HashIV";
                    // 拿取訂單結果
                    errorList.AddRange(notify.CheckOutFeedback(ref feedBack));
                }

                // 寫入Log
                _ecPayLog.Info(JsonConvert.SerializeObject(feedBack));

                // 回傳訂單有問題寫入log，回傳0 失敗
                if (errorList.Count() > 0)
                {
                    _logger.Error($"{MethodBase.GetCurrentMethod().Name} CheckOutFeedback error: {JsonConvert.SerializeObject(errorList)}");
                    return Content($"0|{JsonConvert.SerializeObject(errorList)}");
                }

                // 轉換接收到的資料
                var result = ECPayConversion(feedBack, true);

                switch (result.PaymentTypeDesc)
                {
                    // 虛擬ATM
                    case PaymentType.ECPayATM:
                        // 新增虛擬 ATM 訂單資料
                        break;
                    // 超商代碼
                    case PaymentType.ECPayCVS:
                        // 新增超商代碼訂單資料
                        break;
                }

                _logger.Info($"{MethodBase.GetCurrentMethod().Name} end");
                return Content("1|OK");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, MethodBase.GetCurrentMethod().Name);
                return Content($"0|{ex.Message}");
            }
        }

        /// <summary>
        /// 綠界支付
        /// 一次付清/超商代碼/虛擬ATM
        /// 接收付款結果通知
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> ECPayNotify()
        {
            try
            {
                _logger.Info("ECPayNotify started");
                var feedBack = new Hashtable();
                var errorList = new List<string>();

                // 取得非分期付款參數 (HashKey、HashIV...等)
                var parameters = GetECPayParameters();

                using (var notify = new AllInOne())
                {
                    notify.HashKey = "HashKey";
                    notify.HashIV = "HashIV";
                    // 拿取訂單結果
                    errorList.AddRange(notify.CheckOutFeedback(ref feedBack));
                }
                // 寫入Log
                _ecPayLog.Info(JsonConvert.SerializeObject(feedBack));

                // 回傳訂單有問題寫入log，回傳0 失敗
                if (errorList.Count() > 0)
                {
                    _logger.Error($"ECPay CheckOutFeedback error: {JsonConvert.SerializeObject(errorList)}");
                    return Content($"0|{JsonConvert.SerializeObject(errorList)}");
                }

                // 跑支付成功流程
                await ECPayScccuess(feedBack);

                _logger.Info("ECPayNotify ended");
                return Content("1|OK");
            }
            catch (Exception ex)
            {
                _logger.Error("{0} {1}", MethodBase.GetCurrentMethod().Name, ex);
                return Content($"0|{ex.Message}");
            }
        }

        /// <summary>
        /// 綠界支付
        /// 分期付款
        /// 接收付款結果通知
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> ECPayInstallmentNotify()
        {
            try
            {
                _logger.Info("ECPayInstallmentNotify started");
                var feedBack = new Hashtable();
                var errorList = new List<string>();

                // 取得分期付款參數 (HashKey、HashIV...等)
                var parameters = GetECPayInstallmentParameters();

                using (var notify = new AllInOne())
                {
                    // HashKey
                    notify.HashKey = "HashKey";
                    // HashIV
                    notify.HashIV = "HashIV";
                    // 拿取訂單結果
                    errorList.AddRange(notify.CheckOutFeedback(ref feedBack));
                }
                // 從這裡拿到訂單結果，寫入Log
                _ecPayLog.Info(JsonConvert.SerializeObject(feedBack));

                // 回傳訂單有問題寫入log，回傳0 失敗
                if (errorList.Count() > 0)
                {
                    _logger.Error($"ECPay CheckOutFeedback error: {JsonConvert.SerializeObject(errorList)}");
                    return Content($"0|{JsonConvert.SerializeObject(errorList)}");
                }

                // 跑支付成功流程
                await ECPayScccuess(feedBack);

                _logger.Info("ECPayInstallmentNotify ended");
                return Content("1|OK");
            }
            catch (Exception ex)
            {
                _logger.Error("{0} {1}", MethodBase.GetCurrentMethod().Name, ex);
                return Content($"0|{ex.Message}");
            }
        }

        /// <summary>
        /// 綠界支付
        /// 支付成功流程
        /// </summary>
        /// <param name="feedBack">綠界回來的訂單資料</param>
        [NonAction]
        public async Task ECPayScccuess(Hashtable feedBack)
        {
            try
            {
                // 轉換接收到的資料
                var response = ECPayConversion(feedBack);

                // 交易狀態代碼不為1 (1表示付款成功)
                if (response.RtnCode != 1)
                {
                    //  Log 紀錄交易失敗訊息
                    return;
                }

                // 取得訂單資料
                var order = GetOrder(response.OrderId);

                // 已經為已支付狀態
                if (Convert.ToBoolean(order.ysnPayment))
                    return;

                // 付款成功
                await PaySucceed(response.OrderId, (PaymentType)response.PaymentType, response.ReponseMsg);
                
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"ECPayScccuess before CheckOutFeedback");
                
                if (feedBack.Count == 0)
                    throw;

                //  Log 紀錄交易失敗訊息
                throw;
            }
        }
        #endregion

        /// <summary>
        /// 支付成功
        /// </summary>
        /// <param name="orderId">訂單ID</param>
        /// <param name="paymentType">支付類型</param>
        /// <param name="response">回傳內容</param>
        private async Task PaySucceed(string orderId, PaymentType paymentType, string response)
        {
            // 取得訂單資料
            var order = GetOrder(orderId);

            // Log 紀錄訂單狀態變化歷程

            // 更新訂單支付狀態

            // 處理發票流程

            // 通知支付完成
        }
    }
}
