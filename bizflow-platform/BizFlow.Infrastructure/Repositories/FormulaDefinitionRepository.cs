using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories;

public class FormulaDefinitionRepository : IFormulaDefinitionRepository
{
    private readonly BizFlowDbContext _context;

    public FormulaDefinitionRepository(BizFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<FormulaDefinition>> GetByCodesAsync(IEnumerable<string> codes)
    {
        var codeList = codes.ToList();
        return await _context.Set<FormulaDefinition>()
            .Where(x => codeList.Contains(x.Code) && x.IsActive)
            .ToListAsync();
    }

    public async Task<List<FormulaDefinition>> GetActiveAsync()
    {
        return await _context.Set<FormulaDefinition>()
            .Where(x => x.IsActive)
            .ToListAsync();
    }
}
