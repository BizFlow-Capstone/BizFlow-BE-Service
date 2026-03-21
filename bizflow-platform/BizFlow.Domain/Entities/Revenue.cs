using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Business revenue source-of-truth table.
/// </summary>
public partial class Revenue
{
    public long RevenueId { get; set; }

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// Soft reference to Order, nullable because some revenues are manual
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// sale | manual
    /// </summary>
    public string RevenueType { get; set; } = null!;

    /// <summary>
    /// Revenue amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Revenue recognition date.
    /// </summary>
    public DateOnly RevenueDate { get; set; }

    /// <summary>
    /// Revenue description.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// cash | bank | debt
    /// </summary>
    public string? MoneyChannel { get; set; }

    /// <summary>
    /// Creator UserId.
    /// </summary>
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Soft delete
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;
}
