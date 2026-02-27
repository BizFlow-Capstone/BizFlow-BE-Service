using BizFlow.Application.DTOs.Product;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IProductRepository
    {
        // ============ Query Methods ============

        /// <summary>
        /// Get products with search, filter, and pagination
        /// </summary>
        Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(ProductQueryParams query);

        /// <summary>
        /// Get product by ID
        /// </summary>
        Task<Product?> GetByIdAsync(long productId);

        /// <summary>
        /// Get product with sale items and price policies
        /// </summary>
        Task<Product?> GetByIdWithSaleItemsAsync(long productId);

        /// <summary>
        /// Get product by ID with all related data (SaleItems, PricePolicies, BusinessLocation)
        /// </summary>
        Task<Product?> GetByIdWithDetailsAsync(long productId);
        Task<bool> HasHistoryAsync(long productId);

        /// <summary>
        /// Get all non-null ImagePublicIds from Products table (for cleanup job)
        /// </summary>
        /// <summary>
        /// Check which of the given PublicIds exist in the database (for orphan detection)
        /// </summary>
        Task<HashSet<string>> GetExistingPublicIdsAsync(IEnumerable<string> publicIds);


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
        void Delete(Product product);

        /// <summary>
        /// Add price policy to sale item
        /// </summary>
        Task AddPricePolicyAsync(ProductPricePolicy pricePolicy);
    }
}
