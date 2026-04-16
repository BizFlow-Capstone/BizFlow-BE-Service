using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        IQueryable<DeviceToken> DeviceTokens { get; }
        IQueryable<Profile> Profiles { get; }
        IQueryable<NotificationTemplate> NotificationTemplates { get; }
        IQueryable<NotificationDispatch> NotificationDispatches { get; }
        IQueryable<BusinessLocation> BusinessLocations { get; }
        IQueryable<UserNotification> UserNotifications { get; }
        IQueryable<NotificationRecord> Notifications { get; }
        IQueryable<NotificationOutboxMessage> NotificationOutboxMessages { get; }
        IQueryable<UserLocationAssignment> UserLocationAssignments { get; }
        IQueryable<Credential> Credentials { get; }

        Task AddDeviceTokenAsync(DeviceToken deviceToken);
        Task AddNotificationTemplateAsync(NotificationTemplate notificationTemplate);
        Task AddNotificationDispatchAsync(NotificationDispatch notificationDispatch);
        Task AddNotificationRecordAsync(NotificationRecord notificationRecord);
        Task AddUserNotificationsAsync(IEnumerable<UserNotification> userNotifications);
        Task AddNotificationOutboxMessageAsync(NotificationOutboxMessage notificationOutboxMessage);
    }
}