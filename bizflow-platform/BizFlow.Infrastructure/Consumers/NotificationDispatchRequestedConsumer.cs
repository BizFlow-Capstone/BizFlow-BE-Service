using BizFlow.Application.DTOs.Notification;
using BizFlow.Application.Interfaces.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Consumers
{
    public class NotificationDispatchRequestedConsumer : IConsumer<NotificationDispatchRequestedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationDispatchRequestedConsumer> _logger;

        public NotificationDispatchRequestedConsumer(
            INotificationService notificationService,
            ILogger<NotificationDispatchRequestedConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<NotificationDispatchRequestedEvent> context)
        {
            try
            {
                await _notificationService.ProcessDispatchAsync(context.Message.DispatchId, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process notification dispatch event for DispatchId={DispatchId}", context.Message.DispatchId);
                throw;
            }
        }
    }
}