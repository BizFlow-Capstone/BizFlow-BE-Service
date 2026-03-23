using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Mapping features to subscription plans and defining usage limits
/// </summary>
public partial class PlanFeature
{
    public int SubscriptionPlanId { get; set; }

    public int FeatureId { get; set; }

    public int UsageLimit { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Feature Feature { get; set; } = null!;

    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}
