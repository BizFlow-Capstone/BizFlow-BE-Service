using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class Import
{
    public long ImportId { get; set; }

    /// <summary>
    /// Auto-generated import code, format: IMP-YYYYMMDD-LOCATIONID-NNN
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
    /// Whether the import has an invoice attached
    /// </summary>
    public bool HasInvoice { get; set; }

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

    /// <summary>
    /// When the import was confirmed
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// When the import was cancelled
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// UserId that cancelled this import.
    /// </summary>
    public Guid? CancelledBy { get; set; }

    /// <summary>
    /// Original Import this row replaces (replace-when-confirmed flow).
    /// </summary>
    public long? RefImportId { get; set; }

    /// <summary>
    /// Client-supplied idempotency key for the replace-when-confirmed flow (stored on the <b>replacement</b> row).
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    public string? CancelReason { get; set; }

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

    /// <summary>
    /// FK to ImportSchemaVersions
    /// </summary>
    public int? SchemaVersionId { get; set; }

    /// <summary>
    /// Stored data captured from schema form
    /// </summary>
    public string? SchemaDataJson { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual ICollection<Cost> Costs { get; set; } = new List<Cost>();

    public virtual ICollection<ProductImport> ProductsImports { get; set; } = new List<ProductImport>();

    public virtual ImportSchemaVersion? SchemaVersion { get; set; }

    public virtual Import? RefImport { get; set; }
}
