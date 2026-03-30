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

    public async Task<List<FormulaDefinition>> GetAllAsync()
    {
        return await _context.Set<FormulaDefinition>()
            .OrderBy(x => x.FormulaId)
            .ToListAsync();
    }

    public async Task<FormulaDefinition?> GetByIdAsync(long formulaId)
    {
        return await _context.Set<FormulaDefinition>()
            .FirstOrDefaultAsync(x => x.FormulaId == formulaId);
    }

    public async Task<FormulaDefinition> AddAsync(FormulaDefinition formula)
    {
        await _context.Set<FormulaDefinition>().AddAsync(formula);
        return formula;
    }

    public void Update(FormulaDefinition formula)
    {
        _context.Set<FormulaDefinition>().Update(formula);
    }
}
