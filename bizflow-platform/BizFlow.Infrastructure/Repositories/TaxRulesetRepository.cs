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

    public async Task<List<TaxRuleset>> GetAllWithRulesAsync()
    {
        return await _context.Set<TaxRuleset>()
            .Include(x => x.GroupRules.OrderBy(r => r.SortOrder))
            .Include(x => x.IndustryTaxRates)
            .Include(x => x.AccountingBooks)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaxRuleset?> GetByIdWithRulesAsync(int rulesetId)
    {
        return await _context.Set<TaxRuleset>()
            .Include(x => x.GroupRules.OrderBy(r => r.SortOrder))
            .Include(x => x.IndustryTaxRates)
            .Include(x => x.AccountingBooks)
            .FirstOrDefaultAsync(x => x.RulesetId == rulesetId);
    }

    public async Task<List<IndustryTaxRate>> GetTaxRatesByBusinessTypeIdsAsync(int rulesetId, IEnumerable<Guid> businessTypeIds)
    {
        var ids = businessTypeIds.ToList();
        return await _context.Set<IndustryTaxRate>()
            .Where(x => x.RulesetId == rulesetId && ids.Contains(x.BusinessTypeId))
            .ToListAsync();
    }

    public async Task<TaxRuleset> AddAsync(TaxRuleset ruleset)
    {
        await _context.Set<TaxRuleset>().AddAsync(ruleset);
        return ruleset;
    }

    public void Update(TaxRuleset ruleset)
    {
        _context.Set<TaxRuleset>().Update(ruleset);
    }

    public void Remove(TaxRuleset ruleset)
    {
        _context.Set<TaxRuleset>().Remove(ruleset);
    }

    public async Task<List<IndustryTaxRate>> GetRatesByBusinessTypeAsync(int rulesetId, Guid businessTypeId)
    {
        return await _context.Set<IndustryTaxRate>()
            .Where(r => r.RulesetId == rulesetId && r.BusinessTypeId == businessTypeId)
            .OrderBy(r => r.TaxType)
            .ToListAsync();
    }

    public void AddRate(IndustryTaxRate rate)
    {
        _context.Set<IndustryTaxRate>().Add(rate);
    }

    public void RemoveRates(IEnumerable<IndustryTaxRate> rates)
    {
        _context.Set<IndustryTaxRate>().RemoveRange(rates);
    }
}
