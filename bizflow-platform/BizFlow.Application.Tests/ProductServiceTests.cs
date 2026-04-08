using AutoMapper;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Product;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Mappers;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBusinessLocationService> _locationService = new();
    private readonly Mock<IBusinessLocationRepository> _locationRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IImageService> _imageService = new();
    private readonly Mock<IImportService> _importService = new();
    private readonly Mock<IStockMovementService> _stockMovementService = new();
    private readonly Mock<IAiServiceClient> _aiClient = new();
    private readonly Mock<BizFlow.Application.Common.Interfaces.IMessageService> _messageService = new();
    private readonly IMapper _mapper;

    public ProductServiceTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<ProductProfile>(), NullLoggerFactory.Instance);
        _mapper = cfg.CreateMapper();

        _uow.SetupGet(x => x.Products).Returns(_productRepo.Object);
        _uow.SetupGet(x => x.BusinessLocations).Returns(_locationRepo.Object);

        _messageService.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] _) => key);
        _messageService.Setup(m => m.GetMessage(It.IsAny<string>()))
            .Returns((string key) => key);

        _aiClient.Setup(a => a.TriggerVectorStoreSyncAsync(
            It.IsAny<int>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _aiClient.Setup(a => a.TriggerVectorStoreDeleteAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private ProductService BuildSut() => new(
        _uow.Object,
        _imageService.Object,
        _locationService.Object,
        _importService.Object,
        _stockMovementService.Object,
        _messageService.Object,
        _mapper,
        _aiClient.Object);

    // ═══════════════════════════════════════════════════
    // GET PRODUCT DETAIL
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task GetProductDetailAsync_WhenNotFound_ShouldThrowNotFound()
    {
        _productRepo.Setup(r => r.GetByIdWithDetailsAsync(99)).ReturnsAsync((Product?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetProductDetailAsync(Guid.NewGuid(), 99));
    }

    [Fact]
    public async Task GetProductDetailAsync_WhenNoAccess_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var product = new Product { ProductId = 1, BusinessLocationId = 5, ProductName = "Test", Unit = "kg" };
        _productRepo.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(product);
        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, 5))
            .ThrowsAsync(new ForbiddenException("COMMON_FORBIDDEN"));

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetProductDetailAsync(userId, 1));
    }

    // ═══════════════════════════════════════════════════
    // GET PRODUCT SALE ITEMS
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task GetProductSaleItemsAsync_WhenNotFound_ShouldThrowNotFound()
    {
        _productRepo.Setup(r => r.GetByIdWithSaleItemsAsync(88)).ReturnsAsync((Product?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetProductSaleItemsAsync(Guid.NewGuid(), 88));
    }

    // ═══════════════════════════════════════════════════
    // UPDATE PRODUCT STATUS
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task UpdateProductStatusAsync_WhenNotFound_ShouldThrowNotFound()
    {
        _productRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Product?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdateProductStatusAsync(Guid.NewGuid(), 404, "active"));
    }

    [Fact]
    public async Task UpdateProductStatusAsync_WhenNotOwner_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var product = new Product { ProductId = 2, BusinessLocationId = 10, ProductName = "P", Unit = "cái" };
        _productRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(product);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 10)).ReturnsAsync(false);

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.UpdateProductStatusAsync(userId, 2, "active"));
    }

    [Fact]
    public async Task UpdateProductStatusAsync_WhenInvalidStatus_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var product = new Product { ProductId = 3, BusinessLocationId = 10, ProductName = "P", Unit = "cái" };
        _productRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(product);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 10)).ReturnsAsync(true);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.UpdateProductStatusAsync(userId, 3, "invalid_status"));
    }

    [Fact]
    public async Task UpdateProductStatusAsync_WhenValid_ShouldNormalizeAndSave()
    {
        var userId = Guid.NewGuid();
        var product = new Product { ProductId = 4, BusinessLocationId = 10, ProductName = "P", Unit = "cái", Status = "active" };
        _productRepo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(product);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 10)).ReturnsAsync(true);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        await sut.UpdateProductStatusAsync(userId, 4, "INACTIVE");

        Assert.Equal("inactive", product.Status);
        _productRepo.Verify(r => r.Update(product), Times.Once);
    }

    // ═══════════════════════════════════════════════════
    // DELETE PRODUCT
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task DeleteProductAsync_WhenNotFound_ShouldThrowNotFound()
    {
        _productRepo.Setup(r => r.GetByIdWithSaleItemsAsync(999)).ReturnsAsync((Product?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.DeleteProductAsync(Guid.NewGuid(), 999));
    }

    [Fact]
    public async Task DeleteProductAsync_WhenNotOwner_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var product = new Product { ProductId = 5, BusinessLocationId = 20, ProductName = "P", Unit = "cái" };
        _productRepo.Setup(r => r.GetByIdWithSaleItemsAsync(5)).ReturnsAsync(product);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 20)).ReturnsAsync(false);

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.DeleteProductAsync(userId, 5));
    }

    [Fact]
    public async Task DeleteProductAsync_WhenHasHistory_ShouldSoftDelete()
    {
        var userId = Guid.NewGuid();
        var saleItem = new SaleItem { SaleItemId = 1 };
        var product = new Product
        {
            ProductId = 6, BusinessLocationId = 20, ProductName = "P", Unit = "cái",
            SaleItems = new List<SaleItem> { saleItem }
        };
        _productRepo.Setup(r => r.GetByIdWithSaleItemsAsync(6)).ReturnsAsync(product);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 20)).ReturnsAsync(true);
        _productRepo.Setup(r => r.HasHistoryAsync(6)).ReturnsAsync(true);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        await sut.DeleteProductAsync(userId, 6);

        Assert.NotNull(product.DeletedAt);
        Assert.NotNull(saleItem.DeletedAt);
        _productRepo.Verify(r => r.Update(product), Times.Once);
        _productRepo.Verify(r => r.Delete(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenNoHistory_ShouldHardDelete()
    {
        var userId = Guid.NewGuid();
        var product = new Product
        {
            ProductId = 7, BusinessLocationId = 20, ProductName = "P", Unit = "cái",
            SaleItems = new List<SaleItem>()
        };
        _productRepo.Setup(r => r.GetByIdWithSaleItemsAsync(7)).ReturnsAsync(product);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 20)).ReturnsAsync(true);
        _productRepo.Setup(r => r.HasHistoryAsync(7)).ReturnsAsync(false);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        await sut.DeleteProductAsync(userId, 7);

        _productRepo.Verify(r => r.Delete(product), Times.Once);
        _productRepo.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════
    // BULK ADJUST PRICE
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task BulkAdjustSellingPriceAsync_WhenEmptyList_ShouldThrowBadRequest()
    {
        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.BulkAdjustSellingPriceAsync(Guid.NewGuid(),
            new BizFlow.Application.DTOs.Product.BulkAdjustSellingPriceRequest
            {
                SaleItemIds = new List<long>(),
                DeltaAmount = 1000
            }));
    }

    [Fact]
    public async Task BulkAdjustSellingPriceAsync_WhenDeltaIsZero_ShouldThrowBadRequest()
    {
        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.BulkAdjustSellingPriceAsync(Guid.NewGuid(),
            new BizFlow.Application.DTOs.Product.BulkAdjustSellingPriceRequest
            {
                SaleItemIds = new List<long> { 1, 2 },
                DeltaAmount = 0
            }));
    }

    // ═══════════════════════════════════════════════════
    // ADJUST STOCK
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task AdjustProductStockAsync_WhenNotFound_ShouldThrowNotFound()
    {
        _productRepo.Setup(r => r.GetByIdWithDetailsAsync(404)).ReturnsAsync((Product?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.AdjustProductStockAsync(Guid.NewGuid(), 404,
            new BizFlow.Application.DTOs.Product.AdjustProductStockRequest { Stock = 10 }));
    }

    [Fact]
    public async Task AdjustProductStockAsync_WhenNotOwner_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var product = new Product { ProductId = 8, BusinessLocationId = 15, ProductName = "P", Unit = "kg" };
        _productRepo.Setup(r => r.GetByIdWithDetailsAsync(8)).ReturnsAsync(product);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 15)).ReturnsAsync(false);

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.AdjustProductStockAsync(userId, 8,
            new BizFlow.Application.DTOs.Product.AdjustProductStockRequest { Stock = 10 }));
    }

    // ═══════════════════════════════════════════════════
    // EDGE CASES: PERMISSIONS
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CreateProductAsync_WhenUserIsEmployee_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var request = new CreateProductRequest
        {
            LocationId = 15,
            BusinessTypeId = Guid.NewGuid(),
            ProductName = "Employee Draft",
            Unit = "cái",
            TrackInventory = false
        };

        // ValidateLocationAccessAsync succeeds (Employee is part of location safely)
        _locationService.Setup(s => s.ValidateLocationAccessAsync(userId, 15)).Returns(Task.CompletedTask);
        // IsOwner returns false!
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 15)).ReturnsAsync(false);

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.CreateProductAsync(userId, request));
    }
}
