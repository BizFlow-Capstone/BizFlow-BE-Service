using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class ProductPricePolicy
{
    public long ProductPricePolicyId { get; set; }

    public long SaleItemId { get; set; }

    /// <summary>
    /// Applied price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Is default price
    /// </summary>
    public bool IsDefault { get; set; }

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public virtual SaleItem SaleItem { get; set; } = null!;
}
