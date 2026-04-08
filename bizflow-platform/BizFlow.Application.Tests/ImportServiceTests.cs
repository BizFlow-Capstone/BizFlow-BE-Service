using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Import;
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

public class ImportServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IImportSchemaRepository> _schemaRepo = new();
    private readonly Mock<IImportRepository> _importRepo = new();
    private readonly Mock<IBusinessLocationRepository> _locationRepo = new();
    private readonly Mock<IImageService> _imageService = new();
    private readonly Mock<IStockMovementService> _stockMovementService = new();
    private readonly Mock<ICostService> _costService = new();
    private readonly Mock<IBackgroundJobScheduler> _bgScheduler = new();
    private readonly IMapper _mapper;

    public ImportServiceTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<ImportProfile>(), NullLoggerFactory.Instance);
        _mapper = cfg.CreateMapper();

        _uow.SetupGet(x => x.ImportSchemas).Returns(_schemaRepo.Object);
        _uow.SetupGet(x => x.Imports).Returns(_importRepo.Object);
        _uow.SetupGet(x => x.BusinessLocations).Returns(_locationRepo.Object);
        _uow.Setup(x => x.ExecuteResilientAsync(It.IsAny<Func<CancellationToken, Task<Import>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Import>> action, CancellationToken ct) => action(ct));
        _uow.Setup(x => x.ExecuteResilientAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> action, CancellationToken ct) => action(ct));
    }

    private ImportService BuildSut() => new(
        _uow.Object,
        _imageService.Object,
        _stockMovementService.Object,
        _costService.Object,
        _bgScheduler.Object,
        _mapper);

    [Fact]
    public async Task GetTemplateAsync_WhenNoActiveSchema_ShouldThrowNotFound()
    {
        _schemaRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync((ImportSchema?)null);
        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetTemplateAsync());
    }

    [Fact]
    public async Task GetTemplateAsync_WhenNoActiveVersion_ShouldThrowNotFound()
    {
        _schemaRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new ImportSchema
        {
            ImportSchemaVersions = new List<ImportSchemaVersion>
            {
                new() { IsActive = false, SchemaJson = "{}" }
            }
        });

        var sut = BuildSut();
        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetTemplateAsync());
    }

    [Fact]
    public async Task GetTemplateAsync_WhenFound_ShouldReturnSchemaJson()
    {
        _schemaRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new ImportSchema
        {
            ImportSchemaVersions = new List<ImportSchemaVersion>
            {
                new() { IsActive = true, SchemaJson = "{\"ok\":true}" }
            }
        });

        var sut = BuildSut();
        var result = await sut.GetTemplateAsync();

        Assert.Equal("{\"ok\":true}", result.SchemaJson);
    }

    [Fact]
    public async Task CreateImportAsync_WhenImportTypeInvalid_ShouldThrowBadRequest()
    {
        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateImportAsync(Guid.NewGuid(), new CreateImportRequest
        {
            ImportType = "wrong",
            BusinessLocationId = 1,
            Items = new List<ImportItemRequest>()
        }));
    }

    [Fact]
    public async Task CreateImportAsync_WhenLocationNotFound_ShouldThrowNotFound()
    {
        _locationRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((BusinessLocation?)null);
        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => sut.CreateImportAsync(Guid.NewGuid(), new CreateImportRequest
        {
            ImportType = ImportType.Invoice,
            BusinessLocationId = 1,
            Items = new List<ImportItemRequest>()
        }));
        Assert.Equal(MessageKeys.ImportLocationNotFound, ex.MessageKey);
    }

    [Fact]
    public async Task CreateImportAsync_WhenConfirmWithoutReceivedAt_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new BusinessLocation { BusinessLocationId = 1, LocationName = "A", Address = "B", Status = "active" });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateImportAsync(userId, new CreateImportRequest
        {
            ImportType = ImportType.Invoice,
            BusinessLocationId = 1,
            SaveAsDraft = false,
            ReceivedAt = null,
            Items = new List<ImportItemRequest>()
        }));
    }

    [Fact]
    public async Task ListImportsAsync_WhenLocationMissing_ShouldThrowBadRequest()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() => sut.ListImportsAsync(Guid.NewGuid(), new ImportQueryParams()));
    }

    [Fact]
    public async Task ListImportsAsync_WhenInvalidStatus_ShouldThrowBadRequest()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() => sut.ListImportsAsync(Guid.NewGuid(), new ImportQueryParams
        {
            BusinessLocationId = 1,
            Status = "bad_status"
        }));
    }

    [Fact]
    public async Task ListImportsAsync_WhenInvalidImportType_ShouldThrowBadRequest()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() => sut.ListImportsAsync(Guid.NewGuid(), new ImportQueryParams
        {
            BusinessLocationId = 1,
            ImportType = "bad_type"
        }));
    }

    [Fact]
    public async Task ListImportsAsync_WhenValid_ShouldNormalizeAndReturnItems()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new BusinessLocation { BusinessLocationId = 2, LocationName = "loc", Address = "a", Status = "active" });
        _locationRepo.Setup(r => r.HasAccessToLocationAsync(userId, 2)).ReturnsAsync(true);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 2)).ReturnsAsync(true);
        _importRepo.Setup(r => r.SearchAsync(It.IsAny<ImportQueryParams>()))
            .ReturnsAsync((new List<Import> { new() { ImportId = 1, BusinessLocationId = 2, Status = ImportStatus.Draft, ImportType = ImportType.Invoice } }, 1));

        var query = new ImportQueryParams { BusinessLocationId = 2, Status = " draft ", ImportType = " invoice " };
        var sut = BuildSut();
        var result = await sut.ListImportsAsync(userId, query);

        Assert.Single(result.Items);
        Assert.Equal("DRAFT", query.Status);
        Assert.Equal("INVOICE", query.ImportType);
    }

    [Fact]
    public async Task GetImportDetailAsync_WhenNotFound_ShouldThrowNotFound()
    {
        _importRepo.Setup(r => r.GetByIdWithItemsAsync(10)).ReturnsAsync((Import?)null);
        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetImportDetailAsync(Guid.NewGuid(), 10));
    }

    [Fact]
    public async Task DeleteImportAsync_WhenNotFound_ShouldThrowNotFound()
    {
        _importRepo.Setup(r => r.GetByIdWithItemsAsync(11)).ReturnsAsync((Import?)null);
        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.DeleteImportAsync(Guid.NewGuid(), 11));
    }

    [Fact]
    public async Task DeleteImportAsync_WhenAlreadyCancelled_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _importRepo.Setup(r => r.GetByIdWithItemsAsync(12)).ReturnsAsync(new Import
        {
            ImportId = 12,
            BusinessLocationId = 3,
            Status = ImportStatus.Cancelled,
            ProductsImports = new List<ProductImport>()
        });
        _locationRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new BusinessLocation { BusinessLocationId = 3, LocationName = "l", Address = "a", Status = "active" });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 3)).ReturnsAsync(true);

        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() => sut.DeleteImportAsync(userId, 12));
    }

    [Fact]
    public async Task DeleteImportAsync_WhenDraft_ShouldHardDelete()
    {
        var userId = Guid.NewGuid();
        var import = new Import
        {
            ImportId = 13,
            BusinessLocationId = 3,
            Status = ImportStatus.Draft,
            ProductsImports = new List<ProductImport>()
        };
        _importRepo.Setup(r => r.GetByIdWithItemsAsync(13)).ReturnsAsync(import);
        _locationRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new BusinessLocation { BusinessLocationId = 3, LocationName = "l", Address = "a", Status = "active" });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 3)).ReturnsAsync(true);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        await sut.DeleteImportAsync(userId, 13);

        _importRepo.Verify(r => r.Delete(import), Times.Once);
    }
}
