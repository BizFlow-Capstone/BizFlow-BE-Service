using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// User subscriptions tracking
/// </summary>
public partial class Subscription
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

    public virtual ICollection<FeatureUsage> FeatureUsages { get; set; } = new List<FeatureUsage>();

    public virtual Profile OwnerProfile { get; set; } = null!;

    public virtual ICollection<SubscriptionAuditLog> SubscriptionAuditLogs { get; set; } = new List<SubscriptionAuditLog>();

    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
