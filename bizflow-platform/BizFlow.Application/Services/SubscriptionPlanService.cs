using BizFlow.Application.Common.Models;
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
        private readonly ILogger<SubscriptionPlanService> _logger;

        public SubscriptionPlanService(
            IUnitOfWork unitOfWork,
            IStripeService stripeService,
            ILogger<SubscriptionPlanService> logger)
        {
            _unitOfWork = unitOfWork;
            _stripeService = stripeService;
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
                plan.Prices.Add(new SubscriptionPlanPrice
                {
                    BasePrice = request.Price.BasePrice,
                    DiscountedPrice = request.Price.DiscountedPrice,
                    DiscountStart = request.Price.DiscountStart,
                    DiscountEnd = request.Price.DiscountEnd,
                    IsDiscountActive = request.Price.DiscountedPrice.HasValue,
                    IsActive = true,
                    Currency = "VND",
                    CreatedAt = now,
                    UpdatedAt = now
                });
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
                throw new KeyNotFoundException($"Subscription plan {planId} not found");

            var now = DateTime.UtcNow;
            plan.Name = request.Name;
            plan.Description = request.Description;
            plan.DurationDays = request.DurationDays;
            plan.UpdatedAt = now;

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

            if (request.Price != null)
            {
                var currentPrice = plan.Prices.FirstOrDefault(p => p.IsActive);
                if (IsPriceChanged(currentPrice, request.Price))
                {
                    await _unitOfWork.PlanPrices.DeactivateByPlanIdAsync(planId);

                    var newPrice = new SubscriptionPlanPrice
                    {
                        SubscriptionPlanId = planId,
                        BasePrice = request.Price.BasePrice,
                        DiscountedPrice = request.Price.DiscountedPrice,
                        DiscountStart = request.Price.DiscountStart,
                        DiscountEnd = request.Price.DiscountEnd,
                        IsDiscountActive = request.Price.DiscountedPrice.HasValue,
                        IsActive = true,
                        Currency = "VND",
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _unitOfWork.PlanPrices.AddAsync(newPrice);

                    _logger.LogInformation("Price changed for plan {PlanId}: BasePrice={BasePrice}", planId, request.Price.BasePrice);
                }
            }

            if (plan.IsActive == true && _stripeService.IsConfigured && !string.IsNullOrWhiteSpace(plan.StripeProductId))
            {
                await _stripeService.UpdateProductAsync(plan.StripeProductId, plan.Name, plan.Description);
                _logger.LogInformation("Stripe product {ProductId} synced after update", plan.StripeProductId);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated subscription plan {PlanId}: {Name}", planId, plan.Name);

            var updated = await _unitOfWork.SubscriptionPlans.GetByIdWithFeaturesAndPriceAsync(planId);
            return MapToAdminDto(updated!);
        }

        public async Task DeletePlanAsync(int planId)
        {
            var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(planId);
            if (plan == null)
                throw new KeyNotFoundException($"Subscription plan {planId} not found");

            var hasActive = await _unitOfWork.SubscriptionPlans.HasActiveSubscriptionsAsync(planId);
            if (hasActive)
                throw new InvalidOperationException("Cannot delete plan with active subscriptions");

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
                throw new KeyNotFoundException($"Subscription plan {planId} not found");

            if (isActive)
            {
                var activePrice = plan.Prices.FirstOrDefault(p => p.IsActive);
                if (activePrice == null)
                    throw new InvalidOperationException("Cannot activate plan without a price");

                if (!plan.PlanFeatures.Any())
                    throw new InvalidOperationException("Cannot activate plan without features");

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
                        await _stripeService.UpdateProductAsync(plan.StripeProductId, plan.Name, plan.Description);
                        _logger.LogInformation("Stripe product {ProductId} reactivated", plan.StripeProductId);
                    }
                }
            }
            else
            {
                if (_stripeService.IsConfigured && !string.IsNullOrWhiteSpace(plan.StripeProductId))
                {
                    await _stripeService.ArchiveProductAsync(plan.StripeProductId);
                    _logger.LogInformation("Stripe product {ProductId} archived", plan.StripeProductId);
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

        // ===================== Helpers =====================

        private static bool IsPriceChanged(SubscriptionPlanPrice? current, UpdatePlanPriceRequest requested)
        {
            if (current == null) return true;
            return current.BasePrice != requested.BasePrice
                || current.DiscountedPrice != requested.DiscountedPrice
                || current.DiscountStart != requested.DiscountStart
                || current.DiscountEnd != requested.DiscountEnd;
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
            var now = DateTime.UtcNow;
            var discountCurrentlyActive = price.IsDiscountActive
                && price.DiscountedPrice.HasValue
                && (price.DiscountStart == null || now >= price.DiscountStart)
                && (price.DiscountEnd == null || now <= price.DiscountEnd);

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

            return new AdminSubscriptionPlanSummaryDto
            {
                SubscriptionPlanId = plan.SubscriptionPlanId,
                Name = plan.Name,
                IsActive = plan.IsActive ?? true,
                DurationDays = plan.DurationDays,
                BasePrice = activePrice?.BasePrice,
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
                UsageLimit = pf.UsageLimit
            }).ToList();
        }
    }
}
