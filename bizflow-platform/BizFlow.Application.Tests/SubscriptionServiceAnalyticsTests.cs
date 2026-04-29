using BizFlow.Application.Common.Configuration;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Subscription;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class SubscriptionServiceAnalyticsTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITransactionRepository> _transactionRepo = new();
    private readonly Mock<ISubscriptionPlanPriceRepository> _planPriceRepo = new();
    private readonly Mock<IStripeService> _stripeService = new();
    private readonly Mock<IFirestoreService> _firestoreService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IBackgroundJobScheduler> _backgroundJobScheduler = new();
    private readonly Mock<IBusinessLocationRepository> _businessLocationRepository = new();
    private readonly Mock<IMessageService> _messageService = new();
    private readonly Mock<IReferenceLabelService> _labelService = new();
    private readonly Mock<ILogger<SubscriptionService>> _logger = new();

    public SubscriptionServiceAnalyticsTests()
    {
        _uow.SetupGet(x => x.Transactions).Returns(_transactionRepo.Object);
        _uow.SetupGet(x => x.PlanPrices).Returns(_planPriceRepo.Object);
        _planPriceRepo.Setup(x => x.GetByPlanIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task GetAdminAnalyticsAsync_ShouldDefaultToWeek_AndReturnSevenDays()
    {
        _transactionRepo.Setup(x => x.GetSuccessfulSubscriptionTransactionsInRangeAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SubscriptionTransactionAnalyticsRecord
                {
                    SubscriptionPlanId = 1,
                    PlanPrice = 100m,
                    FinalAmount = 100m,
                    PaidAtUtc = new DateTime(2026, 4, 28, 3, 0, 0, DateTimeKind.Utc)
                }
            ]);

        var sut = BuildSut();
        var result = await sut.GetAdminAnalyticsAsync(new AdminSubscriptionAnalyticsQuery
        {
            ReferenceDate = new DateOnly(2026, 4, 29)
        });

        Assert.Equal(new DateOnly(2026, 4, 27), result.FromDate);
        Assert.Equal(new DateOnly(2026, 5, 3), result.ToDate);
        Assert.Equal(7, result.DailySeries.Count);
        Assert.Equal(100m, result.TotalRevenue);
        Assert.Equal(1, result.TotalSubscriptionRegistrations);
    }

    [Fact]
    public async Task GetAdminAnalyticsAsync_WhenCustomWithoutDates_ShouldThrow()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() =>
            sut.GetAdminAnalyticsAsync(new AdminSubscriptionAnalyticsQuery { Period = "custom" }));
    }

    [Fact]
    public async Task GetAdminAnalyticsAsync_WhenCustomFromGreaterThanTo_ShouldThrow()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() =>
            sut.GetAdminAnalyticsAsync(new AdminSubscriptionAnalyticsQuery
            {
                Period = "custom",
                FromDate = new DateOnly(2026, 5, 1),
                ToDate = new DateOnly(2026, 4, 1)
            }));
    }

    [Fact]
    public async Task GetAdminAnalyticsAsync_ShouldFillMissingDatesWithZero()
    {
        _transactionRepo.Setup(x => x.GetSuccessfulSubscriptionTransactionsInRangeAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SubscriptionTransactionAnalyticsRecord
                {
                    SubscriptionPlanId = 1,
                    PlanPrice = 120m,
                    FinalAmount = 120m,
                    PaidAtUtc = new DateTime(2026, 4, 1, 1, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionTransactionAnalyticsRecord
                {
                    SubscriptionPlanId = 1,
                    PlanPrice = 80m,
                    FinalAmount = 80m,
                    PaidAtUtc = new DateTime(2026, 4, 3, 1, 0, 0, DateTimeKind.Utc)
                }
            ]);

        var sut = BuildSut();
        var result = await sut.GetAdminAnalyticsAsync(new AdminSubscriptionAnalyticsQuery
        {
            Period = "custom",
            FromDate = new DateOnly(2026, 4, 1),
            ToDate = new DateOnly(2026, 4, 3)
        });

        Assert.Equal(3, result.DailySeries.Count);
        Assert.Equal(0m, result.DailySeries[1].Revenue);
        Assert.Equal(0, result.DailySeries[1].SubscriptionRegistrations);
        Assert.Equal(200m, result.TotalRevenue);
        Assert.Equal(2, result.TotalSubscriptionRegistrations);
    }

    [Fact]
    public async Task GetAdminAnalyticsAsync_ShouldInferQuantityFromPlanPriceAndHistoricalUnitPrice()
    {
        _transactionRepo.Setup(x => x.GetSuccessfulSubscriptionTransactionsInRangeAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SubscriptionTransactionAnalyticsRecord
                {
                    SubscriptionPlanId = 7,
                    PlanPrice = 500m,
                    FinalAmount = 500m,
                    PaidAtUtc = new DateTime(2026, 4, 2, 1, 0, 0, DateTimeKind.Utc)
                }
            ]);

        _planPriceRepo.Setup(x => x.GetByPlanIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SubscriptionPlanPrice
                {
                    SubscriptionPlanId = 7,
                    BasePrice = 100m,
                    CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            ]);

        var sut = BuildSut();
        var result = await sut.GetAdminAnalyticsAsync(new AdminSubscriptionAnalyticsQuery
        {
            Period = "custom",
            FromDate = new DateOnly(2026, 4, 2),
            ToDate = new DateOnly(2026, 4, 2)
        });

        Assert.Equal(500m, result.TotalRevenue);
        Assert.Equal(5, result.TotalSubscriptionRegistrations);
    }

    private SubscriptionService BuildSut()
    {
        return new SubscriptionService(
            _uow.Object,
            _stripeService.Object,
            _firestoreService.Object,
            _notificationService.Object,
            _backgroundJobScheduler.Object,
            _businessLocationRepository.Object,
            _messageService.Object,
            _labelService.Object,
            Options.Create(new StripeSettings()),
            Options.Create(new FreePlanOptions()),
            _logger.Object);
    }
}
