using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IAccountingBookRepository
{
    Task<AccountingBook?> GetByIdAsync(long bookId);
    Task<AccountingBook?> GetByIdWithBusinessTypesAsync(long bookId);
    Task<List<AccountingBook>> GetByLocationAndPeriodAsync(int locationId, long periodId);
    Task<bool> ExistsForPeriodAsync(long periodId, int templateVersionId, string taxProfileKey);
    Task AddAsync(AccountingBook book);
    void Update(AccountingBook book);
}
