using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories;

public class FormulaResultRepository : IFormulaResultRepository
{
    private readonly BizFlowDbContext _context;

    public FormulaResultRepository(BizFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<FormulaResult>> GetByBookIdAsync(long bookId)
    {
        return await _context.Set<FormulaResult>()
            .Where(x => x.BookId == bookId)
            .ToListAsync();
    }

    public async Task UpsertAsync(FormulaResult result)
    {
        var existing = await _context.Set<FormulaResult>()
            .FirstOrDefaultAsync(x =>
                x.BookId == result.BookId &&
                x.FormulaId == result.FormulaId &&
                x.ProductId == result.ProductId &&
                x.BusinessTypeId == result.BusinessTypeId &&
                x.SectionCode == result.SectionCode);

        if (existing != null)
        {
            existing.ResultValue = result.ResultValue;
            existing.ComputedAt = DateTime.UtcNow;
            existing.IsStale = false;
        }
        else
        {
            result.ComputedAt = DateTime.UtcNow;
            result.IsStale = false;
            await _context.Set<FormulaResult>().AddAsync(result);
        }
    }

    public async Task MarkStaleByBookIdAsync(long bookId)
    {
        await _context.Set<FormulaResult>()
            .Where(x => x.BookId == bookId)
            .ExecuteUpdateAsync(x => x.SetProperty(r => r.IsStale, true));
    }
}
