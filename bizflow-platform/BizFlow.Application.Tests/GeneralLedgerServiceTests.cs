using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using AutoMapper;
using BizFlow.Application.Mappers;
using BizFlow.Application.Common.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class GeneralLedgerServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBusinessLocationService> _locationService = new();
    private readonly Mock<IBusinessLocationRepository> _locationRepo = new();
    private readonly Mock<IGeneralLedgerRepository> _glRepo = new();
    private readonly Mock<ICostRepository> _costRepo = new();
    private readonly Mock<IRevenueRepository> _revenueRepo = new();
    private readonly Mock<BizFlow.Application.Common.Interfaces.IMessageService> _messageService = new();
    private readonly IMapper _mapper;

    public GeneralLedgerServiceTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<GeneralLedgerProfile>(), NullLoggerFactory.Instance);
        _mapper = cfg.CreateMapper();

        _uow.SetupGet(x => x.BusinessLocations).Returns(_locationRepo.Object);
        _uow.SetupGet(x => x.GeneralLedgerEntries).Returns(_glRepo.Object);
        _uow.SetupGet(x => x.Costs).Returns(_costRepo.Object);
        _uow.SetupGet(x => x.Revenues).Returns(_revenueRepo.Object);

        _messageService.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] _) => key);
        _messageService.Setup(m => m.GetMessage(It.IsAny<string>()))
            .Returns((string key) => key);
    }

    private GeneralLedgerService BuildSut(int lookbackDays = 365)
    {
        var settings = Options.Create(new GeneralLedgerSettings
        {
            LookbackValue = lookbackDays,
            LookbackUnit = "day"
        });

        return new GeneralLedgerService(
            _uow.Object,
            _locationService.Object,
            _mapper,
            _messageService.Object,
            settings);
    }

    // ═══════════════════════════════════════════════════
    // LIST LEDGER ENTRIES
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task ListAsync_WhenNotOwner_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1))
            .ThrowsAsync(new ForbiddenException("COMMON_FORBIDDEN"));

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.ListAsync(userId,
            new GeneralLedgerQueryParams { BusinessLocationId = 1 }));
    }

    [Fact]
    public async Task ListAsync_WhenInvalidViewMode_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.ListAsync(userId,
            new GeneralLedgerQueryParams { BusinessLocationId = 1, ViewMode = "bad_mode" }));
        Assert.Equal(MessageKeys.LedgerInvalidViewMode, ex.MessageKey);
    }

    [Fact]
    public async Task ListAsync_WhenInvalidTransactionType_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.ListAsync(userId,
            new GeneralLedgerQueryParams
            {
                BusinessLocationId = 1,
                TransactionTypes = new List<string> { "invalid_tx_type" }
            }));
        Assert.Equal(MessageKeys.LedgerInvalidTransactionType, ex.MessageKey);
    }

    [Fact]
    public async Task ListAsync_WhenInvalidReferenceType_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.ListAsync(userId,
            new GeneralLedgerQueryParams
            {
                BusinessLocationId = 1,
                ReferenceTypes = new List<string> { "bad_ref" }
            }));
        Assert.Equal(MessageKeys.LedgerInvalidReferenceType, ex.MessageKey);
    }

    [Fact]
    public async Task ListAsync_WhenInvalidMoneyChannel_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.ListAsync(userId,
            new GeneralLedgerQueryParams
            {
                BusinessLocationId = 1,
                MoneyChannels = new List<string> { "bad_channel" }
            }));
        Assert.Equal(MessageKeys.LedgerInvalidMoneyChannel, ex.MessageKey);
    }

    [Fact]
    public async Task ListAsync_WhenFromDateAfterToDate_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);

        var sut = BuildSut();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.ListAsync(userId,
            new GeneralLedgerQueryParams
            {
                BusinessLocationId = 1,
                FromDate = today,
                ToDate = today.AddDays(-1) // ToDate before FromDate
            }));
        Assert.Equal(MessageKeys.LedgerInvalidDateRange, ex.MessageKey);
    }

    [Fact]
    public async Task ListAsync_WhenValidQuery_ShouldReturnPagedResults()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);
        _glRepo.Setup(r => r.SearchAsync(It.IsAny<GeneralLedgerQueryParams>()))
            .ReturnsAsync((new List<GeneralLedgerEntry>(), 0));
        _glRepo.Setup(r => r.GetReversalSummaryAsOfAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new Dictionary<long, (int, long?)>());
        _costRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new List<Cost>());
        _revenueRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new List<Revenue>());

        var sut = BuildSut();
        var result = await sut.ListAsync(userId, new GeneralLedgerQueryParams
        {
            BusinessLocationId = 1, PageNumber = 1, PageSize = 20
        });

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
