using System;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Stripe webhook event persistence for absolute idempotency and replay safety.
/// </summary>
public partial class StripeWebhookEvent
{
    public int StripeWebhookEventId { get; set; }

    public string EventId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public DateTime StripeCreatedAt { get; set; }

    public DateTime ReceivedAt { get; set; }

    public string ProcessingStatus { get; set; } = null!;

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
