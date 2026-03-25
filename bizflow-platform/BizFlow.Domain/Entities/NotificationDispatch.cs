using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class NotificationDispatch
{
    public long NotificationDispatchId { get; set; }

    public Guid? NotificationTemplateId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string? DataJson { get; set; }

    public string? ActionType { get; set; }

    public string? TargetScreen { get; set; }

    public string? ActionPayloadJson { get; set; }

    public string RecipientScope { get; set; } = null!;

    public string? RecipientUserIdsJson { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string Status { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual NotificationTemplate? NotificationTemplate { get; set; }

    public virtual Profile? CreatedByUser { get; set; }

}