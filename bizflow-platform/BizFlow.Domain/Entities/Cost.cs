using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Store expenses - source of truth for all expense records
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
    /// Expense description
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Expense amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Expense date
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
    /// Cloudinary public ID of the receipt/invoice
    /// </summary>
    public string? DocumentPublicId { get; set; }

    /// <summary>
    /// UserId of the creator
    /// </summary>
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual Import? Import { get; set; }
}
