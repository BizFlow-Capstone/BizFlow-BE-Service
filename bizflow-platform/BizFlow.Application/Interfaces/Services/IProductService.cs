using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Product;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IProductService
    {
        /// <summary>
        /// Search and filter products with pagination
        /// </summary>
        Task<PaginatedResponse<ProductSummaryDto>> SearchProductsAsync(Guid userId, ProductQueryParams query);

        /// <summary>
        /// Get detailed product information by ID
        /// </summary>
        Task<ProductDetailDto?> GetProductDetailAsync(Guid userId, long productId);

        /// <summary>
        /// Get product sale items (price tiers)
        /// </summary>
        Task<ProductSaleItemsResponseDto?> GetProductSaleItemsAsync(Guid userId, long productId);

        /// <summary>
        /// Create a new product
        /// </summary>
        Task<(ProductSummaryDto Product, List<string>? Warnings)> CreateProductAsync(Guid userId, CreateProductRequest request);

        /// <summary>
        /// Update an existing product
        /// </summary>
        Task<(ProductSummaryDto Product, List<string>? Warnings)> UpdateProductAsync(Guid userId, long productId, UpdateProductRequest request);

        /// <summary>
        /// Update product status
        /// </summary>
        Task UpdateProductStatusAsync(Guid userId, long productId, string status);

        /// <summary>
        /// Manually adjust product stock to a target quantity.
        /// Increase creates an import + stock movement, decrease creates stock movement only.
        /// </summary>
        Task<ProductSummaryDto> AdjustProductStockAsync(Guid userId, long productId, AdjustProductStockRequest request);

        /// <summary>
        /// Bulk adjust selling price by fixed delta on selected sale items.
        /// Positive delta increases price; negative delta decreases price.
        /// </summary>
        Task BulkAdjustSellingPriceAsync(Guid userId, BulkAdjustSellingPriceRequest request);

        /// <summary>
        /// Delete product (soft/hard delete based on history)
        /// </summary>
        Task DeleteProductAsync(Guid userId, long productId);

        /// <summary>
        /// Get cost price history for a product (owner only)
        /// </summary>
        Task<CostPriceHistoryDto> GetCostPriceHistoryAsync(Guid userId, long productId);
    }
}
