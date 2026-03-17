using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Business costs source-of-truth table.
/// </summary>
public partial class Cost
{
    public long CostId { get; set; }

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// import | salary | rent | utilities | transport | marketing | maintenance | other | manual
    /// </summary>
    public string CostType { get; set; } = null!;

    /// <summary>
    /// FK to Imports (only when CostType = import)
    /// </summary>
    public long? ImportId { get; set; }

    /// <summary>
    /// Cost description.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Cost amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Cost date.
    /// </summary>
    public DateOnly CostDate { get; set; }

    /// <summary>
    /// cash | bank
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Document or invoice URL (Cloudinary).
    /// </summary>
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Cloudinary public ID of the document.
    /// </summary>
    public string? DocumentPublicId { get; set; }

    /// <summary>
    /// Creator UserId.
    /// </summary>
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual Import? Import { get; set; }
}
