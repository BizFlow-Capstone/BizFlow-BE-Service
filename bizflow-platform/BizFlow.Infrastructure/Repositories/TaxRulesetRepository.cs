using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories;

public class TaxRulesetRepository : ITaxRulesetRepository
{
    private readonly BizFlowDbContext _context;

    public TaxRulesetRepository(BizFlowDbContext context)
    {
        _context = context;
    }

    public async Task<TaxRuleset?> GetActiveRulesetAsync()
    {
        return await _context.Set<TaxRuleset>()
            .FirstOrDefaultAsync(x => x.IsActive);
    }

    public async Task<TaxRuleset?> GetActiveRulesetWithRulesAsync()
    {
        return await _context.Set<TaxRuleset>()
            .Include(x => x.GroupRules.OrderBy(r => r.SortOrder))
            .Include(x => x.IndustryTaxRates)
            .FirstOrDefaultAsync(x => x.IsActive);
    }

    public async Task<List<IndustryTaxRate>> GetTaxRatesByBusinessTypeIdsAsync(int rulesetId, IEnumerable<Guid> businessTypeIds)
    {
        var ids = businessTypeIds.ToList();
        return await _context.Set<IndustryTaxRate>()
            .Where(x => x.RulesetId == rulesetId && ids.Contains(x.BusinessTypeId))
            .ToListAsync();
    }
}
