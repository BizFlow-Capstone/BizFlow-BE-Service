using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IAccountingPeriodRepository
{
    Task<bool> ExistsAsync(int locationId, string periodType, short year, int? quarter);
    Task<AccountingPeriod?> GetByLocationAndIdAsync(int locationId, long periodId);
    Task<List<AccountingPeriod>> GetByLocationAsync(int locationId);
    Task<AccountingPeriod?> GetPreviousPeriodAsync(int locationId, string periodType, DateOnly currentStartDate);
    Task<bool> ExistsOverlappingPeriodAsync(int locationId, DateOnly startDate, DateOnly endDate);
    Task<(decimal NetCash, decimal NetBank)> CalculateNetCashAndBankAsync(int locationId, DateOnly startDate, DateOnly endDate);
    Task<long> CountActiveBooksAsync(long periodId);

    Task<bool> HasTaxPaymentsAsync(long periodId);
    Task RemoveAuditLogsAsync(long periodId);

    Task AddAsync(AccountingPeriod period);
    void Update(AccountingPeriod period);
    void Remove(AccountingPeriod period);

    Task AddAuditLogAsync(AccountingPeriodAuditLog log);
    Task<List<AccountingPeriodAuditLog>> GetAuditLogsAsync(long periodId);
}