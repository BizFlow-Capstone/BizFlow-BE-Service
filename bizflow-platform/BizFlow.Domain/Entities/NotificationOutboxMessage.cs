using System;

namespace BizFlow.Domain.Entities;

public partial class NotificationOutboxMessage
{
    public long NotificationOutboxMessageId { get; set; }

    public string EventType { get; set; } = null!;

    public string PayloadJson { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? LastError { get; set; }
}