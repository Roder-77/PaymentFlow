using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Request
{
    public class ReturnOrderRequest
    {
        public string Id { get; set; }
        public int Reason { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public int Gender { get; set; }
        public string CellPhone { get; set; }
        public string PhoneArea { get; set; }
        public string PhoneNum { get; set; }
        public string PhoneSub { get; set; }
        public string PostalCode { get; set; }
        public string Address { get; set; }
        public string BankCode { get; set; }
        public string BankSubCode { get; set; }
        public string BankAccountName { get; set; }
        public string BankAccount { get; set; }
    }
}
