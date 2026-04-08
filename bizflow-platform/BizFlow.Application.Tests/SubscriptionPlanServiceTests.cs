using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Subscription;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class SubscriptionPlanServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepo = new();
    private readonly Mock<IFeatureRepository> _featureRepo = new();
    private readonly Mock<ISubscriptionPlanPriceRepository> _priceRepo = new();
    private readonly Mock<IStripeService> _stripeService = new();
    private readonly Mock<IFirestoreService> _firestoreService = new();

    public SubscriptionPlanServiceTests()
    {
        _uow.SetupGet(u => u.SubscriptionPlans).Returns(_planRepo.Object);
        _uow.SetupGet(u => u.Features).Returns(_featureRepo.Object);
        _uow.SetupGet(u => u.PlanPrices).Returns(_priceRepo.Object);
    }

    private SubscriptionPlanService BuildSut() => new(
        _uow.Object,
        _stripeService.Object,
        _firestoreService.Object,
        NullLogger<SubscriptionPlanService>.Instance);

    // ═══════════════════════════════════════════════════
    // CREATE PLAN ASYNC
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CreatePlanAsync_WhenFeatureNotFound_ShouldThrowBadRequest()
    {
        var request = new CreateSubscriptionPlanRequest
        {
            Name = "Test",
            Features = new List<PlanFeatureRequest> { new PlanFeatureRequest { FeatureId = 99 } }
        };

        _featureRepo.Setup(r => r.GetExistingIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new HashSet<int>());

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreatePlanAsync(request));
    }

    [Fact]
    public async Task CreatePlanAsync_WhenDiscountRulesViolated_ShouldThrowBadRequest()
    {
        var request = new CreateSubscriptionPlanRequest
        {
            Name = "Test",
            Features = new List<PlanFeatureRequest> { new PlanFeatureRequest { FeatureId = 1, UsageLimit = 10 } },
            Price = new CreatePlanPriceRequest
            {
                BasePrice = 100,
                DiscountedPrice = 50,
                DiscountStart = DateTime.UtcNow.AddDays(5),
                DiscountEnd = DateTime.UtcNow.AddDays(-1)
            }
        };

        _featureRepo.Setup(r => r.GetExistingIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new HashSet<int> { 1 });

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreatePlanAsync(request));
    }

    [Fact]
    public async Task CreatePlanAsync_WhenValid_ShouldSaveAndReturnDto()
    {
        var request = new CreateSubscriptionPlanRequest
        {
            Name = "Valid Plan",
            DurationDays = 30,
            Features = new List<PlanFeatureRequest>(),
            Price = new CreatePlanPriceRequest { BasePrice = 1000 }
        };

        var planOutput = new SubscriptionPlan
        {
            SubscriptionPlanId = 1,
            Name = "Valid Plan",
            DurationDays = 30,
            Prices = new List<SubscriptionPlanPrice> { new SubscriptionPlanPrice { BasePrice = 1000, IsActive = true } }
        };

        _planRepo.Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>())).Returns(Task.CompletedTask);
        _planRepo.Setup(r => r.GetByIdWithFeaturesAndPriceAsync(It.IsAny<int>()))
            .ReturnsAsync(planOutput);

        var sut = BuildSut();

        var result = await sut.CreatePlanAsync(request);

        Assert.Equal("Valid Plan", result.Name);
        Assert.Equal(1000, result.CurrentPrice!.BasePrice);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════
    // SET PLAN STATUS ASYNC
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task SetPlanStatusAsync_WhenActivatingWithoutPrice_ShouldThrowBadRequest()
    {
        var plan = new SubscriptionPlan { SubscriptionPlanId = 1, Name = "No Price Plan" };

        _planRepo.Setup(r => r.GetByIdWithFeaturesAndPriceAsync(1))
            .ReturnsAsync(plan);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.SetPlanStatusAsync(1, true));
    }

    [Fact]
    public async Task SetPlanStatusAsync_WhenActivatingWithoutFeatures_ShouldThrowBadRequest()
    {
        var plan = new SubscriptionPlan
        {
            SubscriptionPlanId = 1,
            Name = "No Feature Plan",
            Prices = new List<SubscriptionPlanPrice> { new SubscriptionPlanPrice { IsActive = true, BasePrice = 10 } }
        };

        _planRepo.Setup(r => r.GetByIdWithFeaturesAndPriceAsync(1))
            .ReturnsAsync(plan);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.SetPlanStatusAsync(1, true));
    }

    [Fact]
    public async Task SetPlanStatusAsync_WhenActivatingWithValidData_ShouldCreateStripeProductAndPrice()
    {
        var plan = new SubscriptionPlan
        {
            SubscriptionPlanId = 1,
            Name = "Valid Plan",
            Prices = new List<SubscriptionPlanPrice> { new SubscriptionPlanPrice { IsActive = true, BasePrice = 1000 } },
            PlanFeatures = new List<PlanFeature> { new PlanFeature { FeatureId = 1, Feature = new Feature() } }
        };

        _planRepo.Setup(r => r.GetByIdWithFeaturesAndPriceAsync(1))
            .ReturnsAsync(plan);

        _stripeService.Setup(s => s.IsConfigured).Returns(true);
        _stripeService.Setup(s => s.CreateProductAsync(plan.Name, It.IsAny<string?>(), It.IsAny<Dictionary<string, string>?>()))
            .ReturnsAsync(new Stripe.Product { Id = "prod_123" });
        _stripeService.Setup(s => s.CreatePriceAsync("prod_123", 1000, It.IsAny<string>()))
            .ReturnsAsync(new Stripe.Price { Id = "price_456" });

        var sut = BuildSut();

        var result = await sut.SetPlanStatusAsync(1, true);

        Assert.True(result.IsActive);
        Assert.Equal("prod_123", plan.StripeProductId);
        Assert.Equal("price_456", plan.StripePriceId);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
