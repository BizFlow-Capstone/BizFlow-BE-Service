using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class SaleItems
{
    public long SaleItemId { get; set; }

    public long ProductId { get; set; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public string Unit { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<OrderDetails> OrderDetails { get; set; } = new List<OrderDetails>();

    public virtual Products Product { get; set; } = null!;

    public virtual ICollection<ProductPricePolicies> ProductPricePolicies { get; set; } = new List<ProductPricePolicies>();
}
