using BizFlow.Application.DTOs.Dashboard;

namespace BizFlow.Application.Interfaces.Services;

public interface IUserDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        Guid userId,
        DashboardSummaryQuery query,
        CancellationToken cancellationToken = default);
}
