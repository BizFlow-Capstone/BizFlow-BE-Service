using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class SaleItem
{
    public long SaleItemId { get; set; }

    public long ProductId { get; set; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public string Unit { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductPricePolicy> ProductPricePolicies { get; set; } = new List<ProductPricePolicy>();
}
