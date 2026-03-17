using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Order line item details (snapshot pricing at sale time).
/// </summary>
public partial class OrderDetail
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
    /// Sold quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Snapshot unit price at order creation time.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Line-item discount.
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// Line amount = Quantity * UnitPrice - Discount.
    /// </summary>
    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual SaleItem SaleItem { get; set; } = null!;
}
