using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Store revenue - source of truth for all income records
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
    /// Revenue recognition date
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
    /// UserId of the creator
    /// </summary>
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Soft delete
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;
    public virtual BusinessType? BusinessType { get; set; }
}
