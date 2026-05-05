using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IBusinessTypeRepository
    {
        Task<IEnumerable<BusinessType>> GetAllAsync();
        Task<BusinessType?> GetByIdAsync(Guid id);
        Task<BusinessType?> GetByCodeAsync(string code);
        Task<BusinessType> AddAsync(BusinessType businessType);
        void Delete(BusinessType businessType);
        void Update(BusinessType businessType);
    }
}
