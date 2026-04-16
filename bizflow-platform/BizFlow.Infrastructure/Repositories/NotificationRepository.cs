using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;

namespace BizFlow.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly BizFlowDbContext _dbContext;

        public NotificationRepository(BizFlowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<DeviceToken> DeviceTokens => _dbContext.DeviceTokens;
        public IQueryable<Profile> Profiles => _dbContext.Profiles;
        public IQueryable<NotificationTemplate> NotificationTemplates => _dbContext.NotificationTemplates;
        public IQueryable<NotificationDispatch> NotificationDispatches => _dbContext.NotificationDispatches;
        public IQueryable<BusinessLocation> BusinessLocations => _dbContext.BusinessLocations;
        public IQueryable<UserNotification> UserNotifications => _dbContext.UserNotifications;
        public IQueryable<NotificationRecord> Notifications => _dbContext.Notifications;
        public IQueryable<NotificationOutboxMessage> NotificationOutboxMessages => _dbContext.NotificationOutboxMessages;
        public IQueryable<UserLocationAssignment> UserLocationAssignments => _dbContext.UserLocationAssignments;
        public IQueryable<Credential> Credentials => _dbContext.Credentials;

        public Task AddDeviceTokenAsync(DeviceToken deviceToken)
        {
            return _dbContext.DeviceTokens.AddAsync(deviceToken).AsTask();
        }

        public Task AddNotificationTemplateAsync(NotificationTemplate notificationTemplate)
        {
            return _dbContext.NotificationTemplates.AddAsync(notificationTemplate).AsTask();
        }

        public Task AddNotificationDispatchAsync(NotificationDispatch notificationDispatch)
        {
            return _dbContext.NotificationDispatches.AddAsync(notificationDispatch).AsTask();
        }

        public Task AddNotificationRecordAsync(NotificationRecord notificationRecord)
        {
            return _dbContext.Notifications.AddAsync(notificationRecord).AsTask();
        }

        public Task AddUserNotificationsAsync(IEnumerable<UserNotification> userNotifications)
        {
            return _dbContext.UserNotifications.AddRangeAsync(userNotifications);
        }

        public Task AddNotificationOutboxMessageAsync(NotificationOutboxMessage notificationOutboxMessage)
        {
            return _dbContext.NotificationOutboxMessages.AddAsync(notificationOutboxMessage).AsTask();
        }
    }
}