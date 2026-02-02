using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Product;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Product
{
    /// <summary>
    /// Product Management APIs
    /// </summary>
    [Route("api/my-business")]
    public class ProductController : BaseApiController
    {
        private readonly IProductService _productService;
        private readonly PaginationSettings _paginationSettings;

        // TODO: Replace with actual JWT-based user identification
        private static readonly Guid _mockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

        public ProductController(
            IProductService productService,
            IMessageService messageService,
            IOptions<PaginationSettings> paginationSettings,
            ILogger<ProductController> logger)
            : base(messageService, logger)
        {
            _productService = productService;
            _paginationSettings = paginationSettings.Value;
        }

        #region Product APIs

        /// <summary>
        /// Search and filter products with pagination
        /// </summary>
        [HttpGet("products")]
        [SwaggerOperation(Summary = "Search/filter products with pagination")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParams query)
        {
            // Apply pagination defaults from settings
            ApplyPaginationDefaults(query);

            var userId = GetCurrentUserId();
            var result = await _productService.SearchProductsAsync(userId, query);
            return Ok(result, MessageKeys.ProductsRetrievedSuccessfully);
        }

        /// <summary>
        /// Get product sale items (price tiers)
        /// </summary>
        [HttpGet("product/{productId:long}/sale-items")]
        [SwaggerOperation(Summary = "Get product price tiers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductSaleItems(long productId)
        {
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductSaleItemsAsync(userId, productId);
            return Ok(result, MessageKeys.ProductsRetrievedSuccessfully);
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        [HttpPost("product")]
        [SwaggerOperation(Summary = "Create a new product with price tiers")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            var userId = GetCurrentUserId();
            var product = await _productService.CreateProductAsync(userId, request);
            return Created(product, MessageKeys.ProductCreatedSuccessfully, nameof(GetProducts), new { locationId = request.LocationId });
        }

        /// <summary>
        /// Update product status
        /// </summary>
        [HttpPut("product/{productId:long}/status")]
        [SwaggerOperation(Summary = "Update product status (active/inactive/discontinued)")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProductStatus(long productId, [FromBody] UpdateProductStatusRequest request)
        {
            var userId = GetCurrentUserId();
            var success = await _productService.UpdateProductStatusAsync(userId, productId, request.Status);

            if (!success)
            {
                return Forbidden(MessageKeys.ProductAccessDenied);
            }

            return Ok(MessageKeys.ProductStatusUpdated);
        }

        /// <summary>
        /// Delete product (soft delete)
        /// </summary>
        [HttpDelete("product/{productId:long}")]
        [SwaggerOperation(Summary = "Delete product (soft delete)")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct(long productId)
        {
            var userId = GetCurrentUserId();
            var success = await _productService.DeleteProductAsync(userId, productId);

            if (!success)
            {
                return Forbidden(MessageKeys.ProductAccessDenied);
            }

            return Ok(MessageKeys.ProductDeletedSuccessfully);
        }

        #endregion

        #region Private Helper Methods

        private Guid GetCurrentUserId()
        {
            return _mockCurrentUserId;
        }

        /// <summary>
        /// Apply default pagination values from settings if not provided
        /// </summary>
        private void ApplyPaginationDefaults(PaginationParams pagination)
        {
            pagination.PageNumber ??= _paginationSettings.DefaultPageNumber;
            pagination.PageSize ??= _paginationSettings.DefaultPageSize;

            // Clamp page size to max allowed
            if (pagination.PageSize > _paginationSettings.MaxPageSize)
            {
                pagination.PageSize = _paginationSettings.MaxPageSize;
            }
        }

        #endregion
    }
}

