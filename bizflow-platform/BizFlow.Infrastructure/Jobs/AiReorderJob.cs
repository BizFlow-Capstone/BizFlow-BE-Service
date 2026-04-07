using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class AiReorderJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiServiceClient _aiServiceClient;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AiReorderJob> _logger;

        public AiReorderJob(
            IUnitOfWork unitOfWork,
            IAiServiceClient aiServiceClient,
            INotificationService notificationService,
            ILogger<AiReorderJob> logger)
        {
            _unitOfWork = unitOfWork;
            _aiServiceClient = aiServiceClient;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var locationIds = await _unitOfWork.BusinessLocations.GetAllActiveLocationIdsAsync();
            if (locationIds.Count == 0)
            {
                _logger.LogInformation("AiReorderJob: no active locations, skipping");
                return;
            }

            var result = await _aiServiceClient.TriggerReorderAsync(locationIds);
            _logger.LogInformation(
                "AiReorderJob completed: processed={Processed}, skipped={Skipped}",
                result.Processed, result.Skipped);

            // Push FCM to owners of locations that have HIGH-urgency reorder items
            await NotifyHighUrgencyReordersAsync(locationIds);
        }

        private async Task NotifyHighUrgencyReordersAsync(List<int> locationIds)
        {
            foreach (var locationId in locationIds)
            {
                var highUrgencyItems = await _unitOfWork.AiReorderSuggestions
                    .GetByLocationAndUrgencyAsync(locationId.ToString(), "HIGH");

                if (highUrgencyItems.Count == 0)
                    continue;

                var ownerId = await _unitOfWork.BusinessLocations.GetOwnerIdByLocationAsync(locationId);
                if (ownerId == null) continue;

                await _notificationService.SendToAllDevicesAsync(
                    ownerId.Value,
                    "Cảnh báo tồn kho",
                    $"Có {highUrgencyItems.Count} sản phẩm sắp hết hàng cần nhập thêm.");
            }
        }
    }
}
