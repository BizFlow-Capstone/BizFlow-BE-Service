using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IFormulaDefinitionRepository
{
    Task<List<FormulaDefinition>> GetByCodesAsync(IEnumerable<string> codes);
    Task<List<FormulaDefinition>> GetByIdsAsync(IEnumerable<long> ids);
    Task<List<FormulaDefinition>> GetActiveAsync();
    Task<List<FormulaDefinition>> GetAllAsync();
    Task<FormulaDefinition?> GetByIdAsync(long formulaId);
    Task<FormulaDefinition> AddAsync(FormulaDefinition formula);
    void Delete(FormulaDefinition formula);
    void Update(FormulaDefinition formula);
}
