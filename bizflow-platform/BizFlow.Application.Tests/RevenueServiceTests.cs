using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Revenue;
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

public class RevenueServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRevenueRepository> _revenueRepo = new();
    private readonly Mock<IBusinessLocationService> _locationService = new();
    private readonly Mock<IGeneralLedgerService> _generalLedgerService = new();
    private readonly IMapper _mapper;

    public RevenueServiceTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<RevenueProfile>(), NullLoggerFactory.Instance);
        _mapper = cfg.CreateMapper();

        _uow.SetupGet(x => x.Revenues).Returns(_revenueRepo.Object);
        _uow.Setup(x => x.ExecuteResilientAsync(It.IsAny<Func<CancellationToken, Task<Revenue>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Revenue>> action, CancellationToken ct) => action(ct));
        _uow.Setup(x => x.ExecuteResilientAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> action, CancellationToken ct) => action(ct));
    }

    private RevenueService BuildSut() => new(_uow.Object, _mapper, _locationService.Object, _generalLedgerService.Object);

    [Fact]
    public async Task CreateManualAsync_WhenValid_ShouldCreateAndRecordGl()
    {
        var userId = Guid.NewGuid();
        var request = new CreateManualRevenueRequest
        {
            BusinessLocationId = 1,
            BusinessTypeId = Guid.NewGuid(),
            Amount = 300000,
            Description = "  Thu khac  ",
            MoneyChannel = PaymentMethods.Cash,
            DocumentNumber = "  CT-001  "
        };

        _locationService.Setup(s => s.ValidateOwnerAsync(userId, request.BusinessLocationId)).Returns(Task.CompletedTask);
        _revenueRepo.Setup(r => r.AddAsync(It.IsAny<Revenue>())).ReturnsAsync((Revenue r) => r);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        var result = await sut.CreateManualAsync(userId, request);

        Assert.Equal(request.Amount, result.Amount);
        _generalLedgerService.Verify(g => g.RecordManualRevenueAsync(It.Is<Revenue>(r =>
            r.Description == "Thu khac" &&
            r.MoneyChannel == PaymentMethods.Cash &&
            r.DocumentNumber == "CT-001")), Times.Once);
    }

    [Fact]
    public async Task CreateManualAsync_WhenBusinessTypeMissing_ShouldThrowBadRequest()
    {
        var request = new CreateManualRevenueRequest
        {
            BusinessLocationId = 1,
            BusinessTypeId = null,
            Amount = 100,
            Description = "abc",
            MoneyChannel = PaymentMethods.Cash
        };

        var sut = BuildSut();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateManualAsync(Guid.NewGuid(), request));
        Assert.Equal(MessageKeys.BadRequest, ex.MessageKey);
    }

    [Fact]
    public async Task CreateManualAsync_WhenMoneyChannelInvalid_ShouldThrowBadRequest()
    {
        var request = new CreateManualRevenueRequest
        {
            BusinessLocationId = 1,
            BusinessTypeId = Guid.NewGuid(),
            Amount = 100,
            Description = "abc",
            MoneyChannel = "invalid_channel"
        };

        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateManualAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task UpdateManualAsync_WhenRevenueNotFound_ShouldThrowNotFound()
    {
        _revenueRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Revenue?)null);
        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdateManualAsync(Guid.NewGuid(), 99, new UpdateManualRevenueRequest
        {
            BusinessTypeId = Guid.NewGuid(),
            Amount = 10,
            Description = "x",
            MoneyChannel = PaymentMethods.Cash
        }));
    }

    [Fact]
    public async Task UpdateManualAsync_WhenTypeNotManual_ShouldThrowBadRequest()
    {
        var revenue = new Revenue { RevenueId = 1, BusinessLocationId = 2, RevenueType = RevenueType.Sale };
        _revenueRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(revenue);

        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() => sut.UpdateManualAsync(Guid.NewGuid(), 1, new UpdateManualRevenueRequest
        {
            BusinessTypeId = Guid.NewGuid(),
            Amount = 10,
            Description = "x",
            MoneyChannel = PaymentMethods.Cash
        }));
    }

    [Fact]
    public async Task UpdateManualAsync_WhenValid_ShouldUpdateAndReverseThenRecordGl()
    {
        var userId = Guid.NewGuid();
        var revenue = new Revenue
        {
            RevenueId = 5,
            BusinessLocationId = 2,
            RevenueType = RevenueType.Manual,
            Amount = 500,
            Description = "old",
            MoneyChannel = PaymentMethods.Cash
        };
        _revenueRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(revenue);
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 2)).Returns(Task.CompletedTask);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        var result = await sut.UpdateManualAsync(userId, 5, new UpdateManualRevenueRequest
        {
            BusinessTypeId = Guid.NewGuid(),
            Amount = 900,
            Description = "  new desc ",
            MoneyChannel = "bank",
            DocumentNumber = "  D01  "
        });

        Assert.Equal(900, result.Amount);
        Assert.Equal("new desc", revenue.Description);
        Assert.Equal(PaymentMethods.Bank, revenue.MoneyChannel);
        Assert.Equal("D01", revenue.DocumentNumber);
        _generalLedgerService.Verify(g => g.ReverseRevenueEntriesAsync(revenue, MessageKeys.ManualRevenueUpdatedReversalReason), Times.Once);
        _generalLedgerService.Verify(g => g.RecordManualRevenueAsync(revenue), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WhenInvalidRevenueType_ShouldThrowBadRequest()
    {
        var query = new RevenueQueryParams { BusinessLocationId = 1, RevenueType = "invalid_type" };
        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.ListAsync(Guid.NewGuid(), query));
    }

    [Fact]
    public async Task ListAsync_WhenValid_ShouldNormalizeFiltersAndReturnPaged()
    {
        var userId = Guid.NewGuid();
        var query = new RevenueQueryParams
        {
            BusinessLocationId = 1,
            RevenueType = "  manual ",
            MoneyChannel = "  CASH  ",
            PageNumber = 2,
            PageSize = 3
        };

        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);
        _revenueRepo.Setup(r => r.SearchAsync(It.IsAny<RevenueQueryParams>()))
            .ReturnsAsync((new List<Revenue>
            {
                new() { RevenueId = 1, BusinessLocationId = 1, RevenueType = RevenueType.Manual, Amount = 10, Description = "a", MoneyChannel = PaymentMethods.Cash }
            }, 1));

        var sut = BuildSut();
        var result = await sut.ListAsync(userId, query);

        Assert.Single(result.Items);
        Assert.Equal("manual", query.RevenueType);
        Assert.Equal("cash", query.MoneyChannel);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(3, result.PageSize);
    }

    [Fact]
    public async Task DeleteManualAsync_WhenNotFound_ShouldThrowNotFound()
    {
        _revenueRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Revenue?)null);
        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.DeleteManualAsync(Guid.NewGuid(), 1));
    }

    [Fact]
    public async Task DeleteManualAsync_WhenNotManual_ShouldThrowBadRequest()
    {
        var revenue = new Revenue { RevenueId = 3, BusinessLocationId = 1, RevenueType = RevenueType.Sale };
        _revenueRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(revenue);
        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.DeleteManualAsync(Guid.NewGuid(), 3));
    }

    [Fact]
    public async Task DeleteManualAsync_WhenValid_ShouldSoftDeleteAndReverseGl()
    {
        var userId = Guid.NewGuid();
        var revenue = new Revenue { RevenueId = 4, BusinessLocationId = 9, RevenueType = RevenueType.Manual };
        _revenueRepo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(revenue);
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 9)).Returns(Task.CompletedTask);

        var sut = BuildSut();
        await sut.DeleteManualAsync(userId, 4);

        Assert.NotNull(revenue.DeletedAt);
        _revenueRepo.Verify(r => r.Update(revenue), Times.Once);
        _generalLedgerService.Verify(g => g.ReverseRevenueEntriesAsync(revenue, MessageKeys.ManualRevenueDeletedReversalReason), Times.Once);
    }
}
