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
    public class ProductController : PaginatedApiController
    {
        private readonly IProductService _productService;

        // TODO: Replace with actual JWT-based user identification
        private static readonly Guid _mockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
        //private static readonly Guid _mockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440003");

        public ProductController(
            IProductService productService,
            IMessageService messageService,
            IOptions<PaginationSettings> paginationSettings,
            ILogger<ProductController> logger)
            : base(messageService, logger, paginationSettings)
        {
            _productService = productService;
        }

        #region Product APIs

        /// <summary>
        /// Search and filter products with pagination
        /// </summary>
        [HttpGet("products")]
        [SwaggerOperation(Summary = "Search/filter products", Description = "Supports filtering by name, SKU, status, business type. Returns paginated results. Owner or Employee access.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParams query)
        {
            ApplyPaginationDefaults(query);

            var userId = GetCurrentUserId();
            var result = await _productService.SearchProductsAsync(userId, query);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Lightweight product search for order flow
        /// </summary>
        [HttpGet("locations/{locationId:int}/products/quick-search")]
        [SwaggerOperation(Summary = "Quick search products", Description = "Search by name/sku in a business location and return: name, sku, imageUrl, sellingPrice, saleItems.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> QuickSearchProducts(int locationId, [FromQuery] string? search)
        {
            var userId = GetCurrentUserId();
            var result = await _productService.SearchQuickProductsAsync(userId, locationId, search);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Get product detail by ID
        /// </summary>
        [HttpGet("product/{productId:long}")]
        [SwaggerOperation(Summary = "Get product detail", Description = "Returns detailed product information including images, prices, and sale items. Owner or Employee access.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductDetail(long productId)
        {
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductDetailAsync(userId, productId);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Get product sale items (price tiers)
        /// </summary>
        [HttpGet("product/{productId:long}/sale-items")]
        [SwaggerOperation(Summary = "Get product sale items", Description = "Returns all price tiers (unit conversions) for a product. Owner or Employee access.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductSaleItems(long productId)
        {
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductSaleItemsAsync(userId, productId);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Get product cost price history from confirmed imports
        /// </summary>
        [HttpGet("product/{productId:long}/cost-price-history")]
        [SwaggerOperation(Summary = "Get cost price history", Description = "Returns all cost price changes from confirmed imports. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCostPriceHistory(long productId)
        {
            var userId = GetCurrentUserId();
            var result = await _productService.GetCostPriceHistoryAsync(userId, productId);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        [HttpPost("product")]
        [SwaggerOperation(Summary = "Create product", Description = "Upload image via multipart/form-data. PriceTiers as JSON string: [{'Unit':'Thùng','Quantity':12,'Price':120000}]. Owner only.")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductRequest request, IFormFile? image)
        {
            if (image != null)
            {
                request.ImageStream = image.OpenReadStream();
                request.ImageFileName = image.FileName;
            }

            var userId = GetCurrentUserId();
            var (product, warnings) = await _productService.CreateProductAsync(userId, request);
            return Created(product, MessageKeys.DataCreatedSuccessfully, nameof(GetProducts), new { locationId = request.LocationId }, warnings);
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        [HttpPut("product/{id:long}")]
        [SwaggerOperation(Summary = "Update product", Description = "Cannot change location. Missing sale items will be soft-deleted. Set RemoveImage=true to remove image. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProduct(long id, [FromForm] UpdateProductRequest request, IFormFile? image)
        {
            if (image != null)
            {
                request.ImageStream = image.OpenReadStream();
                request.ImageFileName = image.FileName;
            }

            var userId = GetCurrentUserId();
            var (product, warnings) = await _productService.UpdateProductAsync(userId, id, request);
            return Ok(product, MessageKeys.DataUpdatedSuccessfully, warnings);
        }

        /// <summary>
        /// Update product status
        /// </summary>
        [HttpPatch("product/{productId:long}/status")]
        [SwaggerOperation(Summary = "Update product status", Description = "Toggle active/inactive. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProductStatus(long productId, [FromBody] UpdateProductStatusRequest request)
        {
            var userId = GetCurrentUserId();
            await _productService.UpdateProductStatusAsync(userId, productId, request.Status);
            return Ok(MessageKeys.DataUpdatedSuccessfully);
        }

        /// <summary>
        /// Manually adjust product stock to target quantity
        /// </summary>
        [HttpPatch("product/{productId:long}/stock")]
        [SwaggerOperation(Summary = "Adjust product stock", Description = "Manual stock adjustment with optional memo. Increase creates import + stock movement. Decrease creates stock movement only. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AdjustProductStock(long productId, [FromBody] AdjustProductStockRequest request)
        {
            var userId = GetCurrentUserId();
            var product = await _productService.AdjustProductStockAsync(userId, productId, request);
            return Ok(product, MessageKeys.DataUpdatedSuccessfully);
        }

        /// <summary>
        /// Bulk adjust selling price on selected sale items by fixed delta.
        /// </summary>
        [HttpPatch("products/sale-items/selling-price")]
        [SwaggerOperation(Summary = "Bulk adjust selling price", Description = "Adjust selected sale-item selling prices by fixed delta. Positive delta increases price, negative delta decreases price. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BulkAdjustSellingPrice([FromBody] BulkAdjustSellingPriceRequest request)
        {
            var userId = GetCurrentUserId();
            await _productService.BulkAdjustSellingPriceAsync(userId, request);
            return Ok(MessageKeys.DataUpdatedSuccessfully);
        }

        /// <summary>
        /// Delete product (soft delete)
        /// </summary>
        [HttpDelete("product/{productId:long}")]
        [SwaggerOperation(Summary = "Delete product", Description = "Soft/Hard delete based on history. Cascades to SaleItems. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct(long productId)
        {
            var userId = GetCurrentUserId();
            await _productService.DeleteProductAsync(userId, productId);
            return Ok(MessageKeys.DataDeletedSuccessfully);
        }

        #endregion

        #region Private Helper Methods

        private Guid GetCurrentUserId()
        {
            return _mockCurrentUserId;
        }

        /// <summary>
        #endregion
    }
}

