using BizFlow.Application.DTOs.Product;
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

        /// <summary>
        /// Extensible search with filters - add new filters in ApplyFilters method
        /// </summary>
        public async Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(ProductQueryParams query)
        {
            var baseQuery = _dbContext.Products
                .Where(p => p.BusinessLocationId == query.LocationId && !p.IsDeleted)
                .AsQueryable();

            // Apply filters (extensible - add more in ApplyFilters)
            baseQuery = ApplyFilters(baseQuery, query);

            // Count before pagination
            var totalCount = await baseQuery.CountAsync();

            // Use provided values or fallback to defaults (should be set by controller)
            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;

            // Apply pagination and include related data
            var items = await baseQuery
                .OrderByDescending(p => p.ProductId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.SaleItems)
                    .ThenInclude(s => s.ProductPricePolicies)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Extensible filter method - add new filter conditions here
        /// </summary>
        private static IQueryable<Product> ApplyFilters(IQueryable<Product> query, ProductQueryParams filters)
        {
            // ============ SEARCH ============
            if (!string.IsNullOrWhiteSpace(filters.Name))
            {
                query = query.Where(p => p.ProductName.Contains(filters.Name));
            }

            if (!string.IsNullOrWhiteSpace(filters.Sku))
            {
                query = query.Where(p => p.Sku != null && p.Sku.Contains(filters.Sku));
            }

            // ============ FILTER ============
            if (filters.MinCostPrice.HasValue)
            {
                query = query.Where(p => p.CostPrice >= filters.MinCostPrice.Value);
            }

            if (filters.MaxCostPrice.HasValue)
            {
                query = query.Where(p => p.CostPrice <= filters.MaxCostPrice.Value);
            }

            if (filters.MinStock.HasValue)
            {
                query = query.Where(p => p.Stock >= filters.MinStock.Value);
            }

            if (filters.MaxStock.HasValue)
            {
                query = query.Where(p => p.Stock <= filters.MaxStock.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.Status))
            {
                query = query.Where(p => p.Status == filters.Status.ToLower());
            }

            if (filters.TrackInventory.HasValue)
            {
                query = query.Where(p => p.TrackInventory == filters.TrackInventory.Value);
            }

            // ============ ADD MORE FILTERS HERE ============
            // Example:
            // if (!string.IsNullOrWhiteSpace(filters.Manufacturer))
            // {
            //     query = query.Where(p => p.Manufacturer != null && p.Manufacturer.Contains(filters.Manufacturer));
            // }

            return query;
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

