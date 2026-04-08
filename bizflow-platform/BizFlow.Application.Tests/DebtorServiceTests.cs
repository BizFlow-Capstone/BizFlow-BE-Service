using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Debtor;
using BizFlow.Application.DTOs.Location;
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

public class DebtorServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDebtorRepository> _debtorRepo = new();
    private readonly Mock<IBusinessLocationService> _locationService = new();
    private readonly Mock<IGeneralLedgerService> _glService = new();
    private readonly IMapper _mapper;

    public DebtorServiceTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<DebtorProfile>(), NullLoggerFactory.Instance);
        _mapper = cfg.CreateMapper();

        _uow.SetupGet(x => x.Debtors).Returns(_debtorRepo.Object);
    }

    private DebtorService BuildSut() => new(_uow.Object, _mapper, _locationService.Object, _glService.Object);

    [Fact]
    public async Task ListAsync_WhenNoOwnedLocation_ShouldReturnEmptyPage()
    {
        var userId = Guid.NewGuid();
        var query = new DebtorQueryParams { PageNumber = 1, PageSize = 20 };
        _locationService.Setup(s => s.GetOwnedLocationsAsync(userId)).ReturnsAsync(Array.Empty<BusinessLocationDto>());

        var sut = BuildSut();
        var result = await sut.ListAsync(userId, query);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task ListAsync_WhenContainsUnauthorizedLocation_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.GetOwnedLocationsAsync(userId))
            .ReturnsAsync(new[] { new BusinessLocationDto { Id = 1 } });

        var sut = BuildSut();
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.ListAsync(userId, new DebtorQueryParams
        {
            BusinessLocationIds = new List<int> { 1, 999 }
        }));
    }

    [Fact]
    public async Task ListAsync_WhenValidFilter_ShouldQueryAndReturnMapped()
    {
        var userId = Guid.NewGuid();
        _locationService.Setup(s => s.GetOwnedLocationsAsync(userId))
            .ReturnsAsync(new[] { new BusinessLocationDto { Id = 1 } });
        _debtorRepo.Setup(r => r.SearchAsync(It.IsAny<DebtorQueryParams>(), It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync((new List<Debtor>
            {
                new() { DebtorId = 10, Name = "A", BusinessLocationId = 1, IsActive = true }
            }, 1));

        var sut = BuildSut();
        var result = await sut.ListAsync(userId, new DebtorQueryParams { BusinessLocationIds = new List<int> { 1 } });

        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal(10, items[0].DebtorId);
    }

    [Fact]
    public async Task CreateAsync_WhenPhoneDuplicated_ShouldThrowConflict()
    {
        var userId = Guid.NewGuid();
        var request = new CreateDebtorRequest { BusinessLocationId = 1, Name = "Debtor", Phone = "0909" };
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);
        _debtorRepo.Setup(r => r.PhoneExistsInLocationAsync(1, "0909", null)).ReturnsAsync(true);

        var sut = BuildSut();
        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(userId, request));
        Assert.Equal(MessageKeys.DebtorPhoneDuplicate, ex.MessageKey);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_ShouldPersistAndReturnDto()
    {
        var userId = Guid.NewGuid();
        var request = new CreateDebtorRequest { BusinessLocationId = 1, Name = "  Khach No  ", Phone = "0909", CreditLimit = 1000 };

        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);
        _debtorRepo.Setup(r => r.PhoneExistsInLocationAsync(1, "0909", null)).ReturnsAsync(false);
        _debtorRepo.Setup(r => r.AddAsync(It.IsAny<Debtor>())).Callback<Debtor>(d => d.DebtorId = 22).ReturnsAsync((Debtor d) => d);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        var result = await sut.CreateAsync(userId, request);

        Assert.Equal(22, result.DebtorId);
        Assert.Equal("Khach No", result.Name);
    }

    [Fact]
    public async Task DeleteAsync_WhenOutstandingBalanceAndNoForce_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _debtorRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Debtor { DebtorId = 5, BusinessLocationId = 1, CurrentBalance = 10 });
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);

        var sut = BuildSut();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.DeleteAsync(userId, 5, false));
        Assert.Equal(MessageKeys.DebtorHasOutstandingBalance, ex.MessageKey);
    }

    [Fact]
    public async Task DeleteAsync_WhenHasActivity_ShouldSoftDelete()
    {
        var userId = Guid.NewGuid();
        var debtor = new Debtor { DebtorId = 7, BusinessLocationId = 2, CurrentBalance = 0, IsActive = true };
        _debtorRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(debtor);
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 2)).Returns(Task.CompletedTask);
        _debtorRepo.Setup(r => r.HasAnyActivityAsync(7)).ReturnsAsync(true);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        await sut.DeleteAsync(userId, 7, false);

        Assert.NotNull(debtor.DeletedAt);
        Assert.False(debtor.IsActive);
        _debtorRepo.Verify(r => r.Update(debtor), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNoActivity_ShouldHardDelete()
    {
        var userId = Guid.NewGuid();
        var debtor = new Debtor { DebtorId = 8, BusinessLocationId = 2, CurrentBalance = 0, IsActive = true };
        _debtorRepo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(debtor);
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 2)).Returns(Task.CompletedTask);
        _debtorRepo.Setup(r => r.HasAnyActivityAsync(8)).ReturnsAsync(false);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        await sut.DeleteAsync(userId, 8, false);

        _debtorRepo.Verify(r => r.Remove(debtor), Times.Once);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenAmountZero_ShouldThrowBadRequest()
    {
        var sut = BuildSut();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.RecordPaymentAsync(Guid.NewGuid(), 1, new RecordDebtPaymentRequest
        {
            Amount = 0,
            PaymentMethod = PaymentMethods.Cash
        }));
        Assert.Equal(MessageKeys.DebtorPaymentAmountZero, ex.MessageKey);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenMethodInvalid_ShouldThrowBadRequest()
    {
        var sut = BuildSut();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.RecordPaymentAsync(Guid.NewGuid(), 1, new RecordDebtPaymentRequest
        {
            Amount = 100,
            PaymentMethod = "bad_method"
        }));
        Assert.Equal(MessageKeys.DebtorPaymentMethodInvalid, ex.MessageKey);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenDebtorInactive_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        _debtorRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Debtor { DebtorId = 3, BusinessLocationId = 1, IsActive = false, CurrentBalance = 10 });
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);

        var sut = BuildSut();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.RecordPaymentAsync(userId, 3, new RecordDebtPaymentRequest
        {
            Amount = -5,
            PaymentMethod = PaymentMethods.Cash
        }));

        Assert.Equal(MessageKeys.DebtorNotActive, ex.MessageKey);
    }

    [Fact]
    public async Task RecordPaymentAsync_WhenValid_ShouldPersistAndRecordGl()
    {
        var userId = Guid.NewGuid();
        var debtor = new Debtor { DebtorId = 11, BusinessLocationId = 4, IsActive = true, CurrentBalance = 100 };

        _debtorRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(debtor);
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 4)).Returns(Task.CompletedTask);
        _debtorRepo.Setup(r => r.AddPaymentAsync(It.IsAny<DebtorPaymentTransaction>())).ReturnsAsync((DebtorPaymentTransaction tx) => tx);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        var result = await sut.RecordPaymentAsync(userId, 11, new RecordDebtPaymentRequest
        {
            Amount = -40,
            PaymentMethod = PaymentMethods.Bank,
            Notes = "  pay  "
        });

        Assert.Equal(-40, result.Amount);
        Assert.Equal(60, debtor.CurrentBalance);
        _glService.Verify(g => g.RecordDebtPaymentAsync(It.IsAny<DebtorPaymentTransaction>(), 4), Times.Once);
    }

    // ═══════════════════════════════════════════════════
    // EDGE CASES: DEBT RULES
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task RecordPayment_WhenAmountExceedsDebt_ShouldAllowPositiveBalance_AndLogGL()
    {
        var userId = Guid.NewGuid();
        var debtor = new Debtor { DebtorId = 99, BusinessLocationId = 1, IsActive = true, CurrentBalance = -100 }; // Owes 100

        _debtorRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(debtor);
        _locationService.Setup(s => s.ValidateOwnerAsync(userId, 1)).Returns(Task.CompletedTask);
        _debtorRepo.Setup(r => r.AddPaymentAsync(It.IsAny<DebtorPaymentTransaction>())).ReturnsAsync((DebtorPaymentTransaction tx) => tx);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        
        // Pays 150 > Debt 100
        var result = await sut.RecordPaymentAsync(userId, 99, new RecordDebtPaymentRequest
        {
            Amount = 150, 
            PaymentMethod = PaymentMethods.Cash,
            Notes = "Overpay"
        });

        // Balance should NOT clamp to 0. It should be -100 + 150 = 50 (Positive balance!).
        Assert.Equal(150, result.Amount);
        Assert.Equal(50, debtor.CurrentBalance); 
        
        // General Ledger should ALSO record 150, so cash is not lost.
        _glService.Verify(g => g.RecordDebtPaymentAsync(It.Is<DebtorPaymentTransaction>(tx => tx.Amount == 150), 1), Times.Once);
    }
}
