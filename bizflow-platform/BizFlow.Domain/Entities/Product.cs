using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class Product
{
    public long ProductId { get; set; }

    /// <summary>
    /// Warehouse/location of product
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// Business type category
    /// </summary>
    public Guid BusinessTypeId { get; set; }

    public string ProductName { get; set; } = null!;

    /// <summary>
    /// Stock Keeping Unit code
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Cost price
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Selling price in base unit
    /// </summary>
    public decimal SellingPrice { get; set; }

    /// <summary>
    /// Quantity in stock
    /// </summary>
    public decimal Stock { get; set; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public string Unit { get; set; } = null!;

    public string? ImageUrl { get; set; }

    /// <summary>
    /// Cloudinary public ID for image deletion
    /// </summary>
    public string? ImagePublicId { get; set; }

    /// <summary>
    /// Manufacturer name
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Whether to track inventory quantity
    /// </summary>
    public bool? TrackInventory { get; set; }

    /// <summary>
    /// Product sale status
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Soft delete timestamp
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual BusinessType BusinessType { get; set; } = null!;

    public virtual ICollection<ProductImport> ProductsImports { get; set; } = new List<ProductImport>();

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
