using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IProfileRepository
    {
        Task<Profile?> GetByIdAsync(Guid profileId);
        Task<Profile?> GetByAccountIdAsync(Guid accountId);
        Task<List<Profile>> GetByIdsAsync(IEnumerable<Guid> profileIds);
        Task<string?> GetFullNameByEmailAsync(string email, CancellationToken ct = default);
    }
}
