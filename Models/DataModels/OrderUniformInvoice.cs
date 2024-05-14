using Common.Enums;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings

namespace Models.DataModels
{
    public class OrderUniformInvoice : BaseDataModel
    {
        [ForeignKey(nameof(Order))]
        public int OrderId { get; set; }

        public string Number { get; set; }

        public string RandomNumber { get; set; }

        public DateTime StartDate { get; set; }

        public string CheckMacValue { get; set; }

        public OrderInvoiceStatus Status { get; set; }

        public Order Order { get; set; }
    }
}
