#nullable disable warnings

namespace Models.Response
{
    #region Base

    public class BaseNewebPayTradeInfoResult
    {
        /// <summary>
        /// 商店代號
        /// </summary>
        public string MerchantID { get; set; }

        /// <summary>
        /// 交易金額
        /// </summary>
        public int Amt { get; set; }

        /// <summary>
        /// 藍新金流交易序號
        /// </summary>
        public string TradeNo { get; set; }

        /// <summary>
        /// 商店訂單編號
        /// </summary>
        public string MerchantOrderNo { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        public string PaymentType { get; set; }

        /// <summary>
        /// 回傳格式
        /// </summary>
        public string RespondType { get; set; }

        /// <summary>
        /// 支付完成時間
        /// </summary>
        public DateTime PayTime { get; set; }

        /// <summary>
        /// 交易 IP
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// 款項保管銀行
        /// </summary>
        public string EscrowBank { get; set; }
    }

    #endregion

    public class NewebPayResponse
    {
        /// <summary>
        /// 回傳狀態
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 商店代號
        /// </summary>
        public string MerchantID { get; set; }

        /// <summary>
        /// 交易資料 AES 加密
        /// </summary>
        public string TradeInfo { get; set; }

        /// <summary>
        /// 交易資料 SHA256 加密
        /// </summary>
        public string TradeSha { get; set; }

        /// <summary>
        /// 串接程式版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 加密模式
        /// </summary>
        public int EncryptType { get; set; }
    }

    public class NewebPayTradeInfo<T> where T : BaseNewebPayTradeInfoResult
    {
        /// <summary>
        /// 回傳狀態
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 回傳訊息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 回傳參數
        /// </summary>
        public T Result { get; set; }
    }

    public class NewebPayTradeInfoCreditCard : BaseNewebPayTradeInfoResult
    {
        /// <summary>
        /// 收單金融機構
        /// </summary>
        public string AuthBank { get; set; }

        /// <summary>
        /// 金融機構回應碼
        /// </summary>
        public string RespondCode { get; set; }

        /// <summary>
        /// 授權碼
        /// </summary>
        public string Auth { get; set; }

        /// <summary>
        /// 卡號前六碼
        /// </summary>
        public string Card6No { get; set; }

        /// <summary>
        /// 卡號末四碼
        /// </summary>
        public string Card4No { get; set; }

        /// <summary>
        /// 分期-期別
        /// </summary>
        public int Inst { get; set; }

        /// <summary>
        /// 分期-首期金額
        /// </summary>
        public int InstFirst { get; set; }

        /// <summary>
        /// 分期-每期金額
        /// </summary>
        public int InstEach { get; set; }

        /// <summary>
        /// ECI 值
        /// 1. 3D 回傳值 eci=1,2,5,6，代表為 3D 交易。
        /// 2. 若交易送至收單機構授權時已是失敗狀態，則本欄位的值會以空值回傳。
        /// </summary>
        public string ECI { get; set; }

        /// <summary>
        /// 信用卡快速結帳使用狀態
        /// 0 = 該筆交易為非使用信用卡快速結帳功能。
        /// 1 = 該筆交易為首次設定信用卡快速結帳功能。
        /// 2 = 該筆交易為使用信用卡快速結帳功能。
        /// 9 = 該筆交易為取消信用卡快速結帳功能功能。
        /// </summary>
        public int TokenUseStatus { get; set; }

        /// <summary>
        /// 紅利折抵後實際金額
        /// 1. 若紅利點數不足，會有以下狀況：
        /// 1-1 紅利折抵交易失敗，回傳參數數值為 0。
        /// 1-2 紅利折抵交易成功，回傳參數數值為訂單金額。
        /// 1-3 紅利折抵交易是否成功，視該銀行之設定為準。
        /// 2. 僅有使用紅利折抵交易時才會回傳此參數。
        /// 3. 若紅利折抵掉全部金額，則此欄位回傳參數數值也會是 0，交易成功或交易失敗，請依回傳參數［Status］回覆為準。
        /// </summary>
        public string RedAmt { get; set; }

        /// <summary>
        /// 交易類別
        /// CREDIT = 台灣發卡機構核發之信用卡
        /// FOREIGN = 國外發卡機構核發之卡
        /// UNIONPAY = 銀聯卡
        /// GOOGLEPAY = GooglePay
        /// SAMSUNGPAY = SamsungPay
        /// DCC = 動態貨幣轉換
        /// </summary>
        public string PaymentMethod { get; set; }

        /// <summary>
        /// 外幣金額
        /// DCC 動態貨幣轉換交易才會回傳的參數
        /// </summary>
        public decimal DCC_Amt { get; set; }

        /// <summary>
        /// 匯率
        /// DCC 動態貨幣轉換交易才會回傳的參數
        /// </summary>
        public decimal DCC_Rate { get; set; }

        /// <summary>
        /// 風險匯率
        /// DCC 動態貨幣轉換交易才會回傳的參數
        /// </summary>
        public decimal DCC_Markup { get; set; }

        /// <summary>
        /// 幣別 ex. USD、JPY、MOP...
        /// DCC 動態貨幣轉換交易才會回傳的參數
        /// </summary>
        public string DCC_Currency { get; set; }

        /// <summary>
        /// 幣別代碼 ex. MOP = 446...
        /// DCC 動態貨幣轉換交易才會回傳的參數
        /// </summary>
        public int DCC_Currency_Code { get; set; }
    }


    public class NewebPayTradeInfoATM : BaseNewebPayTradeInfoResult
    {
        /// <summary>
        /// 付款人金融機構代碼
        /// </summary>
        public string PayBankCode { get; set; }

        /// <summary>
        /// 付款人金融機構帳號末五碼
        /// </summary>
        public string PayerAccount5Code { get; set; }
    }

    public class NewebPayTradeInfoCVS : BaseNewebPayTradeInfoResult
    {
        /// <summary>
        /// 繳費代碼
        /// </summary>
        public string CodeNo { get; set; }

        /// <summary>
        /// 繳費門市類別
        /// 1 = 7-11 統一超商
        /// 2 = 全家便利商店
        /// 3 = OK 便利商店
        /// 4 = 萊爾富便利商店
        /// </summary>
        public int StoreType { get; set; }

        /// <summary>
        /// 繳費門市代號
        /// </summary>
        public string StoreID { get; set; }
    }

    public class NewebPayTradeInfoBarcode : BaseNewebPayTradeInfoResult
    {
        /// <summary>
        /// 第一段條碼
        /// </summary>
        public string Barcode_1 { get; set; }

        /// <summary>
        /// 第二段條碼
        /// </summary>
        public string Barcode_2 { get; set; }

        /// <summary>
        /// 第三段條碼
        /// </summary>
        public string Barcode_3 { get; set; }

        /// <summary>
        /// 付款次數
        /// </summary>
        public int RepayTimes { get; set; }

        /// <summary>
        /// 繳費超商
        /// SEVEN = 7-11
        /// FAMILY = 全家
        /// OK = OK 超商
        /// HILIFE = 萊爾富
        /// </summary>
        public string PayStore { get; set; }
    }

    public class NewebPayTradeInfoCVSLogistics : BaseNewebPayTradeInfoResult
    {
        /// <summary>
        /// 超商門市編號
        /// </summary>
        public string StoreCode { get; set; }

        /// <summary>
        /// 超商門市名稱
        /// </summary>
        public string StoreName { get; set; }

        /// <summary>
        /// 超商類別名稱
        /// </summary>
        public string StoreType { get; set; }

        /// <summary>
        /// 超商門市地址
        /// </summary>
        public string StoreAddr { get; set; }

        /// <summary>
        /// 取件交易方式
        /// </summary>
        public int TradeType { get; set; }

        /// <summary>
        /// 取貨人
        /// </summary>
        public string CVSCOMName { get; set; }

        /// <summary>
        /// 取貨人手機號碼
        /// </summary>
        public string CVSCOMPhone { get; set; }

        /// <summary>
        /// 物流寄件單號
        /// </summary>
        public string LgsNo { get; set; }

        /// <summary>
        /// 物流型態
        /// </summary>
        public string LgsType { get; set; }
    }
}
