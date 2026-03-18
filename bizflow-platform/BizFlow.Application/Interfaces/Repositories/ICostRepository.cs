using BizFlow.Application.DTOs.Cost;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface ICostRepository
    {
        Task<(IEnumerable<Cost> Items, int TotalCount)> SearchAsync(CostQueryParams query);
        Task<Cost?> GetByIdAsync(long costId);
        Task<Cost?> GetByImportIdAsync(long importId);
        Task<Cost> AddAsync(Cost cost);
        void Update(Cost cost);
    }
}
