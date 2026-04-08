using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Constants;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class SubscriptionReminderJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBusinessLocationRepository _businessLocationRepository;
        private readonly INotificationService _notificationService;
        private readonly IMessageService _messageService;
        private readonly ILogger<SubscriptionReminderJob> _logger;

        public SubscriptionReminderJob(
            IUnitOfWork unitOfWork,
            IBusinessLocationRepository businessLocationRepository,
            INotificationService notificationService,
            IMessageService messageService,
            ILogger<SubscriptionReminderJob> logger)
        {
            _unitOfWork = unitOfWork;
            _businessLocationRepository = businessLocationRepository;
            _notificationService = notificationService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var now = DateTime.UtcNow;
            var activeSubscriptions = await _unitOfWork.Subscriptions.GetAllActiveForSyncAsync();

            var t3Window = activeSubscriptions
                .Where(s => s.SubscriptionPlan.DurationDays > 0
                         && s.EndDate >= now.AddDays(2.5)
                         && s.EndDate < now.AddDays(3.5)
                         && (s.LastReminderSentAt == null || s.LastReminderSentAt < now.AddDays(-1)))
                .ToList();

            foreach (var subscription in t3Window)
            {
                var latestPrice = subscription.SubscriptionPlan.Prices.FirstOrDefault(p => p.IsActive);
                var price = latestPrice?.GetEffectivePrice() ?? 0m;
                await _notificationService.NotifySubscriptionExpiringAsync(
                    subscription.OwnerProfileId,
                    subscription.SubscriptionPlan.Name,
                    3,
                    price);

                subscription.LastReminderSentAt = now;
                subscription.UpdatedAt = now;
            }

            var t1Window = activeSubscriptions
                .Where(s => s.SubscriptionPlan.DurationDays > 0
                         && s.EndDate >= now.AddHours(12)
                         && s.EndDate < now.AddDays(1.5)
                         && (s.LastReminderSentAt == null || s.LastReminderSentAt < now.AddHours(-12)))
                .ToList();

            foreach (var subscription in t1Window)
            {
                var latestPrice = subscription.SubscriptionPlan.Prices.FirstOrDefault(p => p.IsActive);
                var price = latestPrice?.GetEffectivePrice() ?? 0m;

                await _notificationService.NotifySubscriptionExpiringAsync(
                    subscription.OwnerProfileId,
                    subscription.SubscriptionPlan.Name,
                    1,
                    price);

                var ownerLocations = await _businessLocationRepository.GetLocationsByUserAsync(subscription.OwnerProfileId, true);
                var employeeIds = new HashSet<Guid>();
                foreach (var location in ownerLocations)
                {
                    var locationEmployees = await _businessLocationRepository.GetAssignedEmployeeIdsAsync(location.Id);
                    foreach (var employeeId in locationEmployees)
                    {
                        employeeIds.Add(employeeId);
                    }
                }

                foreach (var employeeId in employeeIds)
                {
                    await _notificationService.SendToAllDevicesAsync(
                        employeeId,
                        _messageService.GetMessage(MessageKeys.SubscriptionEmployeeExpiringTitle),
                        _messageService.GetMessage(MessageKeys.SubscriptionEmployeeExpiringBody));
                }

                subscription.LastReminderSentAt = now;
                subscription.UpdatedAt = now;
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "SubscriptionReminderJob completed. T-3 reminders: {T3Count}, T-1 reminders: {T1Count}",
                t3Window.Count,
                t1Window.Count);
        }
    }
}
