using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IFeatureRepository
    {
        Task<List<Feature>> GetAllAsync();
        Task<Feature?> GetByCodeAsync(string featureCode);
        Task AddAsync(Feature feature);

        /// <summary>Trả về các <c>FeatureId</c> có trong DB trong tập <paramref name="featureIds"/>.</summary>
        Task<HashSet<int>> GetExistingIdsAsync(IEnumerable<int> featureIds);
    }
}
