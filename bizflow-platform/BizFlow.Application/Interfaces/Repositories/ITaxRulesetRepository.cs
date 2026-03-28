using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface ITaxRulesetRepository
{
    Task<TaxRuleset?> GetActiveRulesetAsync();
    Task<TaxRuleset?> GetActiveRulesetWithRulesAsync();
    Task<List<TaxRuleset>> GetAllWithRulesAsync();
    Task<TaxRuleset?> GetByIdWithRulesAsync(int rulesetId);
    Task<List<IndustryTaxRate>> GetTaxRatesByBusinessTypeIdsAsync(int rulesetId, IEnumerable<Guid> businessTypeIds);
    Task<TaxRuleset> AddAsync(TaxRuleset ruleset);
    void Update(TaxRuleset ruleset);
    void Remove(TaxRuleset ruleset);
}
