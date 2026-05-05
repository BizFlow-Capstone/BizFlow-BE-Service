using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class BusinessTypeRepository : IBusinessTypeRepository
    {
        private readonly BizFlowDbContext _context;

        public BusinessTypeRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BusinessType>> GetAllAsync()
        {
            return await _context.BusinessTypes
                .OrderBy(bt => bt.Name)
                .ToListAsync();
        }

        public async Task<BusinessType?> GetByIdAsync(Guid id)
        {
            return await _context.BusinessTypes
                .FirstOrDefaultAsync(bt => bt.BusinessTypeId == id);
        }

        public async Task<BusinessType?> GetActiveByIdAsync(Guid id)
        {
            return await _context.BusinessTypes
                .FirstOrDefaultAsync(bt =>
                    bt.BusinessTypeId == id &&
                    bt.Status == "active");
        }

        public async Task<BusinessType?> GetByCodeAsync(string code)
        {
            return await _context.BusinessTypes
                .FirstOrDefaultAsync(bt => bt.Code == code);
        }

        public async Task<BusinessType> AddAsync(BusinessType businessType)
        {
            await _context.BusinessTypes.AddAsync(businessType);
            return businessType;
        }

        public void Delete(BusinessType businessType)
        {
            _context.BusinessTypes.Remove(businessType);
        }

        public void Update(BusinessType businessType)
        {
            _context.BusinessTypes.Update(businessType);
        }
    }
}
