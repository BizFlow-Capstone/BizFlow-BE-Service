using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IFeatureRepository
    {
        Task<List<Feature>> GetAllAsync();
        Task<Feature?> GetByCodeAsync(string featureCode);
        Task AddAsync(Feature feature);

        /// <summary>Returns <c>FeatureId</c> values that exist in the database among <paramref name="featureIds"/>.</summary>
        Task<HashSet<int>> GetExistingIdsAsync(IEnumerable<int> featureIds);
    }
}
