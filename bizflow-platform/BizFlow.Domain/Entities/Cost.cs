using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Store cost source-of-truth for all expense entries
/// </summary>
public partial class Cost
{
    public long CostId { get; set; }

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// FK to BusinessTypes — business sector for this expense (nullable for legacy rows)
    /// </summary>
    public Guid? BusinessTypeId { get; set; }

    /// <summary>
    /// import | salary | rent | utilities | transport | marketing | maintenance | other | manual
    /// </summary>
    public string CostType { get; set; } = null!;

    /// <summary>
    /// FK to Imports (only when CostType = import)
    /// </summary>
    public long? ImportId { get; set; }

    /// <summary>
    /// Cost description
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Cost amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// draft | posted | cancelled | replaced (see <c>BizFlow.Domain.Enums.CostStatus</c>).
    /// </summary>
    public string Status { get; set; } = "posted";

    /// <summary>
    /// Cost posting date
    /// </summary>
    public DateOnly CostDate { get; set; }

    /// <summary>
    /// cash | bank
    /// </summary>
    public string? PaymentMethod { get; set; }

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

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When this cost was cancelled (either standalone cancel or as part of replace-flow).
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// UserId that cancelled this cost.
    /// </summary>
    public Guid? CancelledBy { get; set; }

    /// <summary>
    /// Original Cost this row replaces (replace-when-posted flow). Null when this record was created fresh.
    /// </summary>
    public long? RefCostId { get; set; }

    /// <summary>
    /// Client-supplied idempotency key for the replace-when-posted flow (stored on the <b>replacement</b> row).
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Normalized (UPPER+TRIM) copy of <see cref="DocumentNumber"/> produced by a DB-generated column.
    /// Used for uniqueness checks across Costs + Revenues.
    /// </summary>
    public string? DocumentNumberNormalized { get; set; }

    /// <summary>
    /// TRUE when this row reverses another cost (append-only audit; Amount is negative of the original).
    /// </summary>
    public bool IsReversal { get; set; }

    /// <summary>
    /// Original <see cref="CostId"/> this reversal offsets (self-reference).
    /// </summary>
    public long? ReversedCostId { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual BusinessType? BusinessType { get; set; }

    public virtual Import? Import { get; set; }

    public virtual Cost? RefCost { get; set; }

    public virtual Cost? ReversedCost { get; set; }

    public virtual ICollection<Cost> InverseReversedCost { get; set; } = new List<Cost>();
}
