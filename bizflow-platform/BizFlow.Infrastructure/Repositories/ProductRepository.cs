using BizFlow.Application.DTOs.Product;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using BizFlow.Application.Specifications.Products;
using BizFlow.Infrastructure.Specifications;

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

        /// <summary>
        /// Extensible search using Specification Pattern
        /// </summary>
        public async Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(ProductQueryParams query)
        {
            // 1. Get Total Count (using Count Spec)
            var countSpec = new ProductSearchSpec(query, isCount: true);
            var countQuery = SpecificationEvaluator<Product>.GetQuery(_dbContext.Products.AsQueryable(), countSpec);
            var totalCount = await countQuery.CountAsync();

            if (totalCount == 0)
            {
                return (new List<Product>(), 0);
            }

            // 2. Deferred Join Strategy:
            // Use 'filterOnly=true' spec (Sorts + Filters, NO Includes, NO Paging inside Spec)
            var filterSpec = new ProductSearchSpec(query, isCount: false, filterOnly: true);
            var filterQuery = SpecificationEvaluator<Product>.GetQuery(_dbContext.Products.AsQueryable(), filterSpec);
            
            // Manually apply Paging to get just IDs
            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;
            
            var ids = await filterQuery
                .Select(p => p.ProductId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!ids.Any())
            {
                return (new List<Product>(), totalCount);
            }

            // Fetch full entities by IDs
            var items = await _dbContext.Products
                .Include(p => p.SaleItems)
                    .ThenInclude(s => s.ProductPricePolicies)
                .Where(p => ids.Contains(p.ProductId))
                .OrderByDescending(p => p.ProductId) 
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Product?> GetByIdAsync(long productId)
        {
            return await _dbContext.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<Product?> GetByIdWithSaleItemsAsync(long productId)
        {
            return await _dbContext.Products
                .Where(p => p.ProductId == productId)
                .Include(p => p.SaleItems)
                    .ThenInclude(s => s.ProductPricePolicies)
                .FirstOrDefaultAsync();
        }

        public async Task<Product?> GetByIdWithDetailsAsync(long productId)
        {
            return await _dbContext.Products
                .Where(p => p.ProductId == productId)
                .Include(p => p.SaleItems)
                    .ThenInclude(s => s.ProductPricePolicies)
                .Include(p => p.BusinessLocation)
                .FirstOrDefaultAsync();
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

        public async Task<List<string>> GetAllImagePublicIdsAsync()
        {
            return await _dbContext.Products
                .Where(p => !string.IsNullOrEmpty(p.ImagePublicId))
                .Select(p => p.ImagePublicId!)
                .ToListAsync();
        }




        public async Task AddPricePolicyAsync(ProductPricePolicy pricePolicy)
        {
            await _dbContext.ProductPricePolicies.AddAsync(pricePolicy);
        }
    }
}
