using BizFlow.Application.Common.Constants;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories;

public class AccountingBookRepository : IAccountingBookRepository
{
    private readonly BizFlowDbContext _context;

    public AccountingBookRepository(BizFlowDbContext context)
    {
        _context = context;
    }

    public async Task<AccountingBook?> GetByIdAsync(long bookId)
    {
        return await _context.Set<AccountingBook>()
            .FirstOrDefaultAsync(x => x.BookId == bookId);
    }

    public async Task<AccountingBook?> GetByIdWithBusinessTypesAsync(long bookId)
    {
        return await _context.Set<AccountingBook>()
            .Include(x => x.TemplateVersion)
                .ThenInclude(v => v.Template)
            .Include(x => x.TemplateVersion)
                .ThenInclude(v => v.FieldMappings)
            .FirstOrDefaultAsync(x => x.BookId == bookId);
    }

    public async Task<List<AccountingBook>> GetByLocationAndPeriodAsync(int locationId, long periodId)
    {
        return await _context.Set<AccountingBook>()
            .Include(x => x.TemplateVersion)
                .ThenInclude(v => v.Template)
            .Where(x => x.BusinessLocationId == locationId && x.PeriodId == periodId && x.Status == AccountingBookConstants.BookStatuses.Active)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(AccountingBook book)
    {
        await _context.Set<AccountingBook>().AddAsync(book);
    }

    public void Update(AccountingBook book)
    {
        _context.Set<AccountingBook>().Update(book);
    }

    public async Task<AccountingBook?> GetByIdWithPeriodAsync(long bookId)
    {
        return await _context.Set<AccountingBook>()
            .Include(x => x.Period)
            .FirstOrDefaultAsync(x => x.BookId == bookId);
    }

    public async Task<bool> HasExportsAsync(long bookId)
    {
        return await _context.Set<AccountingExport>()
            .AnyAsync(x => x.BookId == bookId);
    }

    public void Remove(AccountingBook book)
    {
        _context.Set<AccountingBook>().Remove(book);
    }
}
