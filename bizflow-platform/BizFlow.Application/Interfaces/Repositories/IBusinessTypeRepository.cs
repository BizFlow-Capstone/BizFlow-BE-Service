using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IBusinessTypeRepository
    {
        Task<IEnumerable<BusinessType>> GetAllAsync();
    }
}
