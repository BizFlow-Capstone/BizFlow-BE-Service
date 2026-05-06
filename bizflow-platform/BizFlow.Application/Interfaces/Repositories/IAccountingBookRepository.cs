using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IAccountingBookRepository
{
    Task<AccountingBook?> GetByIdAsync(long bookId);
    Task<AccountingBook?> GetByIdWithBusinessTypesAsync(long bookId);
    Task<AccountingBook?> GetByIdWithPeriodAsync(long bookId);
    Task<bool> HasExportsAsync(long bookId);
    Task<List<AccountingBook>> GetByLocationAndPeriodAsync(int locationId, long periodId);
    Task AddAsync(AccountingBook book);
    void Update(AccountingBook book);
    void Remove(AccountingBook book);
}
