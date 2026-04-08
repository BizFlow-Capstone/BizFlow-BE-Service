using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class ScheduledNotificationDispatchJob
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<ScheduledNotificationDispatchJob> _logger;

        public ScheduledNotificationDispatchJob(
            INotificationService notificationService,
            ILogger<ScheduledNotificationDispatchJob> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                await _notificationService.ProcessDueDispatchesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ScheduledNotificationDispatchJob failed.");
            }
        }
    }
}