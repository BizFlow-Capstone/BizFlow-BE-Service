using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class NotificationRetentionJob
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationRetentionJob> _logger;

        public NotificationRetentionJob(
            INotificationService notificationService,
            ILogger<NotificationRetentionJob> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                await _notificationService.ArchiveExpiredNotificationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationRetentionJob failed.");
            }
        }
    }
}