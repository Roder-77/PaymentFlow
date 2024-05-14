using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Enums
{
    public enum Status
    {
        停用,
        啟用,
    }

    public enum OrderStatus
    {
        待付款,
        已付款,
        出貨中,
        退貨中,
    }

    public enum OrderReturnStatus
    {
        待通知物流,
        物流收貨中,
        盤點退貨商品,
        待退款,
        已退款,
        審核不通過,
    }

    public enum OrderInvoiceStatus
    {
        已作廢,
        已開立,
    }
}
