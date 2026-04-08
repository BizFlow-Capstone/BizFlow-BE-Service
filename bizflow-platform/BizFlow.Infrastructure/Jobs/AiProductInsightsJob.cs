using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class AiProductInsightsJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiServiceClient _aiServiceClient;
        private readonly ILogger<AiProductInsightsJob> _logger;

        public AiProductInsightsJob(
            IUnitOfWork unitOfWork,
            IAiServiceClient aiServiceClient,
            ILogger<AiProductInsightsJob> logger)
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
                _logger.LogInformation("AiProductInsightsJob: no active locations, skipping");
                return;
            }

            var result = await _aiServiceClient.TriggerProductInsightsAsync(locationIds);
            _logger.LogInformation(
                "AiProductInsightsJob completed: processed={Processed}, skipped={Skipped}",
                result.Processed, result.Skipped);
        }
    }
}
