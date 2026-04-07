using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class AiForecastJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiServiceClient _aiServiceClient;
        private readonly ILogger<AiForecastJob> _logger;

        public AiForecastJob(
            IUnitOfWork unitOfWork,
            IAiServiceClient aiServiceClient,
            ILogger<AiForecastJob> logger)
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
                _logger.LogInformation("AiForecastJob: no active locations, skipping");
                return;
            }

            var result = await _aiServiceClient.TriggerForecastAsync(locationIds);
            _logger.LogInformation(
                "AiForecastJob completed: processed={Processed}, skipped={Skipped}",
                result.Processed, result.Skipped);
        }
    }
}
