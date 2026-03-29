using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Audit trail for subscription lifecycle events
/// </summary>
public partial class SubscriptionAuditLog
{
    public int AuditLogId { get; set; }

    public Guid SubscriptionId { get; set; }

    public string Action { get; set; } = null!;

    public string? Details { get; set; }

    public Guid? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Subscription Subscription { get; set; } = null!;
}
