using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class Import
{
    public long ImportId { get; set; }

    /// <summary>
    /// Auto-generated import code (e.g. PNK-2026-001)
    /// </summary>
    public string? ImportCode { get; set; }

    /// <summary>
    /// INVOICE, INVENTORY_ADJUSTMENT, RETURN
    /// </summary>
    public string ImportType { get; set; } = null!;

    /// <summary>
    /// DRAFT, CONFIRMED, CANCELLED
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// Supplier name (free text)
    /// </summary>
    public string? Supplier { get; set; }

    /// <summary>
    /// Import data schema
    /// </summary>
    public string? SchemaJson { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public string? Note { get; set; }

    /// <summary>
    /// URL of attached image/document
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Cloudinary public ID for image deletion
    /// </summary>
    public string? ImagePublicId { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual ICollection<ProductImport> ProductsImports { get; set; } = new List<ProductImport>();
}
