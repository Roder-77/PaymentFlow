using Common.Enums;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings

namespace Models.DataModels
{
    public class OrderLog : BaseDataModel
    {
        [ForeignKey(nameof(Order))]
        public int OrderId { get; set; }

        public LogAction Action { get; set; }

        public string? Remark { get; set; }

        public DateTime CreateTime { get; set; }

        public Order Order { get; set; }
    }
}
