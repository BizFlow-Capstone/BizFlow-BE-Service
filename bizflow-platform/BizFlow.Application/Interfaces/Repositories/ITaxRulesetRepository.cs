using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface ITaxRulesetRepository
{
    Task<TaxRuleset?> GetActiveRulesetAsync();
    Task<TaxRuleset?> GetActiveRulesetWithRulesAsync();
    Task<List<IndustryTaxRate>> GetTaxRatesByBusinessTypeIdsAsync(int rulesetId, IEnumerable<Guid> businessTypeIds);
}
