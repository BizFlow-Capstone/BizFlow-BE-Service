using BizFlow.Application.DTOs.Accounting;

namespace BizFlow.Application.Interfaces.Services;

public interface IAccountingPeriodService
{
    Task<AccountingPeriodDto> CreatePeriodAsync(int locationId, Guid userId, CreateAccountingPeriodRequest request);
    Task<AccountingPeriodDto> CreateCustomPeriodAsync(int locationId, Guid userId, CreateCustomAccountingPeriodRequest request);
    Task<OpeningBalanceSuggestionDto> GetOpeningBalanceSuggestionAsync(int locationId, Guid userId, OpeningBalanceSuggestionRequest request);
    Task<List<AccountingPeriodDto>> GetPeriodsAsync(int locationId, Guid userId);
    Task<AccountingPeriodDto> GetPeriodDetailAsync(int locationId, long periodId, Guid userId);
    Task<AccountingPeriodDto> FinalizePeriodAsync(int locationId, long periodId, Guid userId);
    Task<AccountingPeriodDto> ReopenPeriodAsync(int locationId, long periodId, Guid userId, string reason);
    Task DeletePeriodAsync(int locationId, long periodId, Guid userId);
    Task<List<AccountingPeriodAuditLogDto>> GetAuditLogsAsync(int locationId, long periodId, Guid userId);
}