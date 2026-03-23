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

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Feature Feature { get; set; } = null!;

    public virtual Subscription Subscription { get; set; } = null!;
}
