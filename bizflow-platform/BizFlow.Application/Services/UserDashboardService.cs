using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Dashboard;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Dashboard;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services;

public class UserDashboardService : IUserDashboardService
{
    private readonly IUnitOfWork _uow;
    private readonly IBusinessLocationService _locationService;

    public UserDashboardService(IUnitOfWork uow, IBusinessLocationService locationService)
    {
        _uow = uow;
        _locationService = locationService;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        Guid userId,
        DashboardSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!DashboardPeriodResolver.TryParsePeriod(query.Period, out var period))
            throw new BadRequestException(MessageKeys.DashboardInvalidPeriod);

        if (period == DashboardPeriod.Custom)
        {
            if (!query.FromDate.HasValue || !query.ToDate.HasValue)
                throw new BadRequestException(MessageKeys.DashboardCustomDatesRequired);

            if (query.FromDate.Value > query.ToDate.Value)
                throw new BadRequestException(MessageKeys.LedgerInvalidDateRange);
        }

        var referenceDate = query.ReferenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var (fromDate, toDate) = DashboardPeriodResolver.Resolve(
            period,
            referenceDate,
            query.FromDate,
            query.ToDate);

        int? singleLocationId = query.BusinessLocationId;
        IReadOnlyList<int> locationIds;

        if (singleLocationId.HasValue)
        {
            await _locationService.ValidateLocationAccessAsync(userId, singleLocationId.Value);
            locationIds = [singleLocationId.Value];
        }
        else
        {
            locationIds = await GetAccessibleLocationIdsAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        var (completedFromUtc, completedToUtc) =
            DashboardPeriodResolver.ToUtcInclusiveDateTimeRange(fromDate, toDate);

        // Same scoped DbContext: do not run EF queries concurrently (Task.WhenAll).
        var totalRevenue = await _uow.Revenues.SumAmountByLocationsAndDateRangeAsync(
            locationIds,
            fromDate,
            toDate,
            cancellationToken).ConfigureAwait(false);

        var totalCost = await _uow.Costs.SumAmountByLocationsAndDateRangeAsync(
            locationIds,
            fromDate,
            toDate,
            cancellationToken).ConfigureAwait(false);

        var totalCompletedOrders = await _uow.Orders.CountCompletedByLocationsAndCompletedAtUtcAsync(
            locationIds,
            completedFromUtc,
            completedToUtc,
            cancellationToken).ConfigureAwait(false);

        var outstandingDebtNetChangeInPeriod =
            await _uow.Debtors.SumDebtBalanceChangeByLocationsAndPaidAtUtcRangeAsync(
                locationIds,
                completedFromUtc,
                completedToUtc,
                cancellationToken).ConfigureAwait(false);

        return new DashboardSummaryResponse
        {
            BusinessLocationId = singleLocationId,
            IncludedLocationCount = locationIds.Count,
            FromDate = fromDate,
            ToDate = toDate,
            TotalRevenue = totalRevenue,
            TotalCost = totalCost,
            TotalCompletedOrders = totalCompletedOrders,
            OutstandingDebtNetChangeInPeriod = outstandingDebtNetChangeInPeriod
        };
    }

    private async Task<IReadOnlyList<int>> GetAccessibleLocationIdsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var owned = await _locationService.GetOwnedLocationsAsync(userId).ConfigureAwait(false);
        var work = await _locationService.GetWorkLocationsAsync(userId).ConfigureAwait(false);

        return owned
            .Select(l => l.Id)
            .Concat(work.Select(l => l.Id))
            .Distinct()
            .ToList();
    }
}
