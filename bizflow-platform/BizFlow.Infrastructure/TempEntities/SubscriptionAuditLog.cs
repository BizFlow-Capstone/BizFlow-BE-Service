using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

/// <summary>
/// Audit trail for subscription lifecycle events
/// </summary>
public partial class SubscriptionAuditLogs
{
    public int AuditLogId { get; set; }

    public Guid SubscriptionId { get; set; }

    public string Action { get; set; } = null!;

    public string? Details { get; set; }

    public Guid? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Subscriptions Subscription { get; set; } = null!;
}
