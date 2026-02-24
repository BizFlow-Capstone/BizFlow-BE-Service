using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Product;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using AutoMapper;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICloudinaryService> _mockCloudinaryService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCloudinaryService = new Mock<ICloudinaryService>();
            _mockMapper = new Mock<IMapper>();
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _productService = new ProductService(
                _mockUnitOfWork.Object,
                _mockCloudinaryService.Object,
                _mockMapper.Object);
        }

        #region SearchProductsAsync Tests

        [Fact]
        public async Task SearchProductsAsync_WithValidAccessAndProducts_ReturnsPaginatedResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var query = new ProductQueryParams
            {
                LocationId = locationId,
                PageNumber = 1,
                PageSize = 10
            };
            var products = new List<Product>
            {
                new() { ProductId = 1, ProductName = "Product 1", BusinessLocationId = locationId, Unit = "kg", SaleItems = new List<SaleItem>() },
                new() { ProductId = 2, ProductName = "Product 2", BusinessLocationId = locationId, Unit = "pcs", SaleItems = new List<SaleItem>() }
            };
            var productDtos = new List<ProductListItemDto>
            {
                new() { ProductId = 1, Name = "Product 1", Status = "Active" },
                new() { ProductId = 2, Name = "Product 2", Status = "Active" }
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.HasAccessToLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.SearchAsync(query))
                .ReturnsAsync((products, 2));
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>()))
                .Returns((Product src) => productDtos.FirstOrDefault(d => d.ProductId == src.ProductId) ?? new ProductListItemDto());

            // Act
            var result = await _productService.SearchProductsAsync(userId, query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
            _mockUnitOfWork.Verify(x => x.BusinessLocations.HasAccessToLocationAsync(userId, locationId), Times.Once);
        }

        [Fact]
        public async Task SearchProductsAsync_WithoutAccess_ThrowsForbiddenException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var query = new ProductQueryParams { LocationId = locationId };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.HasAccessToLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(
                () => _productService.SearchProductsAsync(userId, query));
        }

        [Fact]
        public async Task SearchProductsAsync_WithNoResults_ReturnsEmptyPaginatedResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var query = new ProductQueryParams { LocationId = locationId, PageNumber = 1, PageSize = 10 };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.HasAccessToLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.SearchAsync(query))
                .ReturnsAsync((new List<Product>(), 0));
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>()))
                .Returns(new ProductListItemDto());

            // Act
            var result = await _productService.SearchProductsAsync(userId, query);

            // Assert
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        #endregion

        #region GetProductDetailAsync Tests

        [Fact]
        public async Task GetProductDetailAsync_WithValidProductAndAccess_ReturnsProductDetail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, ProductName = "Test Product", BusinessLocationId = locationId, Unit = "kg", SaleItems = new List<SaleItem>() };
            var productDto = new ProductDetailDto 
            { 
                ProductId = productId, 
                Name = "Test Product",
                Status = "Active"
            };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithDetailsAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.HasAccessToLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockMapper.Setup(x => x.Map<ProductDetailDto>(product))
                .Returns(productDto);

            // Act
            var result = await _productService.GetProductDetailAsync(userId, productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result.ProductId);
            Assert.Equal("Test Product", result.Name);
            _mockUnitOfWork.Verify(x => x.Products.GetByIdWithDetailsAsync(productId), Times.Once);
        }

        [Fact]
        public async Task GetProductDetailAsync_WithInvalidProductId_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 999L;
            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithDetailsAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _productService.GetProductDetailAsync(userId, productId));
        }

        [Fact]
        public async Task GetProductDetailAsync_WithoutAccess_ThrowsForbiddenException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithDetailsAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.HasAccessToLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(
                () => _productService.GetProductDetailAsync(userId, productId));
        }

        #endregion

        #region GetProductSaleItemsAsync Tests

        [Fact]
        public async Task GetProductSaleItemsAsync_WithValidProductAndAccess_ReturnsSaleItems()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };
            var saleItemsDto = new ProductSaleItemsResponseDto { ProductId = productId };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.HasAccessToLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockMapper.Setup(x => x.Map<ProductSaleItemsResponseDto>(product))
                .Returns(saleItemsDto);

            // Act
            var result = await _productService.GetProductSaleItemsAsync(userId, productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result.ProductId);
        }

        [Fact]
        public async Task GetProductSaleItemsAsync_WithInvalidProductId_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 999L;
            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _productService.GetProductSaleItemsAsync(userId, productId));
        }

        [Fact]
        public async Task GetProductSaleItemsAsync_WithoutAccess_ThrowsForbiddenException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.HasAccessToLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(
                () => _productService.GetProductSaleItemsAsync(userId, productId));
        }

        #endregion

        #region Additional Product Tests

        [Fact]
        public async Task SearchProductsAsync_WithPagination_ReturnsPaginatedResults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var query = new ProductQueryParams 
            { 
                LocationId = locationId, 
                PageNumber = 2, 
                PageSize = 5 
            };
            var products = new List<Product>
            {
                new() { ProductId = 6, ProductName = "Product 6", BusinessLocationId = locationId, Unit = "kg", SaleItems = new List<SaleItem>() },
                new() { ProductId = 7, ProductName = "Product 7", BusinessLocationId = locationId, Unit = "pcs", SaleItems = new List<SaleItem>() }
            };
            var productDtos = new List<ProductListItemDto>
            {
                new() { ProductId = 6, Name = "Product 6", Status = "Active" },
                new() { ProductId = 7, Name = "Product 7", Status = "Active" }
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.HasAccessToLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.SearchAsync(query))
                .ReturnsAsync((products, 15));
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>()))
                .Returns((Product src) => productDtos.FirstOrDefault(d => d.ProductId == src.ProductId) ?? new ProductListItemDto());

            // Act
            var result = await _productService.SearchProductsAsync(userId, query);

            // Assert
            Assert.Equal(2, result.PageNumber);
            Assert.Equal(5, result.PageSize);
            Assert.Equal(15, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
        }

        #endregion

        #region CreateProductAsync Tests

        [Fact]
        public async Task CreateProductAsync_WithValidOwnerAndNoImage_CreatesProductSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var request = new CreateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                ProductName = "Test Product",
                Sku = "SKU-001",
                TrackInventory = true,
                Unit = "kg",
                CostPrice = 100m,
                Stock = 10,
                Manufacturer = "Test Manufacturer",
                PriceTiers = new List<PriceTierRequest>()
            };

            var createdProduct = new Product
            {
                ProductId = 1,
                ProductName = request.ProductName,
                BusinessLocationId = locationId,
                Unit = request.Unit,
                SaleItems = new List<SaleItem>()
            };
            var productDto = new ProductListItemDto { ProductId = 1, Name = "Test Product" };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.AddAsync(It.IsAny<Product>()))
                .ReturnsAsync(createdProduct);
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>()))
                .Returns(productDto);

            // Act
            var result = await _productService.CreateProductAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ProductId);
            Assert.Equal("Test Product", result.Name);
            _mockUnitOfWork.Verify(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId), Times.Once);
            _mockUnitOfWork.Verify(x => x.Products.AddAsync(It.IsAny<Product>()), Times.Once);
        }

        [Fact]
        public async Task CreateProductAsync_WithoutOwnership_ThrowsForbiddenException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var request = new CreateProductRequest { LocationId = locationId };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(
                () => _productService.CreateProductAsync(userId, request));
        }

        [Fact]
        public async Task CreateProductAsync_WithDuplicateUnitInPriceTiers_ThrowsBadRequestException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var request = new CreateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                ProductName = "Test Product",
                Unit = "kg",
                CostPrice = 100m,
                PriceTiers = new List<PriceTierRequest>
                {
                    new() { Unit = "kg", Quantity = 5, Price = 50m }  // Duplicate of main unit
                }
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _productService.CreateProductAsync(userId, request));
        }

        // Note: Image upload failure test cannot be tested directly because ImageStream is internal
        // and can only be set via controller deserialization. This would require integration tests.

        [Fact]
        public async Task CreateProductAsync_WithPriceTiers_CreatesSaleItemsForEachTier()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var request = new CreateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                ProductName = "Test Product",
                Unit = "kg",
                CostPrice = 100m,
                PriceTiers = new List<PriceTierRequest>
                {
                    new() { Unit = "pack", Quantity = 5, Price = 450m },
                    new() { Unit = "box", Quantity = 10, Price = 800m }
                }
            };

            var productDto = new ProductListItemDto { ProductId = 1, Name = "Test Product" };

            var createdProduct2 = new Product
            {
                ProductId = 1,
                ProductName = request.ProductName,
                BusinessLocationId = locationId,
                Unit = request.Unit,
                SaleItems = new List<SaleItem>()
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.AddAsync(It.IsAny<Product>()))
                .ReturnsAsync(createdProduct2);
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>()))
                .Returns(productDto);

            // Act
            var result = await _productService.CreateProductAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            _mockUnitOfWork.Verify(x => x.Products.AddAsync(It.IsAny<Product>()), Times.Once);
        }

        #endregion

        #region UpdateProductAsync Tests

        [Fact]
        public async Task UpdateProductAsync_WithValidOwnerAndUniqueName_UpdatesProductSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var request = new UpdateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                ProductName = "Updated Product",
                Sku = "SKU-001",
                TrackInventory = true,
                Unit = "kg",
                CostPrice = 120m,
                Stock = 15,
                Manufacturer = "Updated Manufacturer",
                PriceTiers = new List<PriceTierRequest>(),
                RemoveImage = false
            };

            var product = new Product
            {
                ProductId = productId,
                ProductName = "Old Product",
                BusinessLocationId = locationId,
                Unit = "kg",
                SaleItems = new List<SaleItem>()
            };
            var productDto = new ProductListItemDto { ProductId = 1, Name = "Updated Product" };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.Update(product));
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(product))
                .Returns(productDto);

            // Act
            var result = await _productService.UpdateProductAsync(userId, productId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Product", result.Name);
            _mockUnitOfWork.Verify(x => x.Products.GetByIdWithSaleItemsAsync(productId), Times.Once);
            _mockUnitOfWork.Verify(x => x.Products.Update(product), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_WithInvalidProductId_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 999L;
            var request = new UpdateProductRequest();

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _productService.UpdateProductAsync(userId, productId, request));
        }

        [Fact]
        public async Task UpdateProductAsync_WithoutOwnership_ThrowsForbiddenException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };
            var request = new UpdateProductRequest { LocationId = locationId };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(
                () => _productService.UpdateProductAsync(userId, productId, request));
        }

        [Fact]
        public async Task UpdateProductAsync_WithDifferentLocation_ThrowsBadRequestException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };
            var request = new UpdateProductRequest { LocationId = 999 };  // Different location

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _productService.UpdateProductAsync(userId, productId, request));
        }

        [Fact]
        public async Task UpdateProductAsync_WithImageRemoval_DeletesImageFromCloudinary()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product
            {
                ProductId = productId,
                BusinessLocationId = locationId,
                ImagePublicId = "public-id-123",
                Unit = "kg",
                SaleItems = new List<SaleItem>()
            };
            var request = new UpdateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                Unit = "kg",
                CostPrice = 100m,
                RemoveImage = true,
                PriceTiers = new List<PriceTierRequest>()
            };
            var productDto = new ProductListItemDto { ProductId = 1, Name = "Product" };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockCloudinaryService.Setup(x => x.DeleteImageAsync("public-id-123"))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.Update(product));
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(product))
                .Returns(productDto);

            // Act
            var result = await _productService.UpdateProductAsync(userId, productId, request);

            // Assert
            Assert.NotNull(result);
            _mockCloudinaryService.Verify(x => x.DeleteImageAsync("public-id-123"), Times.Once);
        }

        #endregion

        #region UpdateProductStatusAsync Tests

        [Fact]
        public async Task UpdateProductStatusAsync_WithValidOwnerAndActiveStatus_UpdatesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId, Status = ProductStatus.Inactive };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.Update(product));

            // Act
            var result = await _productService.UpdateProductStatusAsync(userId, productId, ProductStatus.Active);

            // Assert
            Assert.True(result);
            Assert.Equal(ProductStatus.Active, product.Status);
            _mockUnitOfWork.Verify(x => x.Products.Update(product), Times.Once);
        }

        [Fact]
        public async Task UpdateProductStatusAsync_WithInvalidProductId_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 999L;

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _productService.UpdateProductStatusAsync(userId, productId, ProductStatus.Active));
        }

        [Fact]
        public async Task UpdateProductStatusAsync_WithoutOwnership_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act
            var result = await _productService.UpdateProductStatusAsync(userId, productId, ProductStatus.Active);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateProductStatusAsync_WithInvalidStatus_ThrowsBadRequestException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _productService.UpdateProductStatusAsync(userId, productId, "InvalidStatus"));
        }

        #endregion

        #region DeleteProductAsync Tests

        [Fact]
        public async Task DeleteProductAsync_WithValidOwnerAndNoHistory_DeletesProductPermanently()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.HasHistoryAsync(productId))
                .ReturnsAsync(false);
            _mockUnitOfWork.Setup(x => x.Products.Delete(product));

            // Act
            var result = await _productService.DeleteProductAsync(userId, productId);

            // Assert
            Assert.True(result);
            _mockUnitOfWork.Verify(x => x.Products.Delete(product), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_WithValidOwnerAndHistory_SoftDeletesProduct()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.HasHistoryAsync(productId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.Update(product));

            // Act
            var result = await _productService.DeleteProductAsync(userId, productId);

            // Assert
            Assert.True(result);
            Assert.NotNull(product.DeletedAt);
            _mockUnitOfWork.Verify(x => x.Products.Update(product), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_WithInvalidProductId_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 999L;

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _productService.DeleteProductAsync(userId, productId));
        }

        [Fact]
        public async Task DeleteProductAsync_WithoutOwnership_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act
            var result = await _productService.DeleteProductAsync(userId, productId);

            // Assert
            Assert.False(result);
        }

        #endregion

        // ===========================================================
        // NOTE: Tests below exist because mocks can mask real bugs.
        // If mapper profile is misconfigured or SaveChanges fails in
        // reality, the tests above still pass (mocks hide it).
        // These tests enforce that the service wires up correctly.
        // ===========================================================

        #region Mapper & SaveChanges Strict Verification

        [Fact]
        public async Task CreateProductAsync_CallsMapper_ExactlyOnce()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var request = new CreateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                ProductName = "Strict Test",
                Unit = "kg",
                CostPrice = 100m,
                PriceTiers = new List<PriceTierRequest>()
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.AddAsync(It.IsAny<Product>())).ReturnsAsync((Product p) => p);
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>())).Returns(new ProductListItemDto());

            // Act
            await _productService.CreateProductAsync(userId, request);

            // Assert — mapper MUST be called exactly once; if it's 0 the return value would be wrong
            _mockMapper.Verify(x => x.Map<ProductListItemDto>(It.IsAny<Product>()), Times.Once);
        }

        [Fact]
        public async Task CreateProductAsync_CallsSaveChanges_ExactlyOnce()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var request = new CreateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                ProductName = "SaveChanges test",
                Unit = "kg",
                CostPrice = 50m,
                PriceTiers = new List<PriceTierRequest>()
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.AddAsync(It.IsAny<Product>())).ReturnsAsync((Product p) => p);
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>())).Returns(new ProductListItemDto());

            // Act
            await _productService.CreateProductAsync(userId, request);

            // Assert — EF SaveChanges must be called after AddAsync
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_CallsMapper_ExactlyOnce()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product
            {
                ProductId = productId,
                BusinessLocationId = locationId,
                Unit = "kg",
                SaleItems = new List<SaleItem>()
            };
            var request = new UpdateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                Unit = "kg",
                CostPrice = 100m,
                PriceTiers = new List<PriceTierRequest>()
            };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId)).ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>())).Returns(new ProductListItemDto());

            // Act
            await _productService.UpdateProductAsync(userId, productId, request);

            // Assert
            _mockMapper.Verify(x => x.Map<ProductListItemDto>(product), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_CallsSaveChanges_ExactlyOnce()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product
            {
                ProductId = productId,
                BusinessLocationId = locationId,
                Unit = "kg",
                SaleItems = new List<SaleItem>()
            };
            var request = new UpdateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                Unit = "kg",
                CostPrice = 100m,
                PriceTiers = new List<PriceTierRequest>()
            };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId)).ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>())).Returns(new ProductListItemDto());

            // Act
            await _productService.UpdateProductAsync(userId, productId, request);

            // Assert
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_WhenSaveChangesFails_ThrowsAndDoesNotSwallow()
        {
            // Arrange — real bug scenario: SaveChanges throws (e.g. DB constraint violation)
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product
            {
                ProductId = productId,
                BusinessLocationId = locationId,
                Unit = "kg",
                SaleItems = new List<SaleItem>()
            };
            var request = new UpdateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                Unit = "kg",
                CostPrice = 100m,
                PriceTiers = new List<PriceTierRequest>()
            };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId)).ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>())).Returns(new ProductListItemDto());

            // Override the constructor default: SaveChanges now throws
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB constraint violated"));

            // Act & Assert — service must NOT swallow DB exceptions
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _productService.UpdateProductAsync(userId, productId, request));
        }

        [Fact]
        public async Task CreateProductAsync_WhenSaveChangesFails_AndNoImage_StillThrows()
        {
            // Arrange — no image, so no Cloudinary rollback needed; just confirms re-throw
            var userId = Guid.NewGuid();
            var locationId = 1;
            var request = new CreateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                ProductName = "DB fail",
                Unit = "kg",
                CostPrice = 100m,
                PriceTiers = new List<PriceTierRequest>()
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.AddAsync(It.IsAny<Product>())).ReturnsAsync((Product p) => p);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("EF Core error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _productService.CreateProductAsync(userId, request));

            // Cloudinary delete should NOT be called (no image was uploaded)
            _mockCloudinaryService.Verify(x => x.DeleteImageAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteProductAsync_CallsSaveChanges_WhenHardDeleting()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId)).ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.HasHistoryAsync(productId)).ReturnsAsync(false);

            // Act
            await _productService.DeleteProductAsync(userId, productId);

            // Assert — SaveChanges must be called after Delete
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.Products.Delete(product), Times.Once);
        }

        #endregion

        #region UpdateProductAsync - SaleItem Smart Merge Tests

        [Fact]
        public async Task UpdateProductAsync_WithMatchingExistingSaleItem_UpdatesPricePolicyInPlace()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;

            var existingPolicy = new ProductPricePolicy { Price = 80m, IsDefault = true, StartAt = DateTime.UtcNow };
            var existingSaleItem = new SaleItem
            {
                SaleItemId = 5,
                Unit = "kg",
                Quantity = 1,
                ProductPricePolicies = new List<ProductPricePolicy> { existingPolicy }
            };
            var product = new Product
            {
                ProductId = productId,
                BusinessLocationId = locationId,
                Unit = "kg",
                SaleItems = new List<SaleItem> { existingSaleItem }
            };
            var request = new UpdateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                Unit = "kg",
                CostPrice = 120m,     // Updated price
                PriceTiers = new List<PriceTierRequest>()
            };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId)).ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(product)).Returns(new ProductListItemDto());

            // Act
            await _productService.UpdateProductAsync(userId, productId, request);

            // Assert — price policy updated in-place, no new SaleItem added
            Assert.Equal(120m, existingPolicy.Price);
            Assert.Single(product.SaleItems);
        }

        [Fact]
        public async Task UpdateProductAsync_WithRemovedPriceTier_SoftDeletesObsoleteSaleItem()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;

            var defaultPolicy = new ProductPricePolicy { Price = 100m, IsDefault = true, StartAt = DateTime.UtcNow };
            var packPolicy   = new ProductPricePolicy { Price = 450m, IsDefault = true, StartAt = DateTime.UtcNow };

            var baseItem = new SaleItem
            {
                SaleItemId = 1, Unit = "kg", Quantity = 1,
                ProductPricePolicies = new List<ProductPricePolicy> { defaultPolicy }
            };
            var packItem = new SaleItem
            {
                SaleItemId = 2, Unit = "pack", Quantity = 5,
                ProductPricePolicies = new List<ProductPricePolicy> { packPolicy }
            };

            var product = new Product
            {
                ProductId = productId,
                BusinessLocationId = locationId,
                Unit = "kg",
                SaleItems = new List<SaleItem> { baseItem, packItem }
            };

            // Request removes the "pack" tier
            var request = new UpdateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                Unit = "kg",
                CostPrice = 100m,
                PriceTiers = new List<PriceTierRequest>() // no extra tiers
            };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId)).ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(product)).Returns(new ProductListItemDto());

            // Act
            await _productService.UpdateProductAsync(userId, productId, request);

            // Assert — pack item soft-deleted (DeletedAt set)
            Assert.Null(baseItem.DeletedAt);
            Assert.NotNull(packItem.DeletedAt);
        }

        [Fact]
        public async Task UpdateProductAsync_WithNewPriceTier_AddsNewSaleItem()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;

            var defaultPolicy = new ProductPricePolicy { Price = 100m, IsDefault = true, StartAt = DateTime.UtcNow };
            var baseItem = new SaleItem
            {
                SaleItemId = 1, Unit = "kg", Quantity = 1,
                ProductPricePolicies = new List<ProductPricePolicy> { defaultPolicy }
            };

            var product = new Product
            {
                ProductId = productId,
                BusinessLocationId = locationId,
                Unit = "kg",
                SaleItems = new List<SaleItem> { baseItem }
            };

            var request = new UpdateProductRequest
            {
                LocationId = locationId,
                BusinessTypeId = Guid.NewGuid(),
                Unit = "kg",
                CostPrice = 100m,
                PriceTiers = new List<PriceTierRequest>
                {
                    new() { Unit = "box", Quantity = 10, Price = 900m }  // brand new tier
                }
            };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId)).ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(product)).Returns(new ProductListItemDto());

            // Act
            await _productService.UpdateProductAsync(userId, productId, request);

            // Assert — new "box" SaleItem added
            Assert.Equal(2, product.SaleItems.Count);
            Assert.Contains(product.SaleItems, s => s.Unit == "box" && s.Quantity == 10);
        }

        [Fact]
        public async Task UpdateProductAsync_WithDuplicateUnitInPriceTiers_ThrowsBadRequestException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product
            {
                ProductId = productId,
                BusinessLocationId = locationId,
                Unit = "kg",
                SaleItems = new List<SaleItem>()
            };

            var request = new UpdateProductRequest
            {
                LocationId = locationId,
                Unit = "kg",
                CostPrice = 100m,
                PriceTiers = new List<PriceTierRequest>
                {
                    new() { Unit = "KG", Quantity = 5, Price = 80m } // Duplicate of main unit (case-insensitive)
                }
            };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdWithSaleItemsAsync(productId)).ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _productService.UpdateProductAsync(userId, productId, request));
        }

        #endregion

        #region UpdateProductStatusAsync - Status normalization Tests

        [Fact]
        public async Task UpdateProductStatusAsync_WithUpperCaseActiveStatus_NormalizesAndUpdates()
        {
            // Arrange — service calls .ToLower() before comparing, so "ACTIVE" should pass
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId, Status = ProductStatus.Inactive };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId)).ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);

            // Act
            var result = await _productService.UpdateProductStatusAsync(userId, productId, "ACTIVE");

            // Assert
            Assert.True(result);
            Assert.Equal(ProductStatus.Active, product.Status);
        }

        [Fact]
        public async Task UpdateProductStatusAsync_SetToInactive_UpdatesStatusCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = 1L;
            var locationId = 1;
            var product = new Product { ProductId = productId, BusinessLocationId = locationId, Status = ProductStatus.Active };

            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId)).ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);

            // Act
            var result = await _productService.UpdateProductStatusAsync(userId, productId, ProductStatus.Inactive);

            // Assert
            Assert.True(result);
            Assert.Equal(ProductStatus.Inactive, product.Status);
            _mockUnitOfWork.Verify(x => x.Products.Update(product), Times.Once);
        }

        #endregion

        #region SearchProductsAsync - Default Pagination Tests

        [Fact]
        public async Task SearchProductsAsync_WithNullPageParams_UsesDefaultPagination()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var query = new ProductQueryParams { LocationId = locationId, PageNumber = null, PageSize = null };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.HasAccessToLocationAsync(userId, locationId)).ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Products.SearchAsync(query)).ReturnsAsync((new List<Product>(), 0));
            _mockMapper.Setup(x => x.Map<ProductListItemDto>(It.IsAny<Product>())).Returns(new ProductListItemDto());

            // Act
            var result = await _productService.SearchProductsAsync(userId, query);

            // Assert — defaults: page 1, size 10
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        #endregion

        #region GetDefaultPrice Static Helper Tests

        [Fact]
        public void GetDefaultPrice_WithMatchingUnitAndDefaultPolicy_ReturnsPrice()
        {
            // Arrange
            var product = new Product
            {
                Unit = "kg",
                SaleItems = new List<SaleItem>
                {
                    new()
                    {
                        Unit = "kg",
                        Quantity = 1,
                        ProductPricePolicies = new List<ProductPricePolicy>
                        {
                            new() { Price = 150m, IsDefault = true, StartAt = DateTime.UtcNow }
                        }
                    }
                }
            };

            // Act
            var price = ProductService.GetDefaultPrice(product);

            // Assert
            Assert.Equal(150m, price);
        }

        [Fact]
        public void GetDefaultPrice_WithNoMatchingSaleItem_ReturnsZero()
        {
            // Arrange
            var product = new Product
            {
                Unit = "kg",
                SaleItems = new List<SaleItem>
                {
                    new() { Unit = "box", Quantity = 5, ProductPricePolicies = new List<ProductPricePolicy>() }
                }
            };

            // Act
            var price = ProductService.GetDefaultPrice(product);

            // Assert
            Assert.Equal(0m, price);
        }

        [Fact]
        public void GetDefaultPrice_WithNoDefaultPolicy_ReturnsZero()
        {
            // Arrange — matching unit but no IsDefault=true policy
            var product = new Product
            {
                Unit = "kg",
                SaleItems = new List<SaleItem>
                {
                    new()
                    {
                        Unit = "kg",
                        Quantity = 1,
                        ProductPricePolicies = new List<ProductPricePolicy>
                        {
                            new() { Price = 200m, IsDefault = false, StartAt = DateTime.UtcNow }
                        }
                    }
                }
            };

            // Act
            var price = ProductService.GetDefaultPrice(product);

            // Assert
            Assert.Equal(0m, price);
        }

        [Fact]
        public void GetDefaultPrice_IsCaseInsensitiveUnitMatch()
        {
            // Arrange — product.Unit = "KG", SaleItem.Unit = "kg"
            var product = new Product
            {
                Unit = "KG",
                SaleItems = new List<SaleItem>
                {
                    new()
                    {
                        Unit = "kg",
                        Quantity = 1,
                        ProductPricePolicies = new List<ProductPricePolicy>
                        {
                            new() { Price = 99m, IsDefault = true, StartAt = DateTime.UtcNow }
                        }
                    }
                }
            };

            // Act
            var price = ProductService.GetDefaultPrice(product);

            // Assert — OrdinalIgnoreCase match
            Assert.Equal(99m, price);
        }

        #endregion

      
    }
}