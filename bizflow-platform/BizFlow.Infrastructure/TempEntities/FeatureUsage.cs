using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

/// <summary>
/// Tracking usage of limited features per subscription period
/// </summary>
public partial class FeatureUsages
{
    public int FeatureUsageId { get; set; }

    public Guid SubscriptionId { get; set; }

    public int FeatureId { get; set; }

    public int UsedCount { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Features Feature { get; set; } = null!;

    public virtual Subscriptions Subscription { get; set; } = null!;
}
