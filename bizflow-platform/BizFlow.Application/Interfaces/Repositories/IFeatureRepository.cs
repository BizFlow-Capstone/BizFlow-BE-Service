using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IFeatureRepository
    {
        Task<List<Feature>> GetAllAsync();
    }
}
