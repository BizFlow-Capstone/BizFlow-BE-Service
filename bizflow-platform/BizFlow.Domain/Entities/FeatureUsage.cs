using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Tracking usage of limited features per subscription period
/// </summary>
public partial class FeatureUsage
{
    public int FeatureUsageId { get; set; }

    public Guid SubscriptionId { get; set; }

    public int FeatureId { get; set; }

    public int UsedCount { get; set; }

    /// <summary>
    /// Accumulated allocated limit for this feature within the current subscription period.
    /// When user purchases the same plan multiple times (quantity stacking), this value is increased accordingly.
    /// -1 means unlimited.
    /// </summary>
    public int AllocatedLimit { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Feature Feature { get; set; } = null!;

    public virtual Subscription Subscription { get; set; } = null!;
}
