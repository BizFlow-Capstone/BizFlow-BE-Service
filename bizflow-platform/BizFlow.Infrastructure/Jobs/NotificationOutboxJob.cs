using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class NotificationOutboxJob
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationOutboxJob> _logger;

        public NotificationOutboxJob(
            INotificationService notificationService,
            ILogger<NotificationOutboxJob> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                await _notificationService.ProcessNotificationOutboxAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationOutboxJob failed.");
            }
        }
    }
}