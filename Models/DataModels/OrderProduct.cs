using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings

namespace Models.DataModels
{
    public class OrderProduct : BaseDataModel
    {
        [ForeignKey(nameof(Order))]
        public int OrderId { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        public int Quantity { get; set; }

        [Precision(18, 3)]
        public decimal Price { get; set; }

        public Order Order { get; set; }
    }
}
