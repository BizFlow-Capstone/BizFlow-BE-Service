using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Available subscription plans
/// </summary>
public partial class SubscriptionPlan
{
    public int SubscriptionPlanId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int DurationDays { get; set; }

    public string? StripeProductId { get; set; }

    public string? StripePriceId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();

    public virtual ICollection<SubscriptionPlanPrice> Prices { get; set; } = new List<SubscriptionPlanPrice>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
