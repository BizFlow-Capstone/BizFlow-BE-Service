using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IAccountingBookRepository
{
    Task<AccountingBook?> GetByIdAsync(long bookId);
    Task<AccountingBook?> GetByIdWithBusinessTypesAsync(long bookId);
    Task<List<AccountingBook>> GetByLocationAndPeriodAsync(int locationId, long periodId);
    Task<bool> ExistsForPeriodAsync(int locationId, long periodId, int templateVersionId);
    Task AddAsync(AccountingBook book);
    void Update(AccountingBook book);
}
