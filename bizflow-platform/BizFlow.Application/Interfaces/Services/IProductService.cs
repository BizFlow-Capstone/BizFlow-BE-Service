using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Product;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IProductService
    {
        /// <summary>
        /// Search and filter products with pagination
        /// </summary>
        Task<PaginatedResponse<ProductListItemDto>> SearchProductsAsync(Guid userId, ProductQueryParams query);

        /// <summary>
        /// Get product sale items (price tiers)
        /// </summary>
        Task<ProductSaleItemsResponseDto?> GetProductSaleItemsAsync(Guid userId, long productId);

        /// <summary>
        /// Create a new product
        /// </summary>
        Task<ProductListItemDto> CreateProductAsync(Guid userId, CreateProductRequest request);

        /// <summary>
        /// Update an existing product
        /// </summary>
        Task<ProductListItemDto> UpdateProductAsync(Guid userId, long productId, UpdateProductRequest request);

        /// <summary>
        /// Update product status
        /// </summary>
        Task<bool> UpdateProductStatusAsync(Guid userId, long productId, string status);

        /// <summary>
        /// Delete product (soft delete, only if not in business)
        /// </summary>
        Task<bool> DeleteProductAsync(Guid userId, long productId);
    }
}
