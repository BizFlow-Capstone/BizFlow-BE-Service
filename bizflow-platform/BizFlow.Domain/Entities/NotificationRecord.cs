using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class NotificationRecord
{
    public Guid NotificationId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? ActionType { get; set; }

    public string? TargetScreen { get; set; }

    public string? ActionPayloadJson { get; set; }

    public string? DataJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
}
