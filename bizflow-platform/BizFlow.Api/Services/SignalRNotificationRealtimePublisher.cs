using BizFlow.Api.Hubs;
using BizFlow.Application.DTOs.Notification;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace BizFlow.Api.Services
{
    public class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationRealtimePublisher(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PublishUserNotificationAsync(Guid userId, UserNotificationDto notification, CancellationToken cancellationToken = default)
        {
            var group = NotificationHub.BuildUserGroup(userId.ToString());
            await _hubContext.Clients.Group(group)
                .SendAsync("notification.received", notification, cancellationToken);
        }
    }
}