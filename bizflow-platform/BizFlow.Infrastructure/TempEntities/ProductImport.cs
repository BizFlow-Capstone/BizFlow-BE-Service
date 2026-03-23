using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class ProductsImports
{
    public long ProductImportId { get; set; }

    /// <summary>
    /// FK to Imports
    /// </summary>
    public long ImportId { get; set; }

    /// <summary>
    /// FK to Products
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// Import quantity
    /// </summary>
    public int Quantity { get; set; }

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

    public DateTime CreatedAt { get; set; }

    public virtual Imports Import { get; set; } = null!;

    public virtual Products Product { get; set; } = null!;
}
