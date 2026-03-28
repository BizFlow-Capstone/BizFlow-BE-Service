using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IFormulaResultRepository
{
    Task<List<FormulaResult>> GetByBookIdAsync(long bookId);
    Task UpsertAsync(FormulaResult result);
    Task MarkStaleByBookIdAsync(long bookId);
}
