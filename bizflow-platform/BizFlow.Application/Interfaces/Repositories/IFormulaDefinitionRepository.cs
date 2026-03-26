using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IFormulaDefinitionRepository
{
    Task<List<FormulaDefinition>> GetByCodesAsync(IEnumerable<string> codes);
    Task<List<FormulaDefinition>> GetActiveAsync();
}
