using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Store revenue source-of-truth for all income entries
/// </summary>
public partial class Revenue
{
    public long RevenueId { get; set; }

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// Optional business type classification for accounting book context
    /// </summary>
    public Guid? BusinessTypeId { get; set; }

    /// <summary>
    /// Soft reference to Order, nullable because some revenues are manual
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// sale | manual
    /// </summary>
    public string RevenueType { get; set; } = null!;

    /// <summary>
    /// Revenue amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// draft | posted | cancelled | replaced (see <c>BizFlow.Domain.Enums.RevenueStatus</c>).
    /// </summary>
    public string Status { get; set; } = "posted";

    /// <summary>
    /// Revenue posting date
    /// </summary>
    public DateOnly RevenueDate { get; set; }

    /// <summary>
    /// Revenue description
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// cash | bank | debt
    /// </summary>
    public string? MoneyChannel { get; set; }

    /// <summary>
    /// Receipt/invoice URL (Cloudinary)
    /// </summary>
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Receipt/invoice Cloudinary public id
    /// </summary>
    public string? DocumentPublicId { get; set; }

    /// <summary>
    /// Supporting document number (optional)
    /// </summary>
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Supporting document date (optional)
    /// </summary>
    public DateOnly? DocumentDate { get; set; }

    /// <summary>
    /// UserId that created this record (null if creator was purged / anonymized).
    /// </summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this revenue was cancelled (either standalone cancel or as part of replace-flow).
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// UserId that cancelled this revenue.
    /// </summary>
    public Guid? CancelledBy { get; set; }

    /// <summary>
    /// Original Revenue this row replaces (replace-when-posted flow). Null when this record was created fresh.
    /// </summary>
    public long? RefRevenueId { get; set; }

    /// <summary>
    /// Client-supplied idempotency key for the replace-when-posted flow (stored on the <b>replacement</b> row).
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Normalized (UPPER+TRIM) copy of <see cref="DocumentNumber"/> produced by a DB-generated column.
    /// Used for uniqueness checks across Costs + Revenues.
    /// </summary>
    public string? DocumentNumberNormalized { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;
    public virtual BusinessType? BusinessType { get; set; }

    public virtual Revenue? RefRevenue { get; set; }
}
