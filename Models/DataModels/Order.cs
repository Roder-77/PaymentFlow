using Common.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings

namespace Models.DataModels
{
    public class Order : BaseDataModel
    {
        [Column(TypeName = "VARCHAR")]
        [StringLength(20)]
        public string SeqNo { get; set; }

        [ForeignKey(nameof(Member))]
        public int MemberId { get; set; }

        [Column(TypeName = "VARCHAR")]
        [StringLength(20)]
        public string? MerchantTradeNo { get; set; }

        [Column(TypeName = "VARCHAR")]
        [StringLength(20)]
        public string? TradeNo { get; set; }

        public OrderStatus Status { get; set; }

        /// <summary>
        /// 材積 (長 + 寬 + 高)
        /// </summary>
        [Precision(18, 3)]
        public decimal ThreeDimensionsTotal { get; set; }

        /// <summary>
        /// 商品小計
        /// </summary>
        [Precision(18, 3)]
        public decimal SubTotal { get; set; }

        /// <summary>
        /// 總金額
        /// </summary>
        [Precision(18, 3)]
        public decimal TotalAmount { get; set; }

        public PaymentType PaymentMethod { get; set; }

        public ShippingType ShippingMethod { get; set; }

        public CarrierType CarrierType { get; set; }

        /// <summary>
        /// 載具編號
        /// </summary>
        public string CarrierNumber { get; set; }

        /// <summary>
        /// 自然人憑證
        /// </summary>
        public string CitizenDigitalCertificate { get; set; }

        /// <summary>
        /// 發票抬頭
        /// </summary>
        public string CompanyTitle { get; set; }

        /// <summary>
        /// 統一編號
        /// </summary>
        public string UniformNumber { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }

        public DateTime PayDeadline { get; set; }

        public Member Member { get; set; }
        public OrderReturnInfo ReturnInfo { get; set; }
        public OrderUniformInvoice UniformInvoice { get; set; }
        public ICollection<OrderLog> Logs { get; set; } = new HashSet<OrderLog>();
        public ICollection<OrderProduct> Products { get; set; } = new HashSet<OrderProduct>();
    }
}
