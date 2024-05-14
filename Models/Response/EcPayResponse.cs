using ECPay.SDK.Logistics;

#nullable disable warnings

namespace Models.Response
{
    #region Base
    public class EcPayDefaultResponse
    {
        /// <summary>
        /// 特店編號
        /// </summary>
        public virtual string MerchantId { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        public virtual int RtnCode { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public virtual string RtnMsg { get; set; }

        /// <summary>
        /// 檢查碼
        /// </summary>
        public string CheckMacValue { get; set; }
    }

    public class EcPayDefaultWithTradeNoResponse : EcPayDefaultResponse
    {
        /// <summary>
        /// 特店交易編號
        /// </summary>
        public string MerchantTradeNo { get; set; }
    }

    public class EcPayPaymentFlowResponse : EcPayDefaultWithTradeNoResponse
    {
        /// <summary>
        /// 特店旗下店舖代號
        /// </summary>
        public string StoreID { get; set; }

        /// <summary>
        /// 交易狀態
        /// 1. 付款結果通知：
        /// 若回傳值為1時，為付款成功，其餘為失敗，請至廠商管理後台確認後再出貨。
        /// 2. 取號結果通知：
        /// ATM 回傳值時為2時，交易狀態為取號成功，其餘為失敗。
        /// CVS/BARCODE回傳值時為10100073時，交易狀態為取號成功，其餘為失敗。
        /// </summary>
        public override int RtnCode { get; set; }

        /// <summary>
        /// 交易訊息
        /// 1. 付款結果通知：
        /// 付款成功後綠界後端(server端)回傳：RtnMsg=交易成功
        /// 綠界後端透過排程補送通知回傳：RtnMsg=paid
        /// 付款成功後綠界回傳到OrderResultURL(client端)：RtnMsg=Succeeded
        /// </summary>
        public override string RtnMsg { get; set; }

        /// <summary>
        /// 綠界的交易編號
        /// </summary>
        public string TradeNo { get; set; }

        /// <summary>
        /// 交易金額
        /// </summary>
        public decimal TradeAmt { get; set; }

        /// <summary>
        /// 訂單成立時間
        /// yyyy/MM/dd HH:mm:ss
        /// </summary>
        public string TradeDate { get; set; }

        /// <summary>
        /// 選擇的付款方式
        /// https://developers.ecpay.com.tw/?p=5686
        /// </summary>
        public string PaymentType { get; set; }

        /// <summary>
        /// 自訂欄位1
        /// 目前為訂單代碼
        /// </summary>
        public string CustomField1 { get; set; }

        /// <summary>
        /// 自訂欄位2
        /// </summary>
        public string CustomField2 { get; set; }

        /// <summary>
        /// 自訂欄位3
        /// </summary>
        public string CustomField3 { get; set; }

        /// <summary>
        /// 自訂欄位4
        /// </summary>
        public string CustomField4 { get; set; }
    }

    public class EcPayInvoiceDefaultResponse
    {
        /// <summary>
        /// 回傳代碼
        /// </summary>
        public virtual int TransCode { get; set; }

        /// <summary>
        /// 回傳訊息
        /// </summary>
        public virtual string TransMsg { get; set; }

        /// <summary>
        /// 加密資料
        /// </summary>
        public string Data { get; set; }
    }

    #endregion

    public class EcPayResponse : EcPayPaymentFlowResponse
    {
        /// <summary>
        /// 是否為模擬付款
        /// 0：代表此交易非模擬付款。
        /// 1：代表此交易為模擬付款，RtnCode也為1。並非是由消費者實際真的付款，所以綠界也不會撥款給廠商，請勿對該筆交易做出貨等動作，以避免損失。
        /// </summary>
        public int SimulatePaid { get; set; }

        /// <summary>
        /// 付款時間
        /// yyyy/MM/dd HH:mm:ss
        /// </summary>
        public string PaymentDate { get; set; }

        /// <summary>
        /// 交易手續費金額
        /// </summary>
        public string PaymentTypeChargeFee { get; set; }
    }

    public class EcPayPaymentWithExtraResponse : EcPayResponse
    {
        public string AlipayID { get; set; }
        public string AlipayTradeNo { get; set; }
        public string TenpayTradeNo { get; set; }
        /// <summary>
        /// 雖然綠界文件上寫說 "額外回傳的參數全部都需要加入檢查碼計算"
        /// 但這個參數又另外註明 "付款方式為行動支付TWQR時才回傳"
        /// </summary>
        public string TWQRTradeNo { get; set; }
        public string WebATMAccBank { get; set; }
        public string WebATMAccNo { get; set; }
        public string WebATMBankName { get; set; }
        public string ATMAccBank { get; set; }
        public string ATMAccNo { get; set; }
        public string PaymentNo { get; set; }
        public string PayFrom { get; set; }
        public string gwsr { get; set; }
        public string process_date { get; set; }
        public string auth_code { get; set; }
        public int amount { get; set; }
        public int stage { get; set; }
        public int stast { get; set; }
        public int staed { get; set; }
        public int eci { get; set; }

        /// <summary>
        /// 卡片的末4碼
        /// </summary>
        public string card4no { get; set; }
        public string card6no { get; set; }
        public int red_dan { get; set; }
        public int red_de_amt { get; set; }
        public int red_ok_amt { get; set; }
        public int red_yet { get; set; }
        public string PeriodType { get; set; }
        public string Frequency { get; set; }
        public int? ExecTimes { get; set; }
        public int? PeriodAmount { get; set; }
        public int? TotalSuccessTimes { get; set; }
        public int? TotalSuccessAmount { get; set; }
    }

    public class EcpayAtmTakeNumberResponse : EcPayPaymentFlowResponse
    {
        /// <summary>
        /// 繳費銀行代碼
        /// ATM
        /// </summary>
        public string BankCode { get; set; }

        /// <summary>
        /// 繳費虛擬帳號
        /// ATM
        /// </summary>
        public string vAccount { get; set; }

        /// <summary>
        /// 繳費期限
        /// ATM
        /// </summary>
        public string ExpireDate { get; set; }
    }

    public class EcPayLogisticResponse : EcPayDefaultWithTradeNoResponse
    {
        /// <summary>
        /// 物流子類型
        /// </summary>
        public LogisticsSubTypes? LogisticsSubType { get; set; }

        /// <summary>
        /// 使用者選擇的超商店舖編號
        /// </summary>
        public string CVSStoreId { get; set; }

        /// <summary>
        /// 使用者選擇的超商店舖名稱
        /// </summary>
        public string CVSStoreName { get; set; }

        /// <summary>
        /// 使用者選擇的超商店舖地址
        /// </summary>
        public string CVSAddress { get; set; }

        /// <summary>
        /// 使用者選擇的超商店舖電話
        /// </summary>
        public string CVSTelephone { get; set; }

        /// <summary>
        /// 使用者選擇的超商店舖是否為離島店鋪
        /// 0：本島, 1：離島
        /// </summary>
        public string CVSOutSide { get; set; }

        /// <summary>
        /// 額外資訊
        /// </summary>
        public string ExtraData { get; set; }

        public bool HasValue => !string.IsNullOrWhiteSpace(CVSStoreId);
    }

    public class EcpayLogisticsNotificationResponse : EcPayDefaultWithTradeNoResponse
    {
        /// <summary>
        /// 綠界科技的物流交易編號
        /// </summary>
        public string AllPayLogisticsID { get; set; }

        /// <summary>
        /// 物流類型
        /// </summary>
        public string LogisticsType { get; set; }

        /// <summary>
        /// 物流子類型
        /// </summary>
        public string LogisticsSubType { get; set; }

        /// <summary>
        /// 商品金額
        /// </summary>
        public int GoodsAmount { get; set; }

        /// <summary>
        /// 物流狀態更新時間
        /// </summary>
        public string UpdateStatusDate { get; set; }

        /// <summary>
        /// 收件人名稱
        /// </summary>
        public string ReceiverName { get; set; }

        /// <summary>
        /// 收件人電話
        /// </summary>
        public string ReceiverPhone { get; set; }

        /// <summary>
        /// 收件人手機
        /// </summary>
        public string ReceiverCellPhone { get; set; }

        /// <summary>
        /// 收件人 email
        /// </summary>
        public string ReceiverEmail { get; set; }

        /// <summary>
        /// 收件人地址
        /// </summary>
        public string ReceiverAddress { get; set; }

        /// <summary>
        /// 寄貨編號
        /// </summary>
        public string CVSPaymentNo { get; set; }

        /// <summary>
        /// 驗證碼
        /// </summary>
        public string CVSValidationNo { get; set; }

        /// <summary>
        /// 託運單號
        /// </summary>
        public string BookingNote { get; set; }
    }


    public class EcpayReturnedLogisticsNotificationResponse : EcPayDefaultResponse
    {
        /// <summary>
        /// 特店交易編號
        /// </summary>
        public string RtnMerchantTradeNo { get; set; }

        /// <summary>
        /// 綠界科技的物流交易編號
        /// </summary>
        public string AllPayLogisticsID { get; set; }

        /// <summary>
        /// 商品金額
        /// </summary>
        public int GoodsAmount { get; set; }

        /// <summary>
        /// 物流狀態更新時間
        /// </summary>
        public string UpdateStatusDate { get; set; }

        /// <summary>
        /// 託運單號
        /// </summary>
        public string BookingNote { get; set; }
    }


    public class EcPayInvoiceResponse : EcPayInvoiceDefaultResponse
    {
        public string Data { get; set; }
    }

    public class EcPayInvoiceDataResponse : EcPayDefaultResponse
    {
        /// <summary>
        /// 發票號碼
        /// </summary>
        public string InvoiceNo { get; set; }

        /// <summary>
        /// 發票建立日期
        /// </summary>
        public string InvoiceDate { get; set; }

        /// <summary>
        /// 隨機碼
        /// </summary>
        public string RandomNumber { get; set; }
    }

    public class EcPayGetCompanyNameResponse : EcPayDefaultResponse
    {
        /// <summary>
        /// 公司名稱
        /// </summary>
        public string CompanyName { get; set; }
    }

    public class EcPayCheckBarcodeResponse : EcPayDefaultResponse
    {
        /// <summary>
        /// 公司名稱
        /// </summary>
        public string IsExist { get; set; }
    }
}
