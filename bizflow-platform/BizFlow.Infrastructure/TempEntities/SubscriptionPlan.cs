using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

/// <summary>
/// Available subscription plans
/// </summary>
public partial class SubscriptionPlans
{
    public int SubscriptionPlanId { get; set; }

    public string Name { get; set; } = null!;

    public int Tier { get; set; }

    public decimal BasePrice { get; set; }

    public decimal? DiscountedPrice { get; set; }

    public int DurationDays { get; set; }

    public string? StripePriceId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PlanFeatures> PlanFeatures { get; set; } = new List<PlanFeatures>();

    public virtual ICollection<Subscriptions> Subscriptions { get; set; } = new List<Subscriptions>();

    public virtual ICollection<Transactions> Transactions { get; set; } = new List<Transactions>();
}
