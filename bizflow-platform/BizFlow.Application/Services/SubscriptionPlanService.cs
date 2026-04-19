using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
using BizFlow.Application.Common.Validation;
using BizFlow.Application.DTOs.Subscription;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BizFlow.Application.Services
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStripeService _stripeService;
        private readonly IFirestoreService _firestoreService;
        private readonly ILogger<SubscriptionPlanService> _logger;

        public SubscriptionPlanService(
            IUnitOfWork unitOfWork,
            IStripeService stripeService,
            IFirestoreService firestoreService,
            ILogger<SubscriptionPlanService> logger)
        {
            _unitOfWork = unitOfWork;
            _stripeService = stripeService;
            _firestoreService = firestoreService;
            _logger = logger;
        }

        // ===================== User =====================

        public async Task<List<SubscriptionPlanDto>> GetActivePlansAsync()
        {
            var plans = await _unitOfWork.SubscriptionPlans.GetActivePlansWithFeaturesAsync();
            return plans.Select(MapToUserDto).ToList();
        }

        // ===================== Admin =====================

        public async Task<PaginatedResponse<AdminSubscriptionPlanSummaryDto>> SearchPlansAsync(SubscriptionPlanQueryParams query)
        {
            var (items, totalCount) = await _unitOfWork.SubscriptionPlans.SearchAsync(query);
            var dtos = items.Select(MapToAdminSummaryDto).ToList();

            return new PaginatedResponse<AdminSubscriptionPlanSummaryDto>(
                dtos,
                totalCount,
                query.PageNumber ?? 1,
                query.PageSize ?? 10);
        }

        public async Task<AdminSubscriptionPlanDto?> GetPlanByIdAsync(int planId)
        {
            var plan = await _unitOfWork.SubscriptionPlans.GetByIdWithFeaturesAndPriceAsync(planId);
            return plan == null ? null : MapToAdminDto(plan);
        }

        public async Task<AdminSubscriptionPlanDto> CreatePlanAsync(CreateSubscriptionPlanRequest request)
        {
            await EnsurePlanFeatureIdsExistAsync(request.Features);

            var now = DateTime.UtcNow;

            var plan = new SubscriptionPlan
            {
                Name = request.Name,
                Description = request.Description,
                DurationDays = request.DurationDays,
                IsActive = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var f in request.Features)
            {
                plan.PlanFeatures.Add(new PlanFeature
                {
                    FeatureId = f.FeatureId,
                    UsageLimit = f.UsageLimit,
                    CreatedAt = now
                });
            }

            if (request.Price != null)
            {
                PlanPriceDiscountRules.ValidateOrThrow(
                    request.Price.DiscountedPrice,
                    request.Price.DiscountStart,
                    request.Price.DiscountEnd);

                var discounted = request.Price.DiscountedPrice;
                var row = new SubscriptionPlanPrice
            {
                BasePrice = request.Price.BasePrice,
                    DiscountedPrice = discounted,
                    DiscountStart = discounted.HasValue ? request.Price.DiscountStart : null,
                    DiscountEnd = discounted.HasValue ? request.Price.DiscountEnd : null,
                    IsActive = true,
                Currency = "VND",
                CreatedAt = now,
                UpdatedAt = now
                };
                row.IsDiscountActive = row.IsDiscountPeriodActive(now);
                plan.Prices.Add(row);
            }

            await _unitOfWork.SubscriptionPlans.AddAsync(plan);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created subscription plan {PlanId}: {Name} (draft)", plan.SubscriptionPlanId, plan.Name);

            var created = await _unitOfWork.SubscriptionPlans.GetByIdWithFeaturesAndPriceAsync(plan.SubscriptionPlanId);
            return MapToAdminDto(created!);
        }

        public async Task<AdminSubscriptionPlanDto> UpdatePlanAsync(int planId, UpdateSubscriptionPlanRequest request)
        {
            var plan = await _unitOfWork.SubscriptionPlans.GetByIdWithFeaturesAndPriceAsync(planId);
            if (plan == null)
                throw new NotFoundException(MessageKeys.SubscriptionPlanNotFound);

            await EnsurePlanFeatureIdsExistAsync(request.Features);

            var now = DateTime.UtcNow;
            var previousDurationDays = plan.DurationDays;
            plan.Name = request.Name;
            plan.Description = request.Description;
            plan.DurationDays = request.DurationDays;
            plan.UpdatedAt = now;

            var previousUsageLimitByFeatureId = plan.PlanFeatures.ToDictionary(pf => pf.FeatureId, pf => pf.UsageLimit);

            plan.PlanFeatures.Clear();
            foreach (var f in request.Features)
            {
                plan.PlanFeatures.Add(new PlanFeature
                {
                    SubscriptionPlanId = planId,
                    FeatureId = f.FeatureId,
                    UsageLimit = f.UsageLimit,
                    CreatedAt = now
                });
            }

            await ResyncActiveSubscriptionsFeatureAllocationsAsync(
                planId,
                plan.PlanFeatures.ToList(),
                previousUsageLimitByFeatureId,
                previousDurationDays,
                now);

            var priceRowReplaced = false;
            if (request.Price != null)
            {
                PlanPriceDiscountRules.ValidateOrThrow(
                    request.Price.DiscountedPrice,
                    request.Price.DiscountStart,
                    request.Price.DiscountEnd);

                var currentPrice = plan.Prices.FirstOrDefault(p => p.IsActive);
                if (IsPriceChanged(currentPrice, request.Price))
                {
                    await _unitOfWork.PlanPrices.DeactivateByPlanIdAsync(planId);
                    // ExecuteUpdateAsync does not update tracked entities;
                    // sync local state to avoid SaveChanges overwriting DB values.
                    foreach (var p in plan.Prices.Where(x => x.IsActive).ToList())
                    {
                        p.IsActive = false;
                        p.UpdatedAt = now;
                    }

                    var discounted = request.Price.DiscountedPrice;
                    var newPrice = new SubscriptionPlanPrice
                    {
                        SubscriptionPlanId = planId,
                        BasePrice = request.Price.BasePrice,
                        DiscountedPrice = discounted,
                        DiscountStart = discounted.HasValue ? request.Price.DiscountStart : null,
                        DiscountEnd = discounted.HasValue ? request.Price.DiscountEnd : null,
                        IsActive = true,
                        Currency = "VND",
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    newPrice.IsDiscountActive = newPrice.IsDiscountPeriodActive(now);
                    await _unitOfWork.PlanPrices.AddAsync(newPrice);
                    priceRowReplaced = true;

                    _logger.LogInformation("Price changed for plan {PlanId}: BasePrice={BasePrice}", planId, request.Price.BasePrice);
                }
            }

            if (plan.IsActive == true && _stripeService.IsConfigured && !string.IsNullOrWhiteSpace(plan.StripeProductId))
            {
                await _stripeService.UpdateProductAsync(plan.StripeProductId, plan.Name, plan.Description, active: true);
                _logger.LogInformation("Stripe product {ProductId} synced after update", plan.StripeProductId);
            }

            if (priceRowReplaced && plan.IsActive == true && _stripeService.IsConfigured && !string.IsNullOrWhiteSpace(plan.StripeProductId))
            {
                var activePrice = plan.Prices.FirstOrDefault(p => p.IsActive);
                if (activePrice != null)
                    await EnsureStripePriceMatchesPlanAsync(plan, activePrice);
                _logger.LogInformation("Stripe price synced for plan {PlanId}: StripePriceId={PriceId}", planId, plan.StripePriceId);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated subscription plan {PlanId}: {Name}", planId, plan.Name);

            await RefreshFirestoreUsageTrackingForPlanAsync(planId);

            var updated = await _unitOfWork.SubscriptionPlans.GetByIdWithFeaturesAndPriceAsync(planId);
            return MapToAdminDto(updated!);
        }

        public async Task DeletePlanAsync(int planId)
        {
            var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(planId);
            if (plan == null)
                throw new NotFoundException(MessageKeys.SubscriptionPlanNotFound);

            var hasActive = await _unitOfWork.SubscriptionPlans.HasActiveSubscriptionsAsync(planId);
            if (hasActive)
                throw new BadRequestException(MessageKeys.SubscriptionPlanDeleteHasActiveSubscriptions);

            var hasSyncedToStripe = !string.IsNullOrWhiteSpace(plan.StripeProductId);

            if (hasSyncedToStripe)
            {
                if (_stripeService.IsConfigured)
                {
                    await _stripeService.ArchiveProductAsync(plan.StripeProductId!);
                    _logger.LogInformation("Stripe product {ProductId} archived", plan.StripeProductId);
                }

                plan.IsActive = false;
                plan.DeletedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Soft-deleted subscription plan {PlanId}", planId);
            }
            else
            {
            _unitOfWork.SubscriptionPlans.Delete(plan);
            await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Hard-deleted subscription plan {PlanId}", planId);
            }
        }

        public async Task<AdminSubscriptionPlanDto> SetPlanStatusAsync(int planId, bool isActive)
        {
            var plan = await _unitOfWork.SubscriptionPlans.GetByIdWithFeaturesAndPriceAsync(planId);
            if (plan == null)
                throw new NotFoundException(MessageKeys.SubscriptionPlanNotFound);

            if (isActive)
            {
                var activePrice = plan.Prices.FirstOrDefault(p => p.IsActive);
                if (activePrice == null)
                    throw new BadRequestException(MessageKeys.SubscriptionPlanActivateRequiresPrice);

                if (string.IsNullOrWhiteSpace(plan.Description))
                    throw new BadRequestException(MessageKeys.SubscriptionPlanActivateRequiresDescription);

                if (!plan.PlanFeatures.Any())
                    throw new BadRequestException(MessageKeys.SubscriptionPlanActivateRequiresFeatures);

                if (_stripeService.IsConfigured)
                {
                    if (string.IsNullOrWhiteSpace(plan.StripeProductId))
                    {
                        var product = await _stripeService.CreateProductAsync(
                            plan.Name,
                            plan.Description,
                            new Dictionary<string, string> { ["source"] = "bizflow", ["planId"] = planId.ToString() });
                        plan.StripeProductId = product.Id;

                        var unitAmount = (long)activePrice.GetEffectivePrice();
                        if (unitAmount > 0)
                        {
                            var price = await _stripeService.CreatePriceAsync(product.Id, unitAmount);
                            plan.StripePriceId = price.Id;
                        }

                        _logger.LogInformation("Stripe synced — Product={ProductId}, Price={PriceId}", plan.StripeProductId, plan.StripePriceId);
                    }
                    else
                    {
                        await _stripeService.UpdateProductAsync(plan.StripeProductId, plan.Name, plan.Description, active: true);
                        await EnsureStripePriceMatchesPlanAsync(plan, activePrice);
                        _logger.LogInformation(
                            "Stripe product {ProductId} set active; price={PriceId}",
                            plan.StripeProductId,
                            plan.StripePriceId);
                    }
                }
            }
            else
            {
                if (_stripeService.IsConfigured && !string.IsNullOrWhiteSpace(plan.StripeProductId))
                {
                    if (!string.IsNullOrWhiteSpace(plan.StripePriceId))
                    {
                        await _stripeService.ArchivePriceAsync(plan.StripePriceId);
                        _logger.LogInformation("Stripe price {PriceId} archived for plan {PlanId}", plan.StripePriceId, planId);
                    }

                    await _stripeService.ArchiveProductAsync(plan.StripeProductId);
                    _logger.LogInformation("Stripe product {ProductId} archived for plan {PlanId}", plan.StripeProductId, planId);
                }
            }

            plan.IsActive = isActive;
            plan.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Set plan {PlanId} status to {IsActive}", planId, isActive);
            return MapToAdminDto(plan);
        }

        public async Task<List<FeatureDto>> GetAllFeaturesAsync()
        {
            var features = await _unitOfWork.Features.GetAllAsync();
            return features.Select(f => new FeatureDto
            {
                FeatureId = f.FeatureId,
                FeatureCode = f.FeatureCode,
                Name = f.Name,
                Description = f.Description
            }).ToList();
        }

        public async Task ReconcileDiscountWindowsAndStripeCatalogAsync()
        {
            var now = DateTime.UtcNow;
            var plans = await _unitOfWork.SubscriptionPlans.GetActivePlansWithFeaturesAsync();

            foreach (var plan in plans)
            {
                var activePrice = plan.Prices.FirstOrDefault(p => p.IsActive);
                if (activePrice == null)
                    continue;

                var inWindow = activePrice.IsDiscountPeriodActive(now);
                if (activePrice.IsDiscountActive != inWindow)
                {
                    activePrice.IsDiscountActive = inWindow;
                    activePrice.UpdatedAt = now;
                }
            }

            if (!_stripeService.IsConfigured)
            {
                await _unitOfWork.SaveChangesAsync();
                return;
            }

            var withStripe = plans.Where(p => !string.IsNullOrWhiteSpace(p.StripeProductId)).ToList();
            foreach (var plan in withStripe)
            {
                var activePrice = plan.Prices.FirstOrDefault(p => p.IsActive);
                if (activePrice == null)
                    continue;

                try
                {
                    await EnsureStripePriceMatchesPlanAsync(plan, activePrice);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Stripe catalog reconcile failed for plan {PlanId}", plan.SubscriptionPlanId);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "ReconcileDiscountWindowsAndStripeCatalog: {PlanCount} active plan(s), {StripeCount} with Stripe",
                plans.Count,
                withStripe.Count);
        }

        // ===================== Helpers =====================

        /// <summary>
        /// When admin updates <see cref="PlanFeature.UsageLimit"/> (or feature list),
        /// subscriber <see cref="FeatureUsage.AllocatedLimit"/> values remain purchase-time snapshots.
        /// This recalculates allocations so /subscriptions/current and Firestore match the new template
        /// while preserving stacked quantity behavior when old limits were positive.
        /// </summary>
        private async Task ResyncActiveSubscriptionsFeatureAllocationsAsync(
            int planId,
            IReadOnlyList<PlanFeature> newTemplate,
            Dictionary<int, int> previousUsageLimitByFeatureId,
            int previousDurationDays,
            DateTime now)
        {
            var newFeatureIds = newTemplate.Select(pf => pf.FeatureId).ToHashSet();
            var subs = await _unitOfWork.Subscriptions.GetActiveByPlanIdWithUsagesAsync(planId);

            foreach (var sub in subs)
            {
                var stackedQty = InferStackedQuantityFromSubscriptionPeriod(sub, previousDurationDays);

                foreach (var u in sub.FeatureUsages.Where(x => !newFeatureIds.Contains(x.FeatureId)).ToList())
                {
                    _unitOfWork.FeatureUsages.Remove(u);
                }

                foreach (var pf in newTemplate)
                {
                    var hadPreviousLimit = previousUsageLimitByFeatureId.TryGetValue(pf.FeatureId, out var oldLimitVal);
                    int? oldLimit = hadPreviousLimit ? oldLimitVal : null;
                    var newLimit = pf.UsageLimit;
                    var u = sub.FeatureUsages.FirstOrDefault(x => x.FeatureId == pf.FeatureId);

                    var newAllocated = ComputeNewAllocatedLimit(newLimit, oldLimit, u, stackedQty);
                    if (u == null)
                    {
                        await _unitOfWork.FeatureUsages.AddRangeAsync(new[]
                        {
                            new FeatureUsage
                            {
                                SubscriptionId = sub.SubscriptionId,
                                FeatureId = pf.FeatureId,
                                UsedCount = 0,
                                AllocatedLimit = newAllocated,
                                PeriodStart = sub.StartDate,
                                PeriodEnd = sub.EndDate,
                                UpdatedAt = now
                            }
                        });
                    }
                    else
                    {
                        u.AllocatedLimit = newAllocated;
                        u.UpdatedAt = now;
                    }
                }
            }

            if (subs.Count > 0)
            {
                _logger.LogInformation(
                    "Resynced feature allocations for plan {PlanId} on {SubCount} active subscription(s)",
                    planId,
                    subs.Count);
            }
        }

        /// <summary>
        /// Infers stacked quantity from subscription period:
        /// <c>EndDate - StartDate ≈ DurationDays × quantity</c> for same-plan purchase/renew stacking.
        /// Uses <paramref name="previousDurationDays"/> (before admin edits) to keep calculations stable
        /// when duration is changed in the same update request.
        /// </summary>
        private static int InferStackedQuantityFromSubscriptionPeriod(Subscription sub, int previousDurationDays)
        {
            var unitDays = Math.Max(1, previousDurationDays);
            var spanDays = (sub.EndDate - sub.StartDate).TotalDays;
            if (spanDays <= 0)
                return 1;

            var q = (int)Math.Round(spanDays / unitDays, MidpointRounding.AwayFromZero);
            return Math.Max(1, q);
        }

        private static int ComputeNewAllocatedLimit(int newLimit, int? oldLimit, FeatureUsage? u, int stackedQuantity)
        {
            if (newLimit == -1)
                return -1;

            var qty = Math.Max(1, stackedQuantity);

            if (u == null)
                return newLimit * qty;

            var fromUnlimitedOrMissingTemplate =
                !oldLimit.HasValue || oldLimit.Value == -1 || u.AllocatedLimit == -1;

            if (fromUnlimitedOrMissingTemplate)
                return newLimit * qty;

            var old = oldLimit!.Value;
            if (old <= 0)
                return newLimit * qty;

            var qtyFromAllocation = Math.Max(1, (int)Math.Ceiling((double)u.AllocatedLimit / old));
            return newLimit * qtyFromAllocation;
        }

        private async Task RefreshFirestoreUsageTrackingForPlanAsync(int planId)
        {
            var plan = await _unitOfWork.SubscriptionPlans.GetByIdWithFeaturesAndPriceAsync(planId);
            if (plan?.PlanFeatures == null || plan.PlanFeatures.Count == 0)
                return;

            var templateFeatures = plan.PlanFeatures.Where(pf => pf.Feature != null).ToList();
            if (templateFeatures.Count == 0)
                return;

            var subs = await _unitOfWork.Subscriptions.GetActiveByPlanIdWithUsagesAsync(planId);
            foreach (var sub in subs)
            {
                var existingUsed = sub.FeatureUsages
                    .Where(u => u.Feature != null)
                    .ToDictionary(u => u.Feature!.FeatureCode, u => u.UsedCount);

                try
                {
                    await _firestoreService.SetUsageTrackingAsync(
                        sub.OwnerProfileId,
                        sub,
                        templateFeatures,
                        existingUsed.Count > 0 ? existingUsed : null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Firestore usage refresh skipped for owner {OwnerId} after plan {PlanId} update",
                        sub.OwnerProfileId,
                        planId);
                }
            }
        }

        private async Task EnsurePlanFeatureIdsExistAsync(IEnumerable<PlanFeatureRequest> features)
        {
            var ids = features.Select(f => f.FeatureId).Distinct().ToList();
            if (ids.Count == 0)
                return;

            var existing = await _unitOfWork.Features.GetExistingIdsAsync(ids);
            foreach (var id in ids)
            {
                if (!existing.Contains(id))
                    throw new BadRequestException(MessageKeys.SubscriptionPlanFeatureNotFound, null, id);
            }
        }

        private static bool IsPriceChanged(SubscriptionPlanPrice? current, UpdatePlanPriceRequest requested)
        {
            if (current == null) return true;
            var discounted = requested.DiscountedPrice;
            var normalizedStart = discounted.HasValue ? requested.DiscountStart : null;
            var normalizedEnd = discounted.HasValue ? requested.DiscountEnd : null;
            return current.BasePrice != requested.BasePrice
                || current.DiscountedPrice != discounted
                || current.DiscountStart != normalizedStart
                || current.DiscountEnd != normalizedEnd;
        }

        /// <summary>
        /// Syncs Stripe Price with DB effective price:
        /// archive/create on amount change, reactivate when amount matches.
        /// </summary>
        private async Task EnsureStripePriceMatchesPlanAsync(SubscriptionPlan plan, SubscriptionPlanPrice activePrice)
        {
            if (!_stripeService.IsConfigured || string.IsNullOrWhiteSpace(plan.StripeProductId))
                return;

            var unitAmount = (long)activePrice.GetEffectivePrice();
            if (unitAmount <= 0)
            {
                _logger.LogWarning("Skip Stripe price sync for plan {PlanId}: effective amount is 0", plan.SubscriptionPlanId);
                return;
            }

            if (string.IsNullOrWhiteSpace(plan.StripePriceId))
            {
                var validProductId = await EnsureStripeProductExistsAsync(plan);
                var created = await _stripeService.CreatePriceAsync(validProductId, unitAmount);
                plan.StripePriceId = created.Id;
                return;
            }

            var stripePrice = await _stripeService.GetPriceAsync(plan.StripePriceId);
            if (stripePrice == null)
            {
                var created = await _stripeService.CreatePriceAsync(plan.StripeProductId, unitAmount);
                plan.StripePriceId = created.Id;
                return;
            }

            var stripeUnit = stripePrice.UnitAmount ?? 0;
            if (stripeUnit == unitAmount)
            {
                if (!stripePrice.Active)
                    await _stripeService.ReactivatePriceAsync(plan.StripePriceId);
                return;
            }

            if (stripePrice.Active)
                await _stripeService.ArchivePriceAsync(plan.StripePriceId);

            var ensuredProductId = await EnsureStripeProductExistsAsync(plan);
            var newStripePrice = await _stripeService.CreatePriceAsync(ensuredProductId, unitAmount);
            plan.StripePriceId = newStripePrice.Id;
        }

        /// <summary>
        /// Heals stale StripeProductId references (e.g. product deleted manually on Stripe dashboard).
        /// </summary>
        private async Task<string> EnsureStripeProductExistsAsync(SubscriptionPlan plan)
        {
            if (string.IsNullOrWhiteSpace(plan.StripeProductId))
                throw new BadRequestException(MessageKeys.BadRequest);

            try
            {
                await _stripeService.UpdateProductAsync(plan.StripeProductId, plan.Name, plan.Description, active: true);
                return plan.StripeProductId;
            }
            catch (Stripe.StripeException ex) when (string.Equals(ex.StripeError?.Code, "resource_missing", StringComparison.OrdinalIgnoreCase))
            {
                var product = await _stripeService.CreateProductAsync(
                    plan.Name,
                    plan.Description,
                    new Dictionary<string, string>
                    {
                        ["source"] = "bizflow-heal",
                        ["planId"] = plan.SubscriptionPlanId.ToString()
                    });

                plan.StripeProductId = product.Id;
                plan.StripePriceId = null;
                plan.UpdatedAt = DateTime.UtcNow;
                _logger.LogWarning(
                    "Recreated missing Stripe product for plan {PlanId}. NewProductId={ProductId}",
                    plan.SubscriptionPlanId,
                    plan.StripeProductId);
                return product.Id;
            }
        }

        // ===================== User Mapping =====================

        private static SubscriptionPlanDto MapToUserDto(SubscriptionPlan plan)
        {
            var latestPrice = plan.Prices.FirstOrDefault(p => p.IsActive);

            return new SubscriptionPlanDto
            {
                SubscriptionPlanId = plan.SubscriptionPlanId,
                Name = plan.Name,
                Description = plan.Description,
                DurationDays = plan.DurationDays,
                CurrentPrice = latestPrice != null ? MapToUserPriceDto(latestPrice) : null,
                Features = MapFeatures(plan)
            };
        }

        private static SubscriptionPlanPriceDto MapToUserPriceDto(SubscriptionPlanPrice price)
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

        // ===================== Admin Mapping =====================

        private static AdminSubscriptionPlanSummaryDto MapToAdminSummaryDto(SubscriptionPlan plan)
        {
            var activePrice = plan.Prices.FirstOrDefault(p => p.IsActive);
            var discountActive = activePrice != null && activePrice.IsDiscountPeriodActive();

            return new AdminSubscriptionPlanSummaryDto
            {
                SubscriptionPlanId = plan.SubscriptionPlanId,
                Name = plan.Name,
                IsActive = plan.IsActive ?? true,
                DurationDays = plan.DurationDays,
                BasePrice = activePrice?.BasePrice,
                DiscountedPrice = activePrice?.DiscountedPrice,
                DiscountStart = activePrice?.DiscountStart,
                DiscountEnd = activePrice?.DiscountEnd,
                IsDiscountPeriodActive = discountActive,
                Currency = activePrice?.Currency ?? "VND",
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt
            };
        }

        private static AdminSubscriptionPlanDto MapToAdminDto(SubscriptionPlan plan)
        {
            var latestPrice = plan.Prices.FirstOrDefault(p => p.IsActive);

            return new AdminSubscriptionPlanDto
            {
                SubscriptionPlanId = plan.SubscriptionPlanId,
                Name = plan.Name,
                Description = plan.Description,
                DurationDays = plan.DurationDays,
                IsActive = plan.IsActive ?? true,
                StripeProductId = plan.StripeProductId,
                StripePriceId = plan.StripePriceId,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt,
                CurrentPrice = latestPrice != null ? MapToAdminPriceDto(latestPrice) : null,
                Features = MapFeatures(plan)
            };
        }

        private static AdminSubscriptionPlanPriceDto MapToAdminPriceDto(SubscriptionPlanPrice price)
        {
            return new AdminSubscriptionPlanPriceDto
            {
                PriceId = price.PriceId,
                BasePrice = price.BasePrice,
                DiscountedPrice = price.DiscountedPrice,
                DiscountStart = price.DiscountStart,
                DiscountEnd = price.DiscountEnd,
                IsDiscountActive = price.IsDiscountPeriodActive(),
                IsActive = price.IsActive,
                Currency = price.Currency,
                CreatedAt = price.CreatedAt,
                UpdatedAt = price.UpdatedAt
            };
        }

        // ===================== Shared Mapping =====================

        private static List<PlanFeatureDto> MapFeatures(SubscriptionPlan plan)
        {
            return plan.PlanFeatures.Select(pf => new PlanFeatureDto
            {
                FeatureId = pf.FeatureId,
                FeatureCode = pf.Feature?.FeatureCode ?? string.Empty,
                FeatureName = pf.Feature?.Name ?? string.Empty,
                UsageLimit = pf.UsageLimit,
                FeatureDescription = pf.Feature?.Description ?? string.Empty
            }).ToList();
        }
    }
}
