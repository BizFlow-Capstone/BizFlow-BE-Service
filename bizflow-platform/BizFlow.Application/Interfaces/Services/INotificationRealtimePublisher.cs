using BizFlow.Application.DTOs.Notification;

namespace BizFlow.Application.Interfaces.Services
{
    public interface INotificationRealtimePublisher
    {
        Task PublishUserNotificationAsync(Guid userId, UserNotificationDto notification, CancellationToken cancellationToken = default);
    }
}