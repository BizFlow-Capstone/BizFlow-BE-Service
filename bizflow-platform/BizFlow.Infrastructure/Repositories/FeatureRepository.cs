using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class FeatureRepository : IFeatureRepository
    {
        private readonly BizFlowDbContext _context;

        public FeatureRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public Task<List<Feature>> GetAllAsync()
        {
            return _context.Features
                .OrderBy(f => f.FeatureId)
                .ToListAsync();
        }
    }
}
