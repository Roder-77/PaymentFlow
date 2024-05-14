using System.Collections.Generic;

namespace BgSoft.Core.Requests
{
    public class EcPayDefaultRequest
    {
        public string PlatformID { get; set; }
        public string MerchantID { get; set; }
        public EcPayHeader RqHeader { get; set; }
        public string Data { get; set; }

        public class EcPayHeader
        {
            public long Timestamp { get; set; }
        }
    }

    public class EcPayInvoiceIssueRequest
    {
        public string MerchantID { get; set; }
        public string RelateNumber { get; set; }
        public string ChannelPartner { get; set; }
        public string CustomerID { get; set; }
        public string CustomerIdentifier { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddr { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string ClearanceMark { get; set; }
        public string Print { get; set; }
        public string Donation { get; set; }
        public string LoveCode { get; set; }
        public string CarrierType { get; set; }
        public string CarrierNum { get; set; }
        public string TaxType { get; set; }
        public int SpecialTaxType { get; set; }
        public long SalesAmount { get; set; }
        public string InvoiceRemark { get; set; }
        public string InvType { get; set; }
        public string vat { get; set; }
        public IEnumerable<EcPayInvoiceItem> Items { get; set; }

        public class EcPayInvoiceItem
        {
            public int ItemSeq { get; set; }
            public string ItemName { get; set; }
            public decimal ItemCount { get; set; }
            public string ItemWord { get; set; }
            public decimal ItemPrice { get; set; }
            public string ItemTaxType { get; set; }
            public decimal ItemAmount { get; set; }
            public string ItemRemark { get; set; }
        }
    }

    public class EcPayInvoiceInvalidRequest
    {
        public string MerchantID { get; set; }
        public string InvoiceNo { get; set; }
        public string InvoiceDate { get; set; }
        public string Reason { get; set; }
    }
}
