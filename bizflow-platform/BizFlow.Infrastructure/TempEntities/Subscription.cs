using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

/// <summary>
/// User subscriptions tracking
/// </summary>
public partial class Subscriptions
{
    public Guid SubscriptionId { get; set; }

    public Guid OwnerProfileId { get; set; }

    public int SubscriptionPlanId { get; set; }

    public string Status { get; set; } = null!;

    public bool IsAutoRenew { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime? LastReminderSentAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<FeatureUsages> FeatureUsages { get; set; } = new List<FeatureUsages>();

    public virtual Profiles OwnerProfile { get; set; } = null!;

    public virtual ICollection<SubscriptionAuditLogs> SubscriptionAuditLogs { get; set; } = new List<SubscriptionAuditLogs>();

    public virtual SubscriptionPlans SubscriptionPlan { get; set; } = null!;

    public virtual ICollection<Transactions> Transactions { get; set; } = new List<Transactions>();
}
