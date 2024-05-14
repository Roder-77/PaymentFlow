using System.Text.Json.Serialization;

namespace Models
{
    public class Bank
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("bank_code")]
        public string BankCode { get; set; }

        [JsonPropertyName("branch_code")]
        public string BranchCode { get; set; }

        [JsonPropertyName("site")]
        public string Site { get; set; }
    }
}
