using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Product;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
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

        // TODO: Replace with actual JWT-based user identification
        private static readonly Guid _mockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

        public ProductController(
            IProductService productService,
            IMessageService messageService,
            ILogger<ProductController> logger)
            : base(messageService, logger)
        {
            _productService = productService;
        }

        #region Product APIs

        /// <summary>
        /// Get all products by location with pagination
        /// </summary>
        [HttpGet("products")]
        [SwaggerOperation(Summary = "Get product list with pagination")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int locationId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductsAsync(userId, locationId, pageNumber, pageSize);
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

        #endregion

        #region Private Helper Methods

        private Guid GetCurrentUserId()
        {
            return _mockCurrentUserId;
        }

        #endregion
    }
}
