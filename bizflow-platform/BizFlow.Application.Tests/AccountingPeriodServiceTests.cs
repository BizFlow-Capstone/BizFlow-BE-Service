using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class AccountingPeriodServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBusinessLocationRepository> _locationRepo = new();
    private readonly Mock<IAccountingPeriodRepository> _periodRepo = new();

    public AccountingPeriodServiceTests()
    {
        _uow.SetupGet(x => x.BusinessLocations).Returns(_locationRepo.Object);
        _uow.SetupGet(x => x.AccountingPeriods).Returns(_periodRepo.Object);
    }

    private AccountingPeriodService BuildSut() => new(_uow.Object);

    // ═══════════════════════════════════════════════════
    // ENSURE OWNER ACCESS (shared guard)
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CreatePeriodAsync_WhenLocationNotFound_ShouldThrowNotFound()
    {
        _locationRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((BizFlow.Domain.Entities.BusinessLocation?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.CreatePeriodAsync(999, Guid.NewGuid(),
            new CreateAccountingPeriodRequest { PeriodType = "quarter", Year = 2025, Quarter = 1 }));
    }

    [Fact]
    public async Task CreatePeriodAsync_WhenNotOwner_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(false);

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.CreatePeriodAsync(1, userId,
            new CreateAccountingPeriodRequest { PeriodType = "quarter", Year = 2025, Quarter = 1 }));
    }

    // ═══════════════════════════════════════════════════
    // CREATE STANDARD PERIOD
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CreatePeriodAsync_WhenInvalidPeriodType_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreatePeriodAsync(1, userId,
            new CreateAccountingPeriodRequest { PeriodType = "invalid_type", Year = 2025, Quarter = 1 }));
    }

    [Fact]
    public async Task CreatePeriodAsync_WhenYearly_WithQuarterProvided_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);

        var sut = BuildSut();

        // Year type should not have a Quarter value
        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreatePeriodAsync(1, userId,
            new CreateAccountingPeriodRequest { PeriodType = "year", Year = 2025, Quarter = 1 }));
    }

    [Fact]
    public async Task CreatePeriodAsync_WhenAlreadyExists_ShouldThrowConflict()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _periodRepo.Setup(r => r.ExistsAsync(1, "quarter", 2025, 1)).ReturnsAsync(true);

        var sut = BuildSut();

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreatePeriodAsync(1, userId,
            new CreateAccountingPeriodRequest { PeriodType = "quarter", Year = 2025, Quarter = 1 }));
    }

    // ═══════════════════════════════════════════════════
    // CREATE CUSTOM PERIOD
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CreateCustomPeriodAsync_WhenEndDateBeforeStartDate_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 2 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 2)).ReturnsAsync(true);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateCustomPeriodAsync(2, userId,
            new CreateCustomAccountingPeriodRequest
            {
                StartDate = new DateOnly(2025, 1, 31),
                EndDate = new DateOnly(2025, 1, 1) // EndDate earlier than StartDate
            }));
    }

    // ═══════════════════════════════════════════════════
    // FINALIZE PERIOD
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task FinalizePeriodAsync_WhenNotFound_ShouldThrowNotFound()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(1, 999)).ReturnsAsync((AccountingPeriod?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.FinalizePeriodAsync(1, 999, userId));
    }

    [Fact]
    public async Task FinalizePeriodAsync_WhenAlreadyFinalized_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var period = new AccountingPeriod
        {
            PeriodId = 10, Status = AccountingPeriodConstants.PeriodStatuses.Finalized
        };
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(1, 10)).ReturnsAsync(period);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.FinalizePeriodAsync(1, 10, userId));
        Assert.Equal(MessageKeys.PeriodAlreadyFinalized, ex.MessageKey);
    }

    [Fact]
    public async Task FinalizePeriodAsync_WhenNoBooksExist_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var period = new AccountingPeriod
        {
            PeriodId = 11, Status = AccountingPeriodConstants.PeriodStatuses.Open
        };
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(1, 11)).ReturnsAsync(period);
        _periodRepo.Setup(r => r.CountActiveBooksAsync(11)).ReturnsAsync(0);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.FinalizePeriodAsync(1, 11, userId));
        Assert.Equal(MessageKeys.PeriodNoBooks, ex.MessageKey);
    }

    // ═══════════════════════════════════════════════════
    // REOPEN PERIOD
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task ReopenPeriodAsync_WhenReasonEmpty_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.ReopenPeriodAsync(1, 12, userId, "  "));
    }

    [Fact]
    public async Task ReopenPeriodAsync_WhenNotFinalized_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var period = new AccountingPeriod
        {
            PeriodId = 13, Status = AccountingPeriodConstants.PeriodStatuses.Open
        };
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(1, 13)).ReturnsAsync(period);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.ReopenPeriodAsync(1, 13, userId, "Valid reason"));
        Assert.Equal(MessageKeys.PeriodNotFinalized, ex.MessageKey);
    }

    // ═══════════════════════════════════════════════════
    // DELETE PERIOD
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task DeletePeriodAsync_WhenPeriodNotFound_ShouldThrowNotFound()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(1, 999)).ReturnsAsync((AccountingPeriod?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.DeletePeriodAsync(1, 999, userId));
    }

    [Fact]
    public async Task DeletePeriodAsync_WhenNotOpenStatus_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var period = new AccountingPeriod
        {
            PeriodId = 20, Status = AccountingPeriodConstants.PeriodStatuses.Finalized
        };
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(1, 20)).ReturnsAsync(period);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.DeletePeriodAsync(1, 20, userId));
    }

    [Fact]
    public async Task DeletePeriodAsync_WhenHasBooks_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var period = new AccountingPeriod
        {
            PeriodId = 21, Status = AccountingPeriodConstants.PeriodStatuses.Open
        };
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(1, 21)).ReturnsAsync(period);
        _periodRepo.Setup(r => r.CountActiveBooksAsync(21)).ReturnsAsync(3);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.DeletePeriodAsync(1, 21, userId));
    }

    // ═══════════════════════════════════════════════════
    // GET PERIOD DETAIL
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task GetPeriodDetailAsync_WhenNotFound_ShouldThrowNotFound()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(1, 777)).ReturnsAsync((AccountingPeriod?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetPeriodDetailAsync(1, 777, userId));
    }

    [Fact]
    public async Task GetPeriodsAsync_WhenOwner_ShouldReturnAllPeriods()
    {
        var userId = Guid.NewGuid();
        _locationRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BizFlow.Domain.Entities.BusinessLocation { BusinessLocationId = 1 });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, 1)).ReturnsAsync(true);
        _periodRepo.Setup(r => r.GetByLocationAsync(1)).ReturnsAsync(new List<AccountingPeriod>
        {
            new() { PeriodId = 1, PeriodType = "quarter", Year = 2025, Status = "open" },
            new() { PeriodId = 2, PeriodType = "year", Year = 2024, Status = "finalized" }
        });

        var sut = BuildSut();
        var result = await sut.GetPeriodsAsync(1, userId);

        Assert.Equal(2, result.Count);
    }
}
