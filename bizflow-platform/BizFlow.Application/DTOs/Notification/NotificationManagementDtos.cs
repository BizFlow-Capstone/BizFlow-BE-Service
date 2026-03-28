using BizFlow.Application.Common.Models;

namespace BizFlow.Application.DTOs.Notification
{
    public class NotificationActionCatalogDto
    {
        public List<NotificationActionTypeDto> ActionTypes { get; set; } = new();
        public List<NotificationTargetDto> Targets { get; set; } = new();
        public List<NotificationTriggerDto> Triggers { get; set; } = new();
        public List<NotificationTemplatePlaceholderDto> Placeholders { get; set; } = new();
    }

    public class NotificationTemplatePlaceholderDto
    {
        public string Key { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ExampleValue { get; set; } = string.Empty;
    }

    public class NotificationTriggerDto
    {
        public string EventCode { get; set; } = string.Empty;
        public string Feature { get; set; } = string.Empty;
        public string TriggerDescription { get; set; } = string.Empty;
    }

    public class NotificationActionTypeDto
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class NotificationTargetDto
    {
        public string Code { get; set; } = string.Empty;
        public string TargetScreen { get; set; } = string.Empty;
        public string MobileRoute { get; set; } = string.Empty;
        public string WebRoute { get; set; } = string.Empty;
        public List<string> Aliases { get; set; } = new();
        public string PayloadExampleJson { get; set; } = string.Empty;
    }

    public class NotificationTemplateDto
    {
        public Guid NotificationTemplateId { get; set; }
        public string EventCode { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public string TitleTemplate { get; set; } = string.Empty;
        public string ContentTemplate { get; set; } = string.Empty;
        public string? DefaultActionType { get; set; }
        public string? DefaultTargetScreen { get; set; }
        public string? DefaultActionPayloadJson { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpsertNotificationTemplateRequest
    {
        public string NotificationType { get; set; } = string.Empty;
        public string TitleTemplate { get; set; } = string.Empty;
        public string ContentTemplate { get; set; } = string.Empty;
        public string? DefaultActionType { get; set; }
        public string? DefaultTargetScreen { get; set; }
        public string? DefaultActionPayloadJson { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ToggleNotificationTemplateRequest
    {
        public bool IsActive { get; set; }
    }

    public class CreateNotificationDispatchRequest
    {
        public string? EventCode { get; set; }
        public string? NotificationType { get; set; }
        public string? Priority { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public Dictionary<string, string>? TemplateData { get; set; }
        public string? DataJson { get; set; }
        public string? ActionType { get; set; }
        public string? TargetScreen { get; set; }
        public string? ActionPayloadJson { get; set; }
        public bool SendToAllUsers { get; set; }
        public List<Guid>? RecipientUserIds { get; set; }
        public DateTime? ScheduledAt { get; set; }
    }

    public class NotificationDispatchDto
    {
        public long NotificationDispatchId { get; set; }
        public Guid? NotificationTemplateId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string RecipientScope { get; set; } = string.Empty;
        public string? RecipientUserIdsJson { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? SentAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class NotificationDispatchQueryParams : PaginationParams
    {
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class UserNotificationDto
    {
        public long UserNotificationId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ActionType { get; set; }
        public string? TargetScreen { get; set; }
        public string? ActionPayloadJson { get; set; }
        public string DeliveryStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class NotificationQueryParams : PaginationParams
    {
        public bool? UnreadOnly { get; set; }
        public string? NotificationType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class NotificationUnreadCountDto
    {
        public int UnreadCount { get; set; }
    }
}