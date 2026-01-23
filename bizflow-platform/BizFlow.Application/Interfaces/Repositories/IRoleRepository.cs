using BizFlow.Domain;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<IEnumerable<Role>> GetAllAsync();
        Task<Role?> GetByNameAsync(string name);
    }
}
