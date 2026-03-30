using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Subscription;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BizFlow.Application.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStripeService _stripeService;
        private readonly IFirestoreService _firestoreService;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundJobScheduler _backgroundJobScheduler;
        private readonly IBusinessLocationRepository _businessLocationRepository;
        private readonly IMessageService _messageService;
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            IUnitOfWork unitOfWork,
            IStripeService stripeService,
            IFirestoreService firestoreService,
            INotificationService notificationService,
            IBackgroundJobScheduler backgroundJobScheduler,
            IBusinessLocationRepository businessLocationRepository,
            IMessageService messageService,
            IOptions<StripeSettings> stripeSettings,
            ILogger<SubscriptionService> logger)
        {
            _unitOfWork = unitOfWork;
            _stripeService = stripeService;
            _firestoreService = firestoreService;
            _notificationService = notificationService;
            _backgroundJobScheduler = backgroundJobScheduler;
            _businessLocationRepository = businessLocationRepository;
            _messageService = messageService;
            _stripeSettings = stripeSettings.Value;
            _logger = logger;
        }



        public async Task<CurrentSubscriptionDto> GetCurrentSubscriptionAsync(Guid profileId)
        {
            var active = await _unitOfWork.Subscriptions.GetActiveByOwnerAsync(profileId);
            if (active == null)
            {
                var hasAny = await _unitOfWork.Subscriptions.HasAnySubscriptionAsync(profileId);
                return new CurrentSubscriptionDto
                {
                    Status = hasAny ? SubscriptionStatus.Expired : SubscriptionStatus.Inactive
                };
            }

            var latestPrice = active.SubscriptionPlan.Prices?.FirstOrDefault(p => p.IsActive);
            var allocatedByFeatureId = active.FeatureUsages
                .ToDictionary(u => u.FeatureId, u => u.AllocatedLimit);

            return new CurrentSubscriptionDto
            {
                SubscriptionId = active.SubscriptionId,
                Status = active.Status,
                StartDate = active.StartDate,
                EndDate = active.EndDate,
                Plan = new SubscriptionPlanDto
                {
                    SubscriptionPlanId = active.SubscriptionPlan.SubscriptionPlanId,
                    Name = active.SubscriptionPlan.Name,
                    Description = active.SubscriptionPlan.Description,
                    DurationDays = active.SubscriptionPlan.DurationDays,
                    CurrentPrice = latestPrice != null ? MapPriceToDto(latestPrice) : null,
                    Features = active.SubscriptionPlan.PlanFeatures.Select(pf => new PlanFeatureDto
                    {
                        FeatureId = pf.FeatureId,
                        FeatureCode = pf.Feature?.FeatureCode ?? string.Empty,
                        FeatureName = pf.Feature?.Name ?? string.Empty,
                        UsageLimit = allocatedByFeatureId.TryGetValue(pf.FeatureId, out var l) ? l : pf.UsageLimit
                    }).ToList()
                }
            };
        }

        public async Task<CheckoutSessionResponseDto> CreateCheckoutSessionAsync(Guid profileId, int subscriptionPlanId, string platform = "web", int quantity = 1)
        {
            if (subscriptionPlanId <= 0)
                throw new BadRequestException(MessageKeys.InvalidSubscriptionPlanId);
            if (quantity <= 0)
                throw new BadRequestException(MessageKeys.SubscriptionCheckoutQuantityInvalid);

            var active = await _unitOfWork.Subscriptions.GetActiveByOwnerAsync(profileId);
            if (active != null && active.SubscriptionPlanId != subscriptionPlanId)
            {
                throw new ConflictException(MessageKeys.SubscriptionAlreadyActive);
            }

            return await CreatePendingCheckoutSessionAsync(profileId, subscriptionPlanId, TransactionType.Purchase, 0m, null, platform, quantity);
        }

        public Task RevokeAccessGrantAsync(Guid ownerProfileId, Guid memberProfileId)
        {
            if (memberProfileId == Guid.Empty || memberProfileId == ownerProfileId)
                throw new BadRequestException(MessageKeys.SubscriptionAccessGrantMemberInvalid);

            return _firestoreService.RevokeSubscriptionAccessGrantAsync(ownerProfileId, memberProfileId);
        }

        public async Task<List<TransactionDto>> GetTransactionsAsync(Guid profileId, int page = 1, int pageSize = 20)
        {
            var transactions = await _unitOfWork.Transactions.GetByProfileAsync(profileId, page, pageSize);
            return transactions.Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                SubscriptionPlanId = t.SubscriptionPlanId,
                PlanName = t.SubscriptionPlan.Name,
                TransactionType = t.TransactionType,
                Status = t.Status,
                PlanPrice = t.PlanPrice,
                ProrationCredit = t.ProrationCredit,
                FinalAmount = t.FinalAmount,
                Currency = t.Currency,
                PaidAt = t.PaidAt,
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        public async Task HandleCheckoutCompletedAsync(StripeCheckoutSessionPayload session)
        {
            var transaction = await ResolveTransactionAsync(session);
            if (transaction == null || transaction.Status == TransactionStatus.Success || transaction.Status == TransactionStatus.Refunded)
            {
                return;
            }

            try
            {
                var plan = await _unitOfWork.SubscriptionPlans.GetByIdWithFeaturesAsync(transaction.SubscriptionPlanId)
                    ?? throw new NotFoundException(MessageKeys.SubscriptionPlanNotFound);

                var now = DateTime.UtcNow;
                var purchasedQuantity = ResolveCheckoutQuantity(session);
                var active = await _unitOfWork.Subscriptions.GetActiveByOwnerAsync(transaction.ProfileId);

                var stackSamePlan =
                    active != null
                    && string.Equals(active.Status, SubscriptionStatus.Active, StringComparison.OrdinalIgnoreCase)
                    && active.SubscriptionPlanId == plan.SubscriptionPlanId
                    && (
                        string.Equals(transaction.TransactionType, TransactionType.Purchase, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(transaction.TransactionType, TransactionType.Renew, StringComparison.OrdinalIgnoreCase));

                if (stackSamePlan)
                {
                    var activeSubscription = active!;
                    var periodDays = Math.Max(1, plan.DurationDays) * purchasedQuantity;
                    var planFeatureById = plan.PlanFeatures.ToDictionary(pf => pf.FeatureId, pf => pf.UsageLimit);
                    var baseEnd = activeSubscription.EndDate > now ? activeSubscription.EndDate : now;
                    activeSubscription.EndDate = baseEnd.AddDays(periodDays);
                    activeSubscription.UpdatedAt = now;

                    foreach (var u in activeSubscription.FeatureUsages)
                    {
                        u.PeriodEnd = activeSubscription.EndDate;
                        // Quantity stacking should accumulate the usage limit too.
                        if (planFeatureById.TryGetValue(u.FeatureId, out var usageLimit))
                        {
                            if (usageLimit == -1 || u.AllocatedLimit == -1)
                            {
                                u.AllocatedLimit = -1;
                            }
                            else
                            {
                                u.AllocatedLimit += usageLimit * purchasedQuantity;
                            }
                        }
                        u.UpdatedAt = now;
                    }

                    transaction.Status = TransactionStatus.Success;
                    transaction.PaidAt = now;
                    transaction.SubscriptionId = activeSubscription.SubscriptionId;
                    transaction.StripeCheckoutSessionId = session.SessionId;
                    transaction.StripePaymentIntentId = session.PaymentIntentId;
                    transaction.UpdatedAt = now;

                    await _unitOfWork.SubscriptionAuditLogs.AddAsync(new SubscriptionAuditLog
                    {
                        SubscriptionId = activeSubscription.SubscriptionId,
                        Action = SubscriptionAuditAction.Renewed,
                        Details = $"{{\"transactionId\":\"{transaction.TransactionId}\",\"stackDays\":{periodDays}}}",
                        CreatedAt = now
                    });

                    await _unitOfWork.SaveChangesAsync();

                    var existingUsed = activeSubscription.FeatureUsages
                        .Where(u => u.Feature != null)
                        .ToDictionary(u => u.Feature!.FeatureCode, u => u.UsedCount);

                    await TrySetUsageTrackingSafeAsync(
                        transaction.ProfileId,
                        activeSubscription,
                        plan.PlanFeatures.ToList(),
                        existingUsed.Count > 0 ? existingUsed : null);

                    await _notificationService.SendToAllDevicesAsync(
                        transaction.ProfileId,
                        _messageService.GetMessage(MessageKeys.SubscriptionActivatedTitle),
                        _messageService.GetMessage(MessageKeys.SubscriptionActivatedBody, plan.Name));

                    await GrantAccessToAcceptedEmployeesSafeAsync(transaction.ProfileId);

                    return;
                }

                var newSubscription = new Subscription
                {
                    SubscriptionId = Guid.NewGuid(),
                    OwnerProfileId = transaction.ProfileId,
                    SubscriptionPlanId = plan.SubscriptionPlanId,
                    Status = SubscriptionStatus.Active,
                    IsAutoRenew = false,
                    StartDate = now,
                    EndDate = now.AddDays(Math.Max(1, plan.DurationDays) * purchasedQuantity),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var featureUsages = plan.PlanFeatures.Select(feature => new FeatureUsage
                {
                    SubscriptionId = newSubscription.SubscriptionId,
                    FeatureId = feature.FeatureId,
                    UsedCount = 0,
                    AllocatedLimit = feature.UsageLimit == -1 ? -1 : feature.UsageLimit * purchasedQuantity,
                    PeriodStart = newSubscription.StartDate,
                    PeriodEnd = newSubscription.EndDate,
                    UpdatedAt = now
                }).ToList();

                await _unitOfWork.Subscriptions.AddAsync(newSubscription);
                await _unitOfWork.FeatureUsages.AddRangeAsync(featureUsages);

                transaction.Status = TransactionStatus.Success;
                transaction.PaidAt = now;
                transaction.SubscriptionId = newSubscription.SubscriptionId;
                transaction.StripeCheckoutSessionId = session.SessionId;
                transaction.StripePaymentIntentId = session.PaymentIntentId;
                transaction.UpdatedAt = now;

                await _unitOfWork.SubscriptionAuditLogs.AddAsync(new SubscriptionAuditLog
                {
                    SubscriptionId = newSubscription.SubscriptionId,
                    Action = string.Equals(transaction.TransactionType, TransactionType.Renew, StringComparison.OrdinalIgnoreCase)
                        ? SubscriptionAuditAction.Renewed
                        : SubscriptionAuditAction.Activated,
                    Details = $"{{\"transactionId\":\"{transaction.TransactionId}\"}}",
                    CreatedAt = now
                });

                await _unitOfWork.SaveChangesAsync();

                await TrySetUsageTrackingSafeAsync(
                    transaction.ProfileId,
                    newSubscription,
                    plan.PlanFeatures.ToList());

                await _notificationService.SendToAllDevicesAsync(
                    transaction.ProfileId,
                    _messageService.GetMessage(MessageKeys.SubscriptionActivatedTitle),
                    _messageService.GetMessage(MessageKeys.SubscriptionActivatedBody, plan.Name));

                await GrantAccessToAcceptedEmployeesSafeAsync(transaction.ProfileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout completed handling failed for transaction {TransactionId}. Attempting compensation refund.", transaction.TransactionId);

                // Make sure transaction isn't stuck in Pending state.
                transaction.Status = TransactionStatus.Failed;
                transaction.UpdatedAt = DateTime.UtcNow;
                try
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception saveEx)
                {
                    _logger.LogWarning(saveEx, "Failed to persist TransactionStatus=Failed for {TransactionId}", transaction.TransactionId);
                }

                var refunded = await TryCompensateRefundAsync(transaction, session, ex.Message);
                if (!refunded)
                {
                    throw;
                }
            }
        }

        public async Task HandleCheckoutExpiredAsync(StripeCheckoutSessionPayload session)
        {
            var transaction = await ResolveTransactionAsync(session);
            if (transaction == null || transaction.Status == TransactionStatus.Success || transaction.Status == TransactionStatus.Refunded)
            {
                return;
            }

            transaction.Status = TransactionStatus.Failed;
            transaction.UpdatedAt = DateTime.UtcNow;
            transaction.StripeCheckoutSessionId = session.SessionId;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task HandlePaymentFailedAsync(StripePaymentIntentPayload paymentIntent)
        {
            if (string.IsNullOrWhiteSpace(paymentIntent.PaymentIntentId))
            {
                return;
            }

            var transaction = await _unitOfWork.Transactions.GetByPaymentIntentIdAsync(paymentIntent.PaymentIntentId);
            if (transaction == null || transaction.Status == TransactionStatus.Success || transaction.Status == TransactionStatus.Refunded)
            {
                return;
            }

            transaction.Status = TransactionStatus.Failed;
            transaction.UpdatedAt = DateTime.UtcNow;
            transaction.StripePaymentIntentId = paymentIntent.PaymentIntentId;

            await _unitOfWork.SaveChangesAsync();

            await _notificationService.SendToAllDevicesAsync(
                transaction.ProfileId,
                _messageService.GetMessage(MessageKeys.PaymentFailedTitle),
                _messageService.GetMessage(MessageKeys.PaymentFailedBody));
        }

        public async Task HandleChargeRefundedAsync(string paymentIntentId)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                return;
            }

            var transaction = await _unitOfWork.Transactions.GetByPaymentIntentIdAsync(paymentIntentId);
            if (transaction == null || transaction.Status == TransactionStatus.Refunded)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var quantity = 1;
            if (!string.IsNullOrWhiteSpace(transaction.StripeCheckoutSessionId))
            {
                var checkoutSession = await _stripeService.GetCheckoutSessionAsync(transaction.StripeCheckoutSessionId);
                if (checkoutSession?.Metadata != null
                    && checkoutSession.Metadata.TryGetValue("quantity", out var quantityRaw)
                    && int.TryParse(quantityRaw, out var parsedQty)
                    && parsedQty > 0)
                {
                    quantity = parsedQty;
                }
            }

            if (transaction.SubscriptionId.HasValue)
            {
                var subscription = await _unitOfWork.Subscriptions.GetByIdWithUsagesAsync(transaction.SubscriptionId.Value);
                var plan = await _unitOfWork.SubscriptionPlans.GetByIdWithFeaturesAsync(transaction.SubscriptionPlanId);

                if (subscription != null && plan != null)
                {
                    var rollbackDays = Math.Max(1, plan.DurationDays) * quantity;
                    var planFeatureUsageLimitById = plan.PlanFeatures.ToDictionary(pf => pf.FeatureId, pf => pf.UsageLimit);
                    subscription.EndDate = subscription.EndDate.AddDays(-rollbackDays);
                    if (subscription.EndDate <= now)
                    {
                        subscription.EndDate = now;
                        subscription.Status = SubscriptionStatus.Expired;
                    }

                    subscription.UpdatedAt = now;

                    foreach (var usage in subscription.FeatureUsages)
                    {
                        usage.PeriodEnd = subscription.EndDate;
                        // Refund should also rollback accumulated limits for the refunded quantity.
                        if (planFeatureUsageLimitById.TryGetValue(usage.FeatureId, out var plannedUsageLimit))
                        {
                            if (plannedUsageLimit == -1 || usage.AllocatedLimit == -1)
                            {
                                usage.AllocatedLimit = -1;
                            }
                            else
                            {
                                var delta = plannedUsageLimit * quantity;
                                usage.AllocatedLimit = Math.Max(0, usage.AllocatedLimit - delta);
                            }
                        }
                        usage.UpdatedAt = now;
                    }

                    var existingUsed = subscription.FeatureUsages
                        .Where(u => u.Feature != null)
                        .ToDictionary(u => u.Feature!.FeatureCode, u => u.UsedCount);

                    await TrySetUsageTrackingSafeAsync(
                        transaction.ProfileId,
                        subscription,
                        plan.PlanFeatures.ToList(),
                        existingUsed.Count > 0 ? existingUsed : null);
                }
            }

            transaction.Status = TransactionStatus.Refunded;
            transaction.UpdatedAt = now;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> CheckFeatureAccessAsync(Guid profileId, int locationId, string featureCode, bool incrementUsage = false)
        {
            var hasAccess = await _businessLocationRepository.HasAccessToLocationAsync(profileId, locationId);
            if (!hasAccess)
            {
                return false;
            }

            var ownerId = await ResolveOwnerProfileIdAsync(profileId, locationId);
            if (ownerId == null)
            {
                return false;
            }

            var active = await _unitOfWork.Subscriptions.GetActiveByOwnerAsync(ownerId.Value);
            if (active == null)
            {
                return false;
            }

            var planFeature = active.SubscriptionPlan.PlanFeatures
                .FirstOrDefault(pf => string.Equals(pf.Feature.FeatureCode, featureCode, StringComparison.OrdinalIgnoreCase));

            if (planFeature == null || planFeature.UsageLimit == 0)
            {
                return false;
            }

            var allocatedLimit = active.FeatureUsages
                .FirstOrDefault(u => u.FeatureId == planFeature.FeatureId)?.AllocatedLimit
                ?? planFeature.UsageLimit;

            if (allocatedLimit > 0)
            {
                var usageSnapshot = await _firestoreService.GetUsageSnapshotAsync(ownerId.Value, featureCode);
                if (usageSnapshot != null)
                {
                    if (usageSnapshot.Limit >= 0 && usageSnapshot.Used >= usageSnapshot.Limit)
                    {
                        return false;
                    }
                }
                else
                {
                    var sqlUsed = active.FeatureUsages
                        .Where(x => x.FeatureId == planFeature.FeatureId)
                        .Select(x => x.UsedCount)
                        .FirstOrDefault();

                    if (sqlUsed >= allocatedLimit)
                    {
                        return false;
                    }
                }
            }
            else if (allocatedLimit == 0)
            {
                return false;
            }

            if (incrementUsage)
            {
                await _firestoreService.IncrementFeatureUsageAsync(ownerId.Value, featureCode);
                _backgroundJobScheduler.EnqueueIncrementUsageSql(active.SubscriptionId, featureCode);
            }

            return true;
        }

        public async Task IncrementUsageSqlBackgroundAsync(Guid subscriptionId, string featureCode)
        {
            var usage = await _unitOfWork.FeatureUsages.GetBySubscriptionAndFeatureCodeAsync(subscriptionId, featureCode);
            if (usage == null)
            {
                return;
            }

            usage.UsedCount += 1;
            usage.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<CheckoutSessionResponseDto> CreatePendingCheckoutSessionAsync(
            Guid profileId,
            int subscriptionPlanId,
            string transactionType,
            decimal prorationCredit,
            Guid? sourceSubscriptionId,
            string platform = "web",
            int quantity = 1)
        {
            var safeQuantity = Math.Max(1, quantity);
            var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(subscriptionPlanId)
                ?? throw new NotFoundException(MessageKeys.SubscriptionPlanNotFound);

            var planPrice = await _unitOfWork.PlanPrices.GetActiveByPlanIdAsync(subscriptionPlanId);
            await EnsureStripePriceForCheckoutAsync(plan, planPrice);

            if (string.IsNullOrWhiteSpace(plan.StripePriceId))
            {
                throw new BadRequestException(MessageKeys.PlanStripePriceNotConfigured);
            }

            var unitPrice = planPrice?.GetEffectivePrice() ?? 0m;
            var totalPlanPrice = unitPrice * safeQuantity;
            var finalAmount = decimal.Round(Math.Max(0m, totalPlanPrice - prorationCredit), 2, MidpointRounding.AwayFromZero);

            var now = DateTime.UtcNow;
            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                ProfileId = profileId,
                SubscriptionPlanId = subscriptionPlanId,
                SubscriptionId = sourceSubscriptionId,
                IdempotencyKey = Guid.NewGuid().ToString("N"),
                TransactionType = transactionType,
                PlanPrice = totalPlanPrice,
                ProrationCredit = prorationCredit,
                FinalAmount = finalAmount,
                Currency = "VND",
                Status = TransactionStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _unitOfWork.Transactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            var profile = await _unitOfWork.Profiles.GetByIdAsync(profileId)
                ?? throw new NotFoundException(MessageKeys.UserNotFound);

            var session = await _stripeService.CreateCheckoutSessionAsync(
                profile.StripeCustomerId,
                plan.StripePriceId,
                transaction.TransactionId,
                profileId,
                transaction.IdempotencyKey,
                platform,
                safeQuantity);

            transaction.StripeCheckoutSessionId = session.Id;
            transaction.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(session.CustomerId) && string.IsNullOrWhiteSpace(profile.StripeCustomerId))
            {
                profile.StripeCustomerId = session.CustomerId;
                profile.UpdatedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();

            return new CheckoutSessionResponseDto
            {
                TransactionId = transaction.TransactionId,
                SessionUrl = session.Url ?? string.Empty,
                PlanPrice = totalPlanPrice,
                ProrationCredit = prorationCredit,
                FinalAmount = finalAmount,
                Currency = transaction.Currency,
                TransactionType = transactionType
            };
        }

        /// <summary>
        /// Best-effort auto provisioning: if a plan is missing StripePriceId at checkout time,
        /// try creating a Stripe price from current effective plan price.
        /// </summary>
        private async Task EnsureStripePriceForCheckoutAsync(SubscriptionPlan plan, SubscriptionPlanPrice? activePrice)
        {
            if (!string.IsNullOrWhiteSpace(plan.StripePriceId))
            {
                return;
            }

            if (!_stripeService.IsConfigured)
            {
                _logger.LogWarning(
                    "Skip Stripe price auto-provision for plan {PlanId}: Stripe service not configured",
                    plan.SubscriptionPlanId);
                return;
            }

            if (string.IsNullOrWhiteSpace(plan.StripeProductId))
            {
                try
                {
                    var productMetadata = new Dictionary<string, string>
                    {
                        ["subscriptionPlanId"] = plan.SubscriptionPlanId.ToString(),
                        ["source"] = "checkout-autoprovision"
                    };

                    var createdProduct = await _stripeService.CreateProductAsync(
                        plan.Name,
                        plan.Description,
                        productMetadata);

                    plan.StripeProductId = createdProduct.Id;
                    _logger.LogInformation(
                        "Auto-provisioned StripeProductId for plan {PlanId}: {StripeProductId}",
                        plan.SubscriptionPlanId,
                        plan.StripeProductId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to auto-provision Stripe product for plan {PlanId}",
                        plan.SubscriptionPlanId);
                    return;
                }
            }

            var unitAmount = (long)(activePrice?.GetEffectivePrice() ?? 0m);
            if (unitAmount <= 0)
            {
                _logger.LogWarning(
                    "Skip Stripe price auto-provision for plan {PlanId}: effective price is invalid ({UnitAmount})",
                    plan.SubscriptionPlanId,
                    unitAmount);
                return;
            }

            try
            {
                var created = await _stripeService.CreatePriceAsync(plan.StripeProductId, unitAmount);
                plan.StripePriceId = created.Id;
                plan.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Auto-provisioned StripePriceId for plan {PlanId}: {StripePriceId}",
                    plan.SubscriptionPlanId,
                    plan.StripePriceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to auto-provision Stripe price for plan {PlanId}",
                    plan.SubscriptionPlanId);
            }
        }

        private static int ResolveCheckoutQuantity(StripeCheckoutSessionPayload session)
        {
            if (session.Metadata != null &&
                session.Metadata.TryGetValue("quantity", out var quantityRaw) &&
                int.TryParse(quantityRaw, out var quantity) &&
                quantity > 0)
            {
                return quantity;
            }

            return 1;
        }

        private async Task TrySetUsageTrackingSafeAsync(
            Guid ownerProfileId,
            Subscription subscription,
            IReadOnlyCollection<PlanFeature> features,
            IReadOnlyDictionary<string, int>? existingUsed = null)
        {
            try
            {
                await _firestoreService.SetUsageTrackingAsync(ownerProfileId, subscription, features, existingUsed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Firestore sync skipped for subscription {SubscriptionId} (owner {OwnerProfileId}).",
                    subscription.SubscriptionId,
                    ownerProfileId);
            }
        }

        private async Task GrantAccessToAcceptedEmployeesSafeAsync(Guid ownerProfileId)
        {
            try
            {
                var employeeIds = await _unitOfWork.Hires.GetHiredEmployeeIdsAsync(ownerProfileId);
                foreach (var employeeId in employeeIds)
                {
                    await _firestoreService.UpsertSubscriptionAccessGrantAsync(
                        ownerProfileId,
                        employeeId,
                        canReadUsage: true,
                        isActive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Auto access-grant sync skipped for owner {OwnerProfileId}.",
                    ownerProfileId);
            }
        }

        private async Task<Transaction?> ResolveTransactionAsync(StripeCheckoutSessionPayload session)
        {
            if (session.Metadata != null &&
                session.Metadata.TryGetValue("transactionId", out var transactionIdRaw) &&
                Guid.TryParse(transactionIdRaw, out var transactionId))
            {
                return await _unitOfWork.Transactions.GetByIdAsync(transactionId);
            }

            if (!string.IsNullOrWhiteSpace(session.SessionId))
            {
                return await _unitOfWork.Transactions.GetByCheckoutSessionIdAsync(session.SessionId);
            }

            return null;
        }

        private async Task<Guid?> ResolveOwnerProfileIdAsync(Guid profileId, int locationId)
        {
            var isOwner = await _businessLocationRepository.IsOwnerOfLocationAsync(profileId, locationId);
            if (isOwner)
            {
                return profileId;
            }

            return await _businessLocationRepository.GetOwnerIdByLocationAsync(locationId);
        }

        private async Task<bool> TryCompensateRefundAsync(Transaction transaction, StripeCheckoutSessionPayload session, string reason)
        {
            var paymentIntentId = transaction.StripePaymentIntentId ?? session.PaymentIntentId;
            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                _logger.LogError("Cannot compensate refund for transaction {TransactionId} because PaymentIntentId is missing.", transaction.TransactionId);
                return false;
            }

            try
            {
                var refund = await _stripeService.RefundPaymentIntentAsync(
                    paymentIntentId,
                    $"refund_{transaction.TransactionId}",
                    "requested_by_customer",
                    new Dictionary<string, string>
                    {
                        ["transactionId"] = transaction.TransactionId.ToString(),
                        ["compensationReason"] = "subscription_activation_failed",
                        ["error"] = reason
                    });

                if (refund == null)
                {
                    return false;
                }

                transaction.Status = TransactionStatus.Refunded;
                transaction.StripePaymentIntentId = paymentIntentId;
                transaction.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Compensation refund succeeded on Stripe but failed to persist REFUNDED status for transaction {TransactionId}.", transaction.TransactionId);
                }

                _logger.LogWarning(
                    "Compensation refund succeeded for transaction {TransactionId}. RefundId={RefundId}",
                    transaction.TransactionId,
                    refund.Id);

                return true;
            }
            catch (Exception refundEx)
            {
                _logger.LogError(refundEx, "Compensation refund failed for transaction {TransactionId}", transaction.TransactionId);
                return false;
            }
        }

        private static SubscriptionPlanPriceDto MapPriceToDto(SubscriptionPlanPrice price)
        {
            var discountCurrentlyActive = price.IsDiscountPeriodActive();

            return new SubscriptionPlanPriceDto
            {
                PriceId = price.PriceId,
                BasePrice = price.BasePrice,
                DiscountedPrice = discountCurrentlyActive ? price.DiscountedPrice : null,
                EffectivePrice = price.GetEffectivePrice(),
                DiscountStart = discountCurrentlyActive ? price.DiscountStart : null,
                DiscountEnd = discountCurrentlyActive ? price.DiscountEnd : null,
                IsDiscountActive = discountCurrentlyActive,
                Currency = price.Currency
            };
        }

        public async Task<string> GetPaymentRedirectUrlAsync(string? sessionId, bool isSuccess)
        {
            var platform = "web";

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                try
                {
                    var session = await _stripeService.GetCheckoutSessionAsync(sessionId);
                    if (session?.Metadata != null && session.Metadata.TryGetValue("platform", out var p))
                        platform = p;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch Stripe session {SessionId} for redirect", sessionId);
                }
            }

            if (isSuccess)
            {
                var query = string.IsNullOrWhiteSpace(sessionId) ? string.Empty : $"?session_id={sessionId}";
                return platform == "mobile"
                    ? _stripeSettings.MobileSuccessUrl + query
                    : _stripeSettings.WebSuccessUrl + query;
            }
            else
            {
                return platform == "mobile"
                    ? _stripeSettings.MobileCancelUrl
                    : _stripeSettings.WebCancelUrl;
            }
        }
    }
}
