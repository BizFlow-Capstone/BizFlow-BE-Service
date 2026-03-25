using System;

namespace BizFlow.Domain.Entities;

public partial class UserNotification
{
    public long UserNotificationId { get; set; }

    public Guid UserId { get; set; }

    public Guid? NotificationId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string? ActionType { get; set; }

    public string? TargetScreen { get; set; }

    public string? ActionPayloadJson { get; set; }

    public string DeliveryStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public string? ErrorMessage { get; set; }

    public virtual NotificationRecord? Notification { get; set; }

    public virtual Profile User { get; set; } = null!;
}