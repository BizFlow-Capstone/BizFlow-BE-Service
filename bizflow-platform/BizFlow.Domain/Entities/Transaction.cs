using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Payment transactions for subscriptions
/// </summary>
public partial class Transaction
{
    public Guid TransactionId { get; set; }

    public Guid ProfileId { get; set; }

    public int SubscriptionPlanId { get; set; }

    public Guid? SubscriptionId { get; set; }

    public string? StripeCheckoutSessionId { get; set; }

    public string? StripePaymentIntentId { get; set; }

    public string IdempotencyKey { get; set; } = null!;

    public string TransactionType { get; set; } = null!;

    public decimal PlanPrice { get; set; }

    public decimal ProrationCredit { get; set; }

    public decimal FinalAmount { get; set; }

    public string Currency { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Profile Profile { get; set; } = null!;

    public virtual Subscription? Subscription { get; set; }

    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}
