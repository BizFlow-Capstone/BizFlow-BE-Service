using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    /// <summary>
    /// Hangfire job: checks a single record for anomalies via AI Service,
    /// then sends FCM push if any CRITICAL alert was created.
    /// </summary>
    public class AiAnomalyCheckJob
    {
        private readonly IAiServiceClient _aiServiceClient;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AiAnomalyCheckJob> _logger;

        public AiAnomalyCheckJob(
            IAiServiceClient aiServiceClient,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            ILogger<AiAnomalyCheckJob> logger)
        {
            _aiServiceClient = aiServiceClient;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteAsync(int locationId, string recordType, long recordId)
        {
            try
            {
                var result = await _aiServiceClient.CheckAnomalyAsync(locationId, recordType, recordId);

                if (result.HasCritical)
                {
                    var ownerId = await _unitOfWork.BusinessLocations.GetOwnerIdByLocationAsync(locationId);
                    if (ownerId != null)
                    {
                        var alertDesc = result.Alerts.FirstOrDefault(a => a.Severity == "CRITICAL")?.Description
                            ?? "Phát hiện bất thường nghiêm trọng trong dữ liệu.";

                        await _notificationService.SendToAllDevicesAsync(
                            ownerId.Value,
                            "Cảnh báo dữ liệu bất thường",
                            alertDesc);
                    }
                }

                _logger.LogInformation(
                    "AiAnomalyCheckJob: locationId={LocationId}, recordType={RecordType}, recordId={RecordId}, alertsCreated={Alerts}, hasCritical={Critical}",
                    locationId, recordType, recordId, result.AlertsCreated, result.HasCritical);
            }
            catch (Exception ex)
            {
                // Non-critical: log and swallow — anomaly check failure must not break anything
                _logger.LogWarning(ex,
                    "AiAnomalyCheckJob failed for locationId={LocationId}, recordType={RecordType}, recordId={RecordId}",
                    locationId, recordType, recordId);
            }
        }
    }
}
