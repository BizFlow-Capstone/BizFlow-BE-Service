using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

/// <summary>
/// Mapping features to subscription plans and defining usage limits
/// </summary>
public partial class PlanFeatures
{
    public int SubscriptionPlanId { get; set; }

    public int FeatureId { get; set; }

    public int UsageLimit { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Features Feature { get; set; } = null!;

    public virtual SubscriptionPlans SubscriptionPlan { get; set; } = null!;
}
