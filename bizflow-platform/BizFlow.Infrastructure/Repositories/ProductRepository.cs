using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly BizFlowDbContext _dbContext;

        public ProductRepository(BizFlowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ============ Query Methods ============

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetByLocationIdAsync(
            int locationId, int pageNumber, int pageSize)
        {
            var query = _dbContext.Products
                .Where(p => p.BusinessLocationId == locationId && !p.IsDeleted)
                .OrderByDescending(p => p.ProductId);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.SaleItems)
                    .ThenInclude(s => s.ProductPricePolicies)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Product?> GetByIdAsync(long productId)
        {
            return await _dbContext.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId && !p.IsDeleted);
        }

        public async Task<Product?> GetByIdWithSaleItemsAsync(long productId)
        {
            return await _dbContext.Products
                .Include(p => p.SaleItems)
                    .ThenInclude(s => s.ProductPricePolicies)
                .FirstOrDefaultAsync(p => p.ProductId == productId && !p.IsDeleted);
        }

        public async Task<bool> BelongsToLocationAsync(long productId, int locationId)
        {
            return await _dbContext.Products
                .AnyAsync(p => p.ProductId == productId 
                    && p.BusinessLocationId == locationId 
                    && !p.IsDeleted);
        }

        // ============ Command Methods ============

        public async Task<Product> AddAsync(Product product)
        {
            var entry = await _dbContext.Products.AddAsync(product);
            return entry.Entity;
        }

        public void Update(Product product)
        {
            _dbContext.Products.Update(product);
        }

        public async Task<SaleItem> AddSaleItemAsync(SaleItem saleItem)
        {
            var entry = await _dbContext.SaleItems.AddAsync(saleItem);
            return entry.Entity;
        }

        public async Task AddPricePolicyAsync(ProductPricePolicy pricePolicy)
        {
            await _dbContext.ProductPricePolicies.AddAsync(pricePolicy);
        }
    }
}
