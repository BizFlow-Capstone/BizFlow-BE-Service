using BizFlow.Application.DTOs.Product;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using BizFlow.Application.Specifications.Products;
using BizFlow.Infrastructure.Specifications;
using BizFlow.Domain.Enums;

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
                .Include(p => p.BusinessType)
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
                .Include(p => p.BusinessType)
                .FirstOrDefaultAsync();
        }

        public async Task<Product?> GetByIdWithDetailsAsync(long productId)
        {
            return await _dbContext.Products
                .Where(p => p.ProductId == productId)
                .Include(p => p.SaleItems)
                    .ThenInclude(s => s.ProductPricePolicies)
                .Include(p => p.BusinessLocation)
                .Include(p => p.BusinessType)
                .FirstOrDefaultAsync();
        }

        public async Task<Product?> FindBySkuInLocationAsync(int locationId, string sku, long? excludeProductId)
        {
            var query = _dbContext.Products
                .Where(p => p.BusinessLocationId == locationId && p.Sku == sku);

            if (excludeProductId.HasValue)
                query = query.Where(p => p.ProductId != excludeProductId.Value);

            return await query.FirstOrDefaultAsync();
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

        public void Delete(Product product)
        {
            _dbContext.Products.Remove(product);
        }

        public async Task<SaleItem> AddSaleItemAsync(SaleItem saleItem)
        {
            var entry = await _dbContext.SaleItems.AddAsync(saleItem);
            return entry.Entity;
        }

        public async Task<HashSet<string>> GetExistingPublicIdsAsync(IEnumerable<string> publicIds)
        {
            var ids = publicIds.ToList();
            if (ids.Count == 0) return new HashSet<string>();

            var existing = await _dbContext.Products
                .Where(p => p.ImagePublicId != null && ids.Contains(p.ImagePublicId))
                .Select(p => p.ImagePublicId!)
                .ToListAsync();

            return new HashSet<string>(existing);
        }




        public async Task AddPricePolicyAsync(ProductPricePolicy pricePolicy)
        {
            await _dbContext.ProductPricePolicies.AddAsync(pricePolicy);
        }
        public async Task<bool> HasHistoryAsync(long productId)
        {
            // Check imports history
            var hasImports = await _dbContext.Products
                .Where(p => p.ProductId == productId)
                .AnyAsync(p => p.ProductsImports.Any());

            if (hasImports) return true;

            // Check orders history (via SaleItems)
            // TODO: Implement actual check when Order entity is available
            // var hasOrders = await _dbContext.SaleItems
            //     .Where(s => s.ProductId == productId)
            //     .AnyAsync(s => s.OrderDetails.Any());
            
            return false; 
        }

        public async Task<List<CostPriceHistoryItemDto>> GetCostPriceHistoryAsync(long productId)
        {
            return await _dbContext.ProductsImports
                .Where(pi => pi.ProductId == productId
                    && pi.Import.Status == ImportStatus.Confirmed)
                .OrderByDescending(pi => pi.Import.ReceivedAt ?? pi.Import.CreatedAt)
                .Select(pi => new CostPriceHistoryItemDto
                {
                    ImportId = pi.ImportId,
                    ImportCode = pi.Import.ImportCode,
                    CostPrice = pi.CostPrice,
                    Quantity = pi.Quantity,
                    TotalPrice = pi.TotalPrice,
                    Supplier = pi.Import.Supplier,
                    ReceivedAt = pi.Import.ReceivedAt,
                    CreatedAt = pi.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<decimal?> GetLatestCostPriceFromImportsAsync(long productId, long excludeImportId)
        {
            return await _dbContext.ProductsImports
                .Where(pi => pi.ProductId == productId
                    && pi.ImportId != excludeImportId
                    && pi.Import.Status == ImportStatus.Confirmed)
                .OrderByDescending(pi => pi.Import.ReceivedAt ?? pi.Import.CreatedAt)
                .Select(pi => (decimal?)pi.CostPrice)
                .FirstOrDefaultAsync();
        }

        public async Task<List<SaleItem>> GetSaleItemsForPriceAdjustAsync(IEnumerable<long> saleItemIds)
        {
            var ids = saleItemIds.Distinct().ToList();
            if (ids.Count == 0)
                return new List<SaleItem>();

            return await _dbContext.SaleItems
                .Where(si => ids.Contains(si.SaleItemId))
                .Include(si => si.Product)
                .Include(si => si.ProductPricePolicies)
                .ToListAsync();
        }
    }
}
