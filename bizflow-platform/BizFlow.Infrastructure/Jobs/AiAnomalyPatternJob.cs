using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class AiAnomalyPatternJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiServiceClient _aiServiceClient;
        private readonly ILogger<AiAnomalyPatternJob> _logger;

        public AiAnomalyPatternJob(
            IUnitOfWork unitOfWork,
            IAiServiceClient aiServiceClient,
            ILogger<AiAnomalyPatternJob> logger)
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
                _logger.LogInformation("AiAnomalyPatternJob: no active locations, skipping");
                return;
            }

            var result = await _aiServiceClient.TriggerAnomalyPatternAsync(locationIds);
            _logger.LogInformation(
                "AiAnomalyPatternJob completed: processed={Processed}, skipped={Skipped}",
                result.Processed, result.Skipped);
        }
    }
}
