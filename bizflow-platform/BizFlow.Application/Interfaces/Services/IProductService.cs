using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Product;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IProductService
    {
        /// <summary>
        /// Get products by location with pagination
        /// </summary>
        Task<PaginatedResponse<ProductListItemDto>> GetProductsAsync(
            Guid userId, int locationId, int pageNumber, int pageSize);

        /// <summary>
        /// Get product sale items (price tiers)
        /// </summary>
        Task<ProductSaleItemsResponseDto?> GetProductSaleItemsAsync(Guid userId, long productId);

        /// <summary>
        /// Create a new product
        /// </summary>
        Task<ProductListItemDto> CreateProductAsync(Guid userId, CreateProductRequest request);

        /// <summary>
        /// Update product status
        /// </summary>
        Task<bool> UpdateProductStatusAsync(Guid userId, long productId, string status);
    }
}
