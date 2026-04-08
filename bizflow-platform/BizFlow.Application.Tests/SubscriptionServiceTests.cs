using BizFlow.Application.Common.Configuration;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Subscription;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using BizFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class SubscriptionServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IStripeService> _stripeService = new();
    private readonly Mock<IFirestoreService> _firestoreService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IBackgroundJobScheduler> _bgJob = new();
    private readonly Mock<IBusinessLocationRepository> _businessRepo = new();
    private readonly Mock<IMessageService> _messageService = new();
    private readonly Mock<ISubscriptionRepository> _subRepo = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepo = new();
    private readonly Mock<IFeatureUsageRepository> _featureUsageRepo = new();
    private readonly Mock<ISubscriptionAuditLogRepository> _auditRepo = new();

    private readonly FreePlanOptions _freeOptions = new()
    {
        PlanName = "Free",
        DurationDaysInDatabase = 30,
        Features = new List<FreePlanFeatureOption> { new() { FeatureCode = "DASHBOARD", UsageLimit = -1 } }
    };

    public SubscriptionServiceTests()
    {
        _uow.SetupGet(u => u.Subscriptions).Returns(_subRepo.Object);
        _uow.SetupGet(u => u.SubscriptionPlans).Returns(_planRepo.Object);
        _uow.SetupGet(u => u.FeatureUsages).Returns(_featureUsageRepo.Object);
        _uow.SetupGet(u => u.SubscriptionAuditLogs).Returns(_auditRepo.Object);
    }

    private SubscriptionService BuildSut() => new(
        _uow.Object,
        _stripeService.Object,
        _firestoreService.Object,
        _notificationService.Object,
        _bgJob.Object,
        _businessRepo.Object,
        _messageService.Object,
        Options.Create(new StripeSettings()),
        Options.Create(_freeOptions),
        NullLogger<SubscriptionService>.Instance);

    // ═══════════════════════════════════════════════════
    // FREE SUBSCRIPTION
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task EnsureFreeSubscriptionAsync_WhenAlreadyActive_ShouldReturn()
    {
        var ownerId = Guid.NewGuid();
        _subRepo.Setup(r => r.GetActiveByOwnerAsync(ownerId))
            .ReturnsAsync(new Subscription()); // Already active

        var sut = BuildSut();
        await sut.EnsureFreeSubscriptionAsync(ownerId);

        // Should not add anything
        _subRepo.Verify(r => r.AddAsync(It.IsAny<Subscription>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════
    // EVALUATE ACCESS
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateFeatureAccessAsync_WhenNoActivePlan_ShouldDeny()
    {
        var ownerId = Guid.NewGuid();
        _businessRepo.Setup(r => r.HasAccessToLocationAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
        _businessRepo.Setup(r => r.GetOwnerIdByLocationAsync(It.IsAny<int>())).ReturnsAsync(ownerId);
        
        _subRepo.Setup(r => r.GetActiveByOwnerAsync(ownerId))
            .ReturnsAsync((Subscription?)null);

        var sut = BuildSut();
        var result = await sut.EvaluateFeatureAccessAsync(Guid.NewGuid(), 1, "DASHBOARD");

        Assert.False(result.Allowed);
        Assert.Equal(FeatureAccessDenialReason.NoActiveSubscription, result.DenialReason);
    }

    [Fact]
    public async Task EvaluateFeatureAccessAsync_WhenFeatureNotIncluded_ShouldDeny()
    {
        var ownerId = Guid.NewGuid();
        var activeSub = new Subscription
        {
            SubscriptionPlan = new SubscriptionPlan
            {
                PlanFeatures = new List<PlanFeature> { new PlanFeature { Feature = new Feature { FeatureCode = "OTHER" }, UsageLimit = 10 } }
            }
        };

        _businessRepo.Setup(r => r.HasAccessToLocationAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
        _businessRepo.Setup(r => r.GetOwnerIdByLocationAsync(It.IsAny<int>())).ReturnsAsync(ownerId);
        _subRepo.Setup(r => r.GetActiveByOwnerAsync(ownerId)).ReturnsAsync(activeSub);

        var sut = BuildSut();
        var result = await sut.EvaluateFeatureAccessAsync(Guid.NewGuid(), 1, "DASHBOARD");

        Assert.False(result.Allowed);
        Assert.Equal(FeatureAccessDenialReason.FeatureNotInPlan, result.DenialReason);
    }

    [Fact]
    public async Task EvaluateFeatureAccessAsync_WhenLimitReached_ShouldDeny()
    {
        var ownerId = Guid.NewGuid();
        var featureId = 1;

        var activeSub = new Subscription
        {
            SubscriptionPlan = new SubscriptionPlan
            {
                PlanFeatures = new List<PlanFeature> { new PlanFeature { FeatureId = featureId, Feature = new Feature { FeatureCode = "DASHBOARD" }, UsageLimit = 5 } }
            },
            FeatureUsages = new List<FeatureUsage> { new FeatureUsage { FeatureId = featureId, AllocatedLimit = 5, UsedCount = 5 } } // Used up
        };

        _businessRepo.Setup(r => r.HasAccessToLocationAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
        _businessRepo.Setup(r => r.GetOwnerIdByLocationAsync(It.IsAny<int>())).ReturnsAsync(ownerId);
        _subRepo.Setup(r => r.GetActiveByOwnerAsync(ownerId)).ReturnsAsync(activeSub);

        _firestoreService.Setup(f => f.GetUsageSnapshotAsync(ownerId, "DASHBOARD"))
            .ReturnsAsync((FeatureUsageSnapshot?)null);

        var sut = BuildSut();
        var result = await sut.EvaluateFeatureAccessAsync(Guid.NewGuid(), 1, "DASHBOARD");

        Assert.False(result.Allowed);
        Assert.Equal(FeatureAccessDenialReason.UsageLimitReached, result.DenialReason);
    }

    // ═══════════════════════════════════════════════════
    // CREATE CHECKOUT SESSION
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenFreePlan_ShouldThrowBadRequest()
    {
        var plan = new SubscriptionPlan { Name = "Free Plan", Prices = new List<SubscriptionPlanPrice> { new SubscriptionPlanPrice { BasePrice = 0, IsActive = true } } };
        
        _planRepo.Setup(r => r.GetByIdWithFeaturesAndPriceAsync(1))
            .ReturnsAsync(plan);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateCheckoutSessionAsync(Guid.NewGuid(), 1));
    }
}
