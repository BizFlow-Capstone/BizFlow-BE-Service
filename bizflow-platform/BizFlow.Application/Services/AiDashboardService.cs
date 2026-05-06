using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.DTOs.Ai;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;

namespace BizFlow.Application.Services
{
    public class AiDashboardService : IAiDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AiDashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AiForecastReadDto> GetForecastAsync(int locationId)
        {
            var forecasts = await _unitOfWork.AiRevenueForecasts
                .GetByLocationAsync(locationId.ToString());

            return new AiForecastReadDto
            {
                Forecasts = forecasts.Select(f => new AiForecastItemDto
                {
                    ForecastDate = f.ForecastDate,
                    PredictedRevenue = f.PredictedRevenue,
                    LowerBound = f.LowerBound,
                    UpperBound = f.UpperBound,
                    TrendNote = f.TrendNote,
                    GeneratedAt = f.GeneratedAt,
                }).ToList()
            };
        }

        public async Task<List<AiReorderItemReadDto>> GetReorderSuggestionsAsync(int locationId)
        {
            var suggestions = await _unitOfWork.AiReorderSuggestions
                .GetByLocationAsync(locationId.ToString());

            return suggestions.Select(r => new AiReorderItemReadDto
            {
                ProductId = r.ProductId,
                CurrentStock = r.CurrentStock,
                DaysUntilStockout = r.DaysUntilStockout,
                SuggestedQuantity = r.SuggestedQuantity,
                AvgDailySales = r.AvgDailySales,
                Urgency = r.Urgency,
                GeneratedAt = r.GeneratedAt,
            }).ToList();
        }

        public async Task<List<AiProductInsightReadDto>> GetProductInsightsAsync(int locationId)
        {
            var insights = await _unitOfWork.AiProductInsights
                .GetByLocationAsync(locationId.ToString());

            return insights.Select(i => new AiProductInsightReadDto
            {
                ProductId = i.ProductId,
                InsightType = i.InsightType,
                Rank = i.Rank,
                MetricValue = i.MetricValue,
                PeriodDays = i.PeriodDays,
                GeneratedAt = i.GeneratedAt,
            }).ToList();
        }

        public async Task<List<AiAnomalyAlertReadDto>> GetAnomaliesAsync(int locationId, bool? acknowledged)
        {
            var alerts = await _unitOfWork.AiAnomalyAlerts
                .GetByLocationAsync(locationId.ToString(), acknowledged);

            return alerts.Select(a => new AiAnomalyAlertReadDto
            {
                Id = a.Id,
                AlertType = a.AlertType,
                Severity = a.Severity,
                Tier = a.Tier,
                ReferenceDate = a.ReferenceDate,
                Description = a.Description,
                ReferenceId = a.ReferenceId,
                RecordType = a.RecordType,
                IsAcknowledged = a.IsAcknowledged,
                GeneratedAt = a.GeneratedAt,
            }).ToList();
        }

        public async Task AcknowledgeAnomalyAsync(string id, int locationId)
        {
            var alert = await _unitOfWork.AiAnomalyAlerts.GetByIdAsync(id);
            if (alert == null || alert.LocationId != locationId.ToString())
                throw new NotFoundException(MessageKeys.NotFound);

            alert.IsAcknowledged = true;
            _unitOfWork.AiAnomalyAlerts.Update(alert);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
