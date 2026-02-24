using BizFlow.Api.Controllers.Product;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Product;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Controllers
{
    public class ProductControllerTests
    {
        // ─── Mock dependencies ──────────────────────────────────────────
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<ProductController>> _mockLogger;
        private readonly IOptions<PaginationSettings> _paginationOptions;
        private readonly ProductController _controller;

        // Mock user ID that matches the controller's static field
        private static readonly Guid _mockUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

        public ProductControllerTests()
        {
            _mockProductService = new Mock<IProductService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<ProductController>>();

            // MessageService always returns a non-null string to avoid NullRef in BaseApiController
            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>()))
                .Returns("ok");
            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns("ok");

            _paginationOptions = Options.Create(new PaginationSettings
            {
                DefaultPageNumber = 1,
                DefaultPageSize = 10,
                MaxPageSize = 100
            });

            _controller = new ProductController(
                _mockProductService.Object,
                _mockMessageService.Object,
                _paginationOptions,
                _mockLogger.Object);
        }

        // ================================================================
        // GET /products
        // ================================================================

        [Fact]
        public async Task GetProducts_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var query = new ProductQueryParams { LocationId = 1, PageNumber = 1, PageSize = 10 };
            var paged = new PaginatedResponse<ProductListItemDto>(
                items: new List<ProductListItemDto> { new() { ProductId = 1, Name = "P1" } },
                count: 1,
                pageNumber: 1,
                pageSize: 10);
            _mockProductService.Setup(s => s.SearchProductsAsync(_mockUserId, query))
                .ReturnsAsync(paged);

            // Act
            var result = await _controller.GetProducts(query);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockProductService.Verify(s => s.SearchProductsAsync(_mockUserId, query), Times.Once);
        }

        [Fact]
        public async Task GetProducts_AppliesPaginationDefaults_WhenQueryParamsAreNull()
        {
            // Arrange — no PageNumber / PageSize set
            var query = new ProductQueryParams { LocationId = 1 };
            _mockProductService.Setup(s => s.SearchProductsAsync(_mockUserId, It.IsAny<ProductQueryParams>()))
                .ReturnsAsync(new PaginatedResponse<ProductListItemDto>(Enumerable.Empty<ProductListItemDto>(), 0, 1, 10));

            // Act
            await _controller.GetProducts(query);

            // Assert — defaults should be applied before service call
            Assert.Equal(1, query.PageNumber);
            Assert.Equal(10, query.PageSize);
        }

        // ================================================================
        // GET /product/{productId}
        // ================================================================

        [Fact]
        public async Task GetProductDetail_ReturnsOk_WhenProductExists()
        {
            // Arrange
            var productId = 1L;
            var dto = new ProductDetailDto { ProductId = productId, Name = "Test Product" };
            _mockProductService.Setup(s => s.GetProductDetailAsync(_mockUserId, productId))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.GetProductDetail(productId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockProductService.Verify(s => s.GetProductDetailAsync(_mockUserId, productId), Times.Once);
        }

        // ================================================================
        // GET /product/{productId}/sale-items
        // ================================================================

        [Fact]
        public async Task GetProductSaleItems_ReturnsOk_WhenProductExists()
        {
            // Arrange
            var productId = 1L;
            var dto = new ProductSaleItemsResponseDto { ProductId = productId };
            _mockProductService.Setup(s => s.GetProductSaleItemsAsync(_mockUserId, productId))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.GetProductSaleItems(productId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockProductService.Verify(s => s.GetProductSaleItemsAsync(_mockUserId, productId), Times.Once);
        }

        // ================================================================
        // POST /product
        // ================================================================

        [Fact]
        public async Task CreateProduct_Returns201_WhenCreatedSuccessfully()
        {
            // Arrange
            var request = new CreateProductRequest { LocationId = 1, ProductName = "New Product" };
            var dto = new ProductListItemDto { ProductId = 10, Name = "New Product" };
            _mockProductService.Setup(s => s.CreateProductAsync(_mockUserId, request))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.CreateProduct(request, null);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            _mockProductService.Verify(s => s.CreateProductAsync(_mockUserId, request), Times.Once);
        }

        // ================================================================
        // PUT /product/{id}
        // ================================================================

        [Fact]
        public async Task UpdateProduct_ReturnsOk_WhenUpdatedSuccessfully()
        {
            // Arrange
            var productId = 1L;
            var request = new UpdateProductRequest { LocationId = 1, ProductName = "Updated" };
            var dto = new ProductListItemDto { ProductId = 1, Name = "Updated" };
            _mockProductService.Setup(s => s.UpdateProductAsync(_mockUserId, productId, request))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.UpdateProduct(productId, request, null);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockProductService.Verify(s => s.UpdateProductAsync(_mockUserId, productId, request), Times.Once);
        }

        // ================================================================
        // PUT /product/{productId}/status
        // ================================================================

        [Fact]
        public async Task UpdateProductStatus_Returns200_WhenServiceReturnsTrue()
        {
            // Arrange
            var productId = 1L;
            var statusRequest = new UpdateProductStatusRequest { Status = "Active" };
            _mockProductService.Setup(s => s.UpdateProductStatusAsync(_mockUserId, productId, statusRequest.Status))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateProductStatus(productId, statusRequest);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task UpdateProductStatus_Returns403_WhenServiceReturnsFalse()
        {
            // Arrange
            var productId = 1L;
            var statusRequest = new UpdateProductStatusRequest { Status = "Active" };
            _mockProductService.Setup(s => s.UpdateProductStatusAsync(_mockUserId, productId, statusRequest.Status))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateProductStatus(productId, statusRequest);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
        }

        // ================================================================
        // DELETE /product/{productId}
        // ================================================================

        [Fact]
        public async Task DeleteProduct_Returns200_WhenSuccessful()
        {
            // Arrange
            var productId = 1L;
            _mockProductService.Setup(s => s.DeleteProductAsync(_mockUserId, productId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteProduct(productId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockProductService.Verify(s => s.DeleteProductAsync(_mockUserId, productId), Times.Once);
        }

        [Fact]
        public async Task DeleteProduct_Returns403_WhenServiceReturnsFalse()
        {
            // Arrange
            var productId = 1L;
            _mockProductService.Setup(s => s.DeleteProductAsync(_mockUserId, productId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteProduct(productId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
        }
    }
}
