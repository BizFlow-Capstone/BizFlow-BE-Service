using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Cost;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class CostServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBusinessLocationService> _locationService = new();
    private readonly Mock<IImageService> _imageService = new();
    private readonly Mock<IGeneralLedgerService> _generalLedgerService = new();
    private readonly Mock<ICostRepository> _costRepo = new();
    private readonly IMapper _mapper;

    public CostServiceTests()
    {
        var cfg = new MapperConfiguration(c => c.CreateProfile("Test", p => p.CreateMap<Cost, CostDto>()), NullLoggerFactory.Instance);
        _mapper = cfg.CreateMapper();

        _uow.SetupGet(x => x.Costs).Returns(_costRepo.Object);
    }

    [Fact]
    public async Task CreateManualAsync_ShouldCreateCost_AndRecordGL()
    {
        var userId = Guid.NewGuid();
        var request = new CreateManualCostRequest
        {
            BusinessLocationId = 1,
            CostType = CostType.Manual,
            Description = "Office supplies",
            Amount = 500000,
            PaymentMethod = "cash"
        };

        Cost? added = null;
        _costRepo.Setup(r => r.AddAsync(It.IsAny<Cost>()))
            .Callback<Cost>(c =>
            {
                c.CostId = 123;
                added = c;
            })
            .ReturnsAsync((Cost c) => c);

        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        var result = await sut.CreateManualAsync(userId, request);

        Assert.NotNull(added);
        Assert.Equal(123, result.CostId);
        _generalLedgerService.Verify(g => g.RecordManualCostAsync(It.Is<Cost>(c => c.CostId == 123)), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task DeleteManualAsync_ShouldSoftDelete_AndReverseGL()
    {
        var userId = Guid.NewGuid();
        var cost = new Cost
        {
            CostId = 9,
            BusinessLocationId = 5,
            CostType = CostType.Manual,
            Amount = 100,
            Description = "Manual cost",
            CostDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _costRepo.Setup(r => r.GetByIdAsync(cost.CostId)).ReturnsAsync(cost);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        await sut.DeleteManualAsync(userId, cost.CostId);

        Assert.NotNull(cost.DeletedAt);
        _generalLedgerService.Verify(g => g.ReverseCostEntriesAsync(cost, MessageKeys.ManualCostDeletedReversalReason), Times.Once);
    }

    [Fact]
    public async Task CreateManualAsync_WhenOwnerValidationFails_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var request = new CreateManualCostRequest
        {
            BusinessLocationId = 7,
            CostType = CostType.Manual,
            Description = "Not allowed",
            Amount = 1000,
            PaymentMethod = "bank"
        };

        _locationService
            .Setup(s => s.ValidateOwnerAsync(userId, request.BusinessLocationId))
            .ThrowsAsync(new ForbiddenException("COMMON_FORBIDDEN"));

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.CreateManualAsync(userId, request));

        _costRepo.Verify(r => r.AddAsync(It.IsAny<Cost>()), Times.Never);
        _generalLedgerService.Verify(g => g.RecordManualCostAsync(It.IsAny<Cost>()), Times.Never);
    }

    [Fact]
    public async Task DeleteManualAsync_WhenImportCost_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var cost = new Cost
        {
            CostId = 10,
            BusinessLocationId = 5,
            CostType = CostType.Import,
            Amount = 999,
            Description = "Import",
            CostDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _costRepo.Setup(r => r.GetByIdAsync(cost.CostId)).ReturnsAsync(cost);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.DeleteManualAsync(userId, cost.CostId));

        _generalLedgerService.Verify(g => g.ReverseCostEntriesAsync(It.IsAny<Cost>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteManualAsync_WhenCostNotFound_ShouldThrowNotFound()
    {
        var userId = Guid.NewGuid();
        _costRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Cost?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.DeleteManualAsync(userId, 404));
    }

    [Fact]
    public async Task CreateManualAsync_WhenPaymentMethodInvalid_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var request = new CreateManualCostRequest
        {
            BusinessLocationId = 1,
            CostType = CostType.Manual,
            Description = "Invalid payment",
            Amount = 2000,
            PaymentMethod = "wire_transfer_unknown"
        };

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateManualAsync(userId, request));
        _costRepo.Verify(r => r.AddAsync(It.IsAny<Cost>()), Times.Never);
    }

    private CostService BuildSut()
    {
        return new CostService(
            _uow.Object,
            _mapper,
            _locationService.Object,
            _imageService.Object,
            _generalLedgerService.Object);
    }
}
