using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

/// <summary>
/// Chi tiết dòng sản phẩm trong đơn hàng (snapshot giá tại thời điểm bán)
/// </summary>
public partial class OrderDetails
{
    public long OrderDetailId { get; set; }

    /// <summary>
    /// FK to Orders
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// FK to SaleItems (live reference)
    /// </summary>
    public long SaleItemId { get; set; }

    /// <summary>
    /// Số lượng bán
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Snapshot đơn giá tại thời điểm tạo đơn
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Chiết khấu theo dòng sản phẩm
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// Thành tiền = Quantity * UnitPrice - Discount
    /// </summary>
    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Orders Order { get; set; } = null!;

    public virtual SaleItems SaleItem { get; set; } = null!;
}
