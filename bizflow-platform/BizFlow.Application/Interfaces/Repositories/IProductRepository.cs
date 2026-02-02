using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IProductRepository
    {
        // ============ Query Methods ============

        /// <summary>
        /// Get products by location with pagination
        /// </summary>
        Task<(IEnumerable<Product> Items, int TotalCount)> GetByLocationIdAsync(
            int locationId, int pageNumber, int pageSize);

        /// <summary>
        /// Get product by ID
        /// </summary>
        Task<Product?> GetByIdAsync(long productId);

        /// <summary>
        /// Get product with sale items and price policies
        /// </summary>
        Task<Product?> GetByIdWithSaleItemsAsync(long productId);

        /// <summary>
        /// Check if product belongs to location
        /// </summary>
        Task<bool> BelongsToLocationAsync(long productId, int locationId);

        // ============ Command Methods ============

        /// <summary>
        /// Add a new product
        /// </summary>
        Task<Product> AddAsync(Product product);

        /// <summary>
        /// Update product
        /// </summary>
        void Update(Product product);

        /// <summary>
        /// Add sale item to product
        /// </summary>
        Task<SaleItem> AddSaleItemAsync(SaleItem saleItem);

        /// <summary>
        /// Add price policy to sale item
        /// </summary>
        Task AddPricePolicyAsync(ProductPricePolicy pricePolicy);
    }
}
