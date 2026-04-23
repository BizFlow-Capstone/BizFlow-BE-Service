using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    /// <summary>
    /// Nightly reconciliation job: backfill toàn bộ sản phẩm Active của mọi location
    /// vào ChromaDB vector store.
    ///
    /// Tại sao cần job này (dù ProductService đã fire-and-forget sync):
    ///   - Nếu AI service down/restart đúng lúc product được tạo/sửa → sync bị bỏ qua im lặng.
    ///   - Job này chạy 04:00 (VN time) mỗi ngày, backfill idempotent → đảm bảo drift được
    ///     tự động recover sau 1 đêm tối đa.
    /// </summary>
    public class AiVectorStoreSyncJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiServiceClient _aiServiceClient;
        private readonly ILogger<AiVectorStoreSyncJob> _logger;

        public AiVectorStoreSyncJob(
            IUnitOfWork unitOfWork,
            IAiServiceClient aiServiceClient,
            ILogger<AiVectorStoreSyncJob> logger)
        {
            _unitOfWork = unitOfWork;
            _aiServiceClient = aiServiceClient;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var locationIds = await _unitOfWork.BusinessLocations.GetAllActiveLocationIdsAsync();
            if (locationIds.Count == 0)
            {
                _logger.LogInformation("AiVectorStoreSyncJob: no active locations, skipping");
                return;
            }

            _logger.LogInformation(
                "AiVectorStoreSyncJob started: reconciling vector store for {Count} locations",
                locationIds.Count);

            int totalSynced = 0;
            int totalSkipped = 0;
            int failedLocations = 0;

            foreach (var locationId in locationIds)
            {
                try
                {
                    var result = await _aiServiceClient.TriggerVectorStoreBackfillAsync(locationId);
                    totalSynced += result.Synced;
                    totalSkipped += result.Skipped;
                    _logger.LogDebug(
                        "AiVectorStoreSyncJob: location={LocationId} synced={Synced} skipped={Skipped}",
                        locationId, result.Synced, result.Skipped);
                }
                catch (Exception ex)
                {
                    failedLocations++;
                    _logger.LogWarning(ex,
                        "AiVectorStoreSyncJob: backfill failed for location={LocationId}, continuing with remaining locations",
                        locationId);
                }
            }

            _logger.LogInformation(
                "AiVectorStoreSyncJob completed: totalSynced={TotalSynced}, totalSkipped={TotalSkipped}, failedLocations={FailedLocations}/{Total}",
                totalSynced, totalSkipped, failedLocations, locationIds.Count);
        }
    }
}
