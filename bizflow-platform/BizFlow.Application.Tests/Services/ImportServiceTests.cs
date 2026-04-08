using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Import;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Services
{
    public class ImportServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ImportService _importService;

        public ImportServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _importService = new ImportService(
                _mockUnitOfWork.Object,
                _mockMapper.Object);
        }

        // =========================================================
        // Helpers
        // =========================================================

        private static Import BuildDraftImport(long importId = 1, int locationId = 1)
            => new()
            {
                ImportId = importId,
                ImportCode = "DRAFT001",
                ImportType = "INVOICE",
                Status = ImportStatus.Draft,
                BusinessLocationId = locationId,
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow,
                ProductsImports = new List<ProductImport>()
            };

        private static Import BuildConfirmedImport(long importId = 1, int locationId = 1)
            => new()
            {
                ImportId = importId,
                ImportCode = "CONF001",
                ImportType = "INVOICE",
                Status = ImportStatus.Confirmed,
                BusinessLocationId = locationId,
                TotalAmount = 500,
                CreatedAt = DateTime.UtcNow,
                ProductsImports = new List<ProductImport>
                {
                    new()
                    {
                        ImportId = importId,
                        ProductId = 10,
                        Quantity = 5,
                        CostPrice = 100m,
                        TotalPrice = 500m,
                        BaseUnit = "kg",
                        Product = new Product { ProductId = 10, Stock = 20, Unit = "kg" }
                    }
                }
            };

        private static Import BuildCancelledImport(long importId = 1)
            => new()
            {
                ImportId = importId,
                ImportCode = "CANC001",
                ImportType = "INVOICE",
                Status = ImportStatus.Cancelled,
                BusinessLocationId = 1,
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow,
                ProductsImports = new List<ProductImport>()
            };

        // =========================================================
        // 1. GetTemplateAsync
        // =========================================================

        #region GetTemplateAsync Tests

        [Fact]
        public async Task GetTemplateAsync_WhenActiveSchemaExists_ReturnsSchema()
        {
            // Arrange
            var schemaVersion = new ImportSchemaVersion { SchemaJson = "{\"type\":\"object\"}" };

            _mockUnitOfWork.Setup(x => x.Imports.GetActiveSchemaVersionAsync())
                .ReturnsAsync(schemaVersion);

            // Act
            var result = await _importService.GetTemplateAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("{\"type\":\"object\"}", result.SchemaJson);
        }

        [Fact]
        public async Task GetTemplateAsync_WhenNoActiveSchema_ThrowsNotFoundException()
        {
            // Arrange
            _mockUnitOfWork.Setup(x => x.Imports.GetActiveSchemaVersionAsync())
                .ReturnsAsync((ImportSchemaVersion?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _importService.GetTemplateAsync());
        }

        #endregion

        // =========================================================
        // 2. CreateImportAsync
        // =========================================================

        #region CreateImportAsync Tests

        [Fact]
        public async Task CreateImportAsync_AsDraft_CreatesImportWithDraftStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var productId = 10L;

            var request = new CreateImportRequest
            {
                ImportType = "INVOICE",
                BusinessLocationId = locationId,
                SaveAsDraft = true,
                Items = new List<ImportItemRequest>
                {
                    new() { ProductId = productId, Quantity = 3, CostPrice = 100m }
                }
            };

            var location = new BusinessLocation { BusinessLocationId = locationId, Name = "Main Store" };
            var product = new Product { ProductId = productId, Stock = 10, Unit = "kg" };
            var createdImport = BuildDraftImport(importId: 1, locationId: locationId);
            var expectedDto = new ImportSummaryDto
            {
                ImportId = 1,
                Status = ImportStatus.Draft,
                BusinessLocationName = "Main Store"
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.Imports.AddAsync(It.IsAny<Import>()))
                .ReturnsAsync(createdImport);
            _mockMapper.Setup(x => x.Map<ImportSummaryDto>(It.IsAny<Import>()))
                .Returns(expectedDto);

            // Act
            var result = await _importService.CreateImportAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ImportStatus.Draft, result.Status);
            Assert.Equal("Main Store", result.BusinessLocationName);
            _mockUnitOfWork.Verify(x => x.Imports.AddAsync(It.IsAny<Import>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));
        }

        [Fact]
        public async Task CreateImportAsync_AsConfirmed_UpdatesProductStock()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var productId = 10L;

            var request = new CreateImportRequest
            {
                ImportType = "INVOICE",
                BusinessLocationId = locationId,
                SaveAsDraft = false,
                ReceivedAt = DateTime.UtcNow,
                Items = new List<ImportItemRequest>
                {
                    new() { ProductId = productId, Quantity = 5, CostPrice = 100m }
                }
            };

            var location = new BusinessLocation { BusinessLocationId = locationId, Name = "Main Store" };
            var product = new Product { ProductId = productId, Stock = 10, Unit = "kg" };
            var createdImport = BuildDraftImport(importId: 2, locationId: locationId);
            var expectedDto = new ImportSummaryDto { ImportId = 2, Status = ImportStatus.Confirmed };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.Imports.AddAsync(It.IsAny<Import>()))
                .ReturnsAsync(createdImport);
            _mockMapper.Setup(x => x.Map<ImportSummaryDto>(It.IsAny<Import>()))
                .Returns(expectedDto);

            // Act
            var result = await _importService.CreateImportAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            // Stock should have been updated
            _mockUnitOfWork.Verify(x => x.Products.Update(product), Times.Once);
        }

        [Fact]
        public async Task CreateImportAsync_WithInvalidLocation_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateImportRequest
            {
                ImportType = "INVOICE",
                BusinessLocationId = 999,
                SaveAsDraft = true,
                Items = new List<ImportItemRequest>()
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(999))
                .ReturnsAsync((BusinessLocation?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _importService.CreateImportAsync(userId, request));
        }

        [Fact]
        public async Task CreateImportAsync_AsConfirmedWithoutReceivedAt_ThrowsBadRequestException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var request = new CreateImportRequest
            {
                ImportType = "INVOICE",
                BusinessLocationId = locationId,
                SaveAsDraft = false,
                ReceivedAt = null,  // Missing required date
                Items = new List<ImportItemRequest>()
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(new BusinessLocation { BusinessLocationId = locationId, Name = "Store" });

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _importService.CreateImportAsync(userId, request));
        }

        [Fact]
        public async Task CreateImportAsync_WithInvalidProduct_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var request = new CreateImportRequest
            {
                ImportType = "INVOICE",
                BusinessLocationId = locationId,
                SaveAsDraft = true,
                Items = new List<ImportItemRequest>
                {
                    new() { ProductId = 999, Quantity = 1, CostPrice = 50m }
                }
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(new BusinessLocation { BusinessLocationId = locationId, Name = "Store" });
            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(999))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _importService.CreateImportAsync(userId, request));
        }

        [Fact]
        public async Task CreateImportAsync_CalculatesTotalAmountCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;

            var request = new CreateImportRequest
            {
                ImportType = "INVOICE",
                BusinessLocationId = locationId,
                SaveAsDraft = true,
                Items = new List<ImportItemRequest>
                {
                    new() { ProductId = 10, Quantity = 2, CostPrice = 100m },
                    new() { ProductId = 11, Quantity = 3, CostPrice = 50m }
                }
            };

            var location = new BusinessLocation { BusinessLocationId = locationId, Name = "Store" };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(10))
                .ReturnsAsync(new Product { ProductId = 10, Stock = 5, Unit = "kg" });
            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(11))
                .ReturnsAsync(new Product { ProductId = 11, Stock = 8, Unit = "pcs" });

            Import? capturedImport = null;
            _mockUnitOfWork.Setup(x => x.Imports.AddAsync(It.IsAny<Import>()))
                .Callback<Import>(imp => capturedImport = imp)
                .ReturnsAsync((Import imp) => imp);

            _mockMapper.Setup(x => x.Map<ImportSummaryDto>(It.IsAny<Import>()))
                .Returns(new ImportSummaryDto());

            // Act
            await _importService.CreateImportAsync(userId, request);

            // Assert — 2×100 + 3×50 = 350
            Assert.NotNull(capturedImport);
            Assert.Equal(350m, capturedImport!.TotalAmount);
        }

        #endregion

        // =========================================================
        // 3. UpdateImportAsync
        // =========================================================

        #region UpdateImportAsync Tests

        [Fact]
        public async Task UpdateImportAsync_WithDraftImportAndNewItems_UpdatesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var productId = 10L;

            var import = BuildDraftImport(importId);
            var request = new UpdateImportRequest
            {
                ImportType = "RETURN",
                Supplier = "New Supplier",
                Note = "Updated note",
                Items = new List<ImportItemRequest>
                {
                    new() { ProductId = productId, Quantity = 4, CostPrice = 75m }
                }
            };

            var expectedDto = new ImportSummaryDto { ImportId = importId };

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);
            _mockUnitOfWork.Setup(x => x.Products.GetByIdAsync(productId))
                .ReturnsAsync(new Product { ProductId = productId, Stock = 10, Unit = "kg" });
            _mockMapper.Setup(x => x.Map<ImportSummaryDto>(import))
                .Returns(expectedDto);

            // Act
            var result = await _importService.UpdateImportAsync(userId, importId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(importId, result.ImportId);
            _mockUnitOfWork.Verify(x => x.Imports.Update(import), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateImportAsync_WithNullItemsClearsItems()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var import = BuildDraftImport(importId);
            import.ProductsImports = new List<ProductImport>
            {
                new() { ImportId = importId, ProductId = 5, Quantity = 2, CostPrice = 50m, TotalPrice = 100m, BaseUnit = "kg" }
            };

            var request = new UpdateImportRequest
            {
                Items = null  // Clear all items
            };

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);
            _mockMapper.Setup(x => x.Map<ImportSummaryDto>(import))
                .Returns(new ImportSummaryDto { ImportId = importId });

            // Act
            var result = await _importService.UpdateImportAsync(userId, importId, request);

            // Assert
            Assert.Empty(import.ProductsImports);
            Assert.Equal(0, import.TotalAmount);
        }

        [Fact]
        public async Task UpdateImportAsync_WithNonExistentImport_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 999L;

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync((Import?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _importService.UpdateImportAsync(userId, importId, new UpdateImportRequest()));
        }

        [Fact]
        public async Task UpdateImportAsync_WithConfirmedImport_ThrowsBadRequestException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var import = BuildConfirmedImport(importId);

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _importService.UpdateImportAsync(userId, importId, new UpdateImportRequest()));
        }

        #endregion

        // =========================================================
        // 4. PatchImportAsync (Confirm DRAFT)
        // =========================================================

        #region PatchImportAsync Tests

        [Fact]
        public async Task PatchImportAsync_WithDraftAndValidDate_ConfirmsImportAndUpdatesStock()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var receivedAt = DateTime.UtcNow;

            var product = new Product { ProductId = 10, Stock = 5, Unit = "kg" };
            var import = BuildDraftImport(importId);
            import.ProductsImports = new List<ProductImport>
            {
                new()
                {
                    ImportId = importId,
                    ProductId = 10,
                    Quantity = 3,
                    CostPrice = 100m,
                    TotalPrice = 300m,
                    BaseUnit = "kg",
                    Product = product
                }
            };

            var request = new PatchImportRequest { ReceivedAt = receivedAt };

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);

            // Act
            var result = await _importService.PatchImportAsync(userId, importId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ImportStatus.Confirmed, import.Status);
            Assert.Equal(8, product.Stock);  // 5 + 3
            _mockUnitOfWork.Verify(x => x.Products.Update(product), Times.Once);
            _mockUnitOfWork.Verify(x => x.Imports.Update(import), Times.Once);
        }

        [Fact]
        public async Task PatchImportAsync_WithNonExistentImport_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 999L;

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync((Import?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _importService.PatchImportAsync(userId, importId, new PatchImportRequest { ReceivedAt = DateTime.UtcNow }));
        }

        [Fact]
        public async Task PatchImportAsync_WithConfirmedImport_ThrowsBadRequestException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var import = BuildConfirmedImport(importId);

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _importService.PatchImportAsync(userId, importId, new PatchImportRequest { ReceivedAt = DateTime.UtcNow }));
        }

        [Fact]
        public async Task PatchImportAsync_WithoutReceivedAt_ThrowsBadRequestException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var import = BuildDraftImport(importId);

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _importService.PatchImportAsync(userId, importId, new PatchImportRequest { ReceivedAt = null }));
        }

        [Fact]
        public async Task PatchImportAsync_WithNullProduct_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;

            var import = BuildDraftImport(importId);
            import.ProductsImports = new List<ProductImport>
            {
                new()
                {
                    ImportId = importId,
                    ProductId = 10,
                    Quantity = 2,
                    CostPrice = 50m,
                    TotalPrice = 100m,
                    BaseUnit = "kg",
                    Product = null!  // Missing navigation property
                }
            };

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _importService.PatchImportAsync(userId, importId, new PatchImportRequest { ReceivedAt = DateTime.UtcNow }));
        }

        #endregion

        // =========================================================
        // 5. ListImportsAsync
        // =========================================================

        #region ListImportsAsync Tests

        [Fact]
        public async Task ListImportsAsync_WithResults_ReturnsPaginatedResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new ImportQueryParams { PageNumber = 1, PageSize = 10 };

            var imports = new List<Import>
            {
                BuildDraftImport(1),
                BuildConfirmedImport(2)
            };
            var dtos = new List<ImportSummaryDto>
            {
                new() { ImportId = 1 },
                new() { ImportId = 2 }
            };

            _mockUnitOfWork.Setup(x => x.Imports.SearchAsync(query))
                .ReturnsAsync((imports, 2));
            _mockMapper.Setup(x => x.Map<List<ImportSummaryDto>>(imports))
                .Returns(dtos);

            // Act
            var result = await _importService.ListImportsAsync(userId, query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public async Task ListImportsAsync_WithNoResults_ReturnsEmptyPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new ImportQueryParams { PageNumber = 1, PageSize = 10, Status = "DRAFT" };

            _mockUnitOfWork.Setup(x => x.Imports.SearchAsync(query))
                .ReturnsAsync((new List<Import>(), 0));
            _mockMapper.Setup(x => x.Map<List<ImportSummaryDto>>(It.IsAny<List<Import>>()))
                .Returns(new List<ImportSummaryDto>());

            // Act
            var result = await _importService.ListImportsAsync(userId, query);

            // Assert
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task ListImportsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new ImportQueryParams { PageNumber = 3, PageSize = 5 };
            var imports = new List<Import> { BuildDraftImport(11) };
            var dtos = new List<ImportSummaryDto> { new() { ImportId = 11 } };

            _mockUnitOfWork.Setup(x => x.Imports.SearchAsync(query))
                .ReturnsAsync((imports, 11));
            _mockMapper.Setup(x => x.Map<List<ImportSummaryDto>>(imports))
                .Returns(dtos);

            // Act
            var result = await _importService.ListImportsAsync(userId, query);

            // Assert
            Assert.Equal(3, result.PageNumber);
            Assert.Equal(5, result.PageSize);
            Assert.Equal(11, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
        }

        [Fact]
        public async Task ListImportsAsync_WithNullPageParams_UsesDefaults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new ImportQueryParams { PageNumber = null, PageSize = null };

            _mockUnitOfWork.Setup(x => x.Imports.SearchAsync(query))
                .ReturnsAsync((new List<Import>(), 0));
            _mockMapper.Setup(x => x.Map<List<ImportSummaryDto>>(It.IsAny<List<Import>>()))
                .Returns(new List<ImportSummaryDto>());

            // Act
            var result = await _importService.ListImportsAsync(userId, query);

            // Assert
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        #endregion

        // =========================================================
        // 6. GetImportDetailAsync
        // =========================================================

        #region GetImportDetailAsync Tests

        [Fact]
        public async Task GetImportDetailAsync_WithValidId_ReturnsDetailDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var import = BuildConfirmedImport(importId);
            var detailDto = new ImportDetailDto { ImportId = importId, Status = ImportStatus.Confirmed };

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);
            _mockMapper.Setup(x => x.Map<ImportDetailDto>(import))
                .Returns(detailDto);

            // Act
            var result = await _importService.GetImportDetailAsync(userId, importId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(importId, result.ImportId);
            Assert.Equal(ImportStatus.Confirmed, result.Status);
        }

        [Fact]
        public async Task GetImportDetailAsync_WithNonExistentId_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 999L;

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync((Import?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _importService.GetImportDetailAsync(userId, importId));
        }

        #endregion

        // =========================================================
        // 7. DeleteImportAsync
        // =========================================================

        #region DeleteImportAsync Tests

        [Fact]
        public async Task DeleteImportAsync_WithDraftImport_HardDeletes()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var import = BuildDraftImport(importId);

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);

            // Act
            await _importService.DeleteImportAsync(userId, importId);

            // Assert — DRAFT → hard delete
            _mockUnitOfWork.Verify(x => x.Imports.Delete(import), Times.Once);
            _mockUnitOfWork.Verify(x => x.Imports.Update(It.IsAny<Import>()), Times.Never);
        }

        [Fact]
        public async Task DeleteImportAsync_WithConfirmedImport_ReversesStockAndCancels()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var product = new Product { ProductId = 10, Stock = 20, Unit = "kg" };
            var import = BuildConfirmedImport(importId);
            import.ProductsImports = new List<ProductImport>
            {
                new()
                {
                    ImportId = importId,
                    ProductId = 10,
                    Quantity = 5,
                    CostPrice = 100m,
                    TotalPrice = 500m,
                    BaseUnit = "kg",
                    Product = product
                }
            };

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);

            // Act
            await _importService.DeleteImportAsync(userId, importId);

            // Assert — stock reversed: 20 - 5 = 15
            Assert.Equal(15, product.Stock);
            Assert.Equal(ImportStatus.Cancelled, import.Status);
            _mockUnitOfWork.Verify(x => x.Imports.Update(import), Times.Once);
            _mockUnitOfWork.Verify(x => x.Imports.Delete(It.IsAny<Import>()), Times.Never);
        }

        [Fact]
        public async Task DeleteImportAsync_WhenCancellingWouldResultInNegativeStock_ThrowsBadRequestException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var product = new Product { ProductId = 10, Stock = 2, Unit = "kg" };  // Stock < Quantity
            var import = BuildConfirmedImport(importId);
            import.ProductsImports = new List<ProductImport>
            {
                new()
                {
                    ImportId = importId,
                    ProductId = 10,
                    Quantity = 10,  // More than current stock
                    CostPrice = 100m,
                    TotalPrice = 1000m,
                    BaseUnit = "kg",
                    Product = product
                }
            };

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _importService.DeleteImportAsync(userId, importId));
        }

        [Fact]
        public async Task DeleteImportAsync_WithNonExistentImport_ThrowsNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 999L;

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync((Import?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _importService.DeleteImportAsync(userId, importId));
        }

        [Fact]
        public async Task DeleteImportAsync_WithCancelledImport_ThrowsBadRequestException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var import = BuildCancelledImport(importId);

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId))
                .ReturnsAsync(import);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _importService.DeleteImportAsync(userId, importId));
        }

        #endregion

        // =========================================================
        // Additional edge-case tests
        // =========================================================

        #region UpdateImportAsync - Additional Edge Cases

        [Fact]
        public async Task UpdateImportAsync_OverwritesAllFields_Correctly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var import = BuildDraftImport(importId);
            var receivedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

            var request = new UpdateImportRequest
            {
                ImportType = "RETURN",
                Supplier = "Supplier XYZ",
                Note = "Some note",
                ReceivedAt = receivedAt,
                Items = new List<ImportItemRequest>()
            };

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId)).ReturnsAsync(import);
            _mockMapper.Setup(x => x.Map<ImportSummaryDto>(import)).Returns(new ImportSummaryDto { ImportId = importId });

            // Act
            await _importService.UpdateImportAsync(userId, importId, request);

            // Assert — all fields overwritten
            Assert.Equal("RETURN", import.ImportType);
            Assert.Equal("Supplier XYZ", import.Supplier);
            Assert.Equal("Some note", import.Note);
            Assert.Equal(receivedAt, import.ReceivedAt);
        }

        [Fact]
        public async Task UpdateImportAsync_CallsDeleteItemForEachOldItem()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 1L;
            var import = BuildDraftImport(importId);

            var oldItem1 = new ProductImport { ImportId = importId, ProductId = 5, Quantity = 2, CostPrice = 50m, TotalPrice = 100m, BaseUnit = "kg" };
            var oldItem2 = new ProductImport { ImportId = importId, ProductId = 6, Quantity = 1, CostPrice = 80m, TotalPrice = 80m,  BaseUnit = "pcs" };
            import.ProductsImports = new List<ProductImport> { oldItem1, oldItem2 };

            var request = new UpdateImportRequest { Items = new List<ImportItemRequest>() };

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId)).ReturnsAsync(import);
            _mockMapper.Setup(x => x.Map<ImportSummaryDto>(import)).Returns(new ImportSummaryDto());

            // Act
            await _importService.UpdateImportAsync(userId, importId, request);

            // Assert — DeleteItem called once per old item
            _mockUnitOfWork.Verify(x => x.Imports.DeleteItem(oldItem1), Times.Once);
            _mockUnitOfWork.Verify(x => x.Imports.DeleteItem(oldItem2), Times.Once);
        }

        #endregion

        #region CreateImportAsync - Empty Items Draft

        [Fact]
        public async Task CreateImportAsync_AsDraftWithNoItems_SucceedsWithZeroTotal()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;

            var request = new CreateImportRequest
            {
                ImportType = "INVENTORY_ADJUSTMENT",
                BusinessLocationId = locationId,
                SaveAsDraft = true,
                Items = new List<ImportItemRequest>()  // no items
            };

            var location = new BusinessLocation { BusinessLocationId = locationId, Name = "Store" };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId)).ReturnsAsync(location);

            Import? captured = null;
            _mockUnitOfWork.Setup(x => x.Imports.AddAsync(It.IsAny<Import>()))
                .Callback<Import>(imp => captured = imp)
                .ReturnsAsync((Import imp) => imp);

            _mockMapper.Setup(x => x.Map<ImportSummaryDto>(It.IsAny<Import>()))
                .Returns(new ImportSummaryDto { ImportId = 1, Status = ImportStatus.Draft });

            // Act
            var result = await _importService.CreateImportAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(captured);
            Assert.Equal(0m, captured!.TotalAmount);
            Assert.Equal(ImportStatus.Draft, captured.Status);
        }

        #endregion

        #region PatchImportAsync - Result DTO correctness

        [Fact]
        public async Task PatchImportAsync_ResultDto_ContainsCorrectImportIdCodeAndStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var importId = 42L;
            var receivedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc);

            var product = new Product { ProductId = 1, Stock = 10, Unit = "kg" };
            var import = BuildDraftImport(importId);
            import.ImportCode = "ABCDEF123456";
            import.ProductsImports = new List<ProductImport>
            {
                new()
                {
                    ImportId = importId, ProductId = 1,
                    Quantity = 2, CostPrice = 50m, TotalPrice = 100m,
                    BaseUnit = "kg", Product = product
                }
            };

            _mockUnitOfWork.Setup(x => x.Imports.GetByIdWithItemsAsync(importId)).ReturnsAsync(import);

            // Act
            var result = await _importService.PatchImportAsync(userId, importId, new PatchImportRequest { ReceivedAt = receivedAt });

            // Assert — result DTO mirrors the updated import
            Assert.Equal(importId, result.ImportId);
            Assert.Equal("ABCDEF123456", result.ImportCode);
            Assert.Equal(ImportStatus.Confirmed, result.Status);
            Assert.Equal(receivedAt, result.ReceivedAt);
            Assert.NotNull(result.UpdatedAt);
        }

        #endregion
    }
}
