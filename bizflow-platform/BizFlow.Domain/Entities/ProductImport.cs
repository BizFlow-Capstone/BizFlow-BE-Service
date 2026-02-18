using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class ProductImport
{
    public long ImportId { get; set; }

    public long ProductId { get; set; }

    /// <summary>
    /// Import quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Import unit (e.g. thÃ¹ng, gÃ³i)
    /// </summary>
    public string Unit { get; set; } = null!;

    /// <summary>
    /// Cost price per import unit
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Total price
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Base/smallest inventory unit
    /// </summary>
    public string BaseUnit { get; set; } = null!;

    public virtual Import Import { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
