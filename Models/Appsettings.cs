#nullable disable

namespace Models
{
    public class Appsettings
    {
        public Jwtsettings JwtSettings { get; set; }

        public MailSettings Mail { get; set; }
    }

    public class Jwtsettings
    {
        public string Issuer { get; set; }

        public string Key { get; set; }
    }

    public class MailSettings
    {
        public string From { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public string User { get; set; }

        public string Password { get; set; }
    }

    public class AWS
    {
        public string AccessKeyId { get; set; }

        public string SecretAccessKey { get; set; }

        public Source Source { get; set; }

        public bool HasValue => !string.IsNullOrWhiteSpace(AccessKeyId)
            && !string.IsNullOrWhiteSpace(SecretAccessKey)
            && !string.IsNullOrWhiteSpace(Source.Domain)
            && !string.IsNullOrWhiteSpace(Source.Arn);
    }

    public class Source
    {
        public string Domain { get; set; }

        public string Arn { get; set; }
    }

    public class EcPaySettings
    {
        public string PaymentUrl { get; set; }
        public string LogisticsUrl { get; set; }
        public string ReturnedLogisticsUrl { get; set; }
        public string B2CInvoiceIssueUrl { get; set; }
        public string B2CInvoiceInvalidUrl { get; set; }
        public string InvoiceDetailUrl { get; set; }
        public string GetCompanyNameByTaxIdUrl { get; set; }
        public string CheckBarcodeUrl { get; set; }
        public EcPayMerchantSettings Payment { get; set; }
        public EcPayMerchantSettings Logistics { get; set; }
        public EcPayMerchantSettings Invoice { get; set; }
    }

    public class EcPayMerchantSettings
    {
        public string HashKey { get; set; }
        public string HashIV { get; set; }
        public string MerchantId { get; set; }
    }

    public class NewebPaySettings
    {
        public string PaymentUrl { get; set; }
        public string HashKey { get; set; }
        public string HashIV { get; set; }
        public string MerchantId { get; set; }
    }
}
