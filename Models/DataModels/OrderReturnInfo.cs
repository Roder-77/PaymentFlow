using Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings

namespace Models.DataModels
{
    public class OrderReturnInfo : BaseDataModel
    {
        [ForeignKey(nameof(Order))]
        public int OrderId { get; set; }

        public OrderReturnStatus Status { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 退款金額
        /// </summary>
        public decimal RefundAmount { get; set; }

        /// <summary>
        /// 託運單號
        /// </summary>
        public string BookingNote { get; set; }

        public virtual Order Order { get; set; }
    }
}
