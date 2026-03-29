using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Notification;

namespace BizFlow.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task RegisterDeviceTokenAsync(Guid userId, string token, string? deviceName, string platform);
        Task UnregisterDeviceTokenAsync(Guid userId, string token);
        Task SendToAllDevicesAsync(Guid userId, string title, string body);
        Task SendSilentNotificationAsync(Guid userId, Dictionary<string, string> data);
        Task SendEmployeeInviteAsync(Guid employeeId, string ownerName);
        Task NotifyEmployeeRemovedAsync(Guid employeeId, string businessName);
        Task NotifySubscriptionExpiringAsync(Guid ownerId, string planName, int daysRemaining, decimal price);
        Task NotifySubscriptionExpiredAsync(Guid ownerId, string planName);

        Task<NotificationActionCatalogDto> GetActionCatalogAsync();
        Task<IEnumerable<NotificationTemplateDto>> GetTemplatesAsync();
        Task<NotificationTemplateDto?> GetTemplateByEventCodeAsync(string eventCode);
        Task<NotificationTemplateDto> UpsertTemplateAsync(string eventCode, UpsertNotificationTemplateRequest request);
        Task<NotificationTemplateDto> ToggleTemplateAsync(string eventCode, bool isActive);

        Task<NotificationDispatchDto> CreateDispatchAsync(Guid createdByUserId, CreateNotificationDispatchRequest request);
        Task ProcessDispatchAsync(long dispatchId, CancellationToken cancellationToken = default);
        Task ProcessDueDispatchesAsync(CancellationToken cancellationToken = default);
        Task ProcessNotificationOutboxAsync(CancellationToken cancellationToken = default);
        Task ArchiveExpiredNotificationsAsync(CancellationToken cancellationToken = default);
        Task<PaginatedResponse<NotificationDispatchDto>> GetDispatchesAsync(NotificationDispatchQueryParams query);

        Task<PaginatedResponse<UserNotificationDto>> GetUserNotificationsAsync(Guid userId, NotificationQueryParams query);
        Task<UserNotificationDto?> GetUserNotificationDetailAsync(Guid userId, long userNotificationId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid userId, long userNotificationId);
        Task<int> MarkAllAsReadAsync(Guid userId);
    }
}
