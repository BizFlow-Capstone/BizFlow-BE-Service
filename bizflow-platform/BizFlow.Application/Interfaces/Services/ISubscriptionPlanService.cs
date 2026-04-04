using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Subscription;

namespace BizFlow.Application.Interfaces.Services
{
    public interface ISubscriptionPlanService
    {
        // User
        Task<List<SubscriptionPlanDto>> GetActivePlansAsync();

        // Admin — raw DB values, includes Stripe IDs
        Task<PaginatedResponse<AdminSubscriptionPlanSummaryDto>> SearchPlansAsync(SubscriptionPlanQueryParams query);
        Task<AdminSubscriptionPlanDto?> GetPlanByIdAsync(int planId);
        Task<AdminSubscriptionPlanDto> CreatePlanAsync(CreateSubscriptionPlanRequest request);
        Task<AdminSubscriptionPlanDto> UpdatePlanAsync(int planId, UpdateSubscriptionPlanRequest request);
        Task DeletePlanAsync(int planId);
        Task<AdminSubscriptionPlanDto> SetPlanStatusAsync(int planId, bool isActive);
        Task<List<FeatureDto>> GetAllFeaturesAsync();

        /// <summary>
        /// Scheduled job: update discount flags from the UTC window and sync Stripe Price to the effective amount (base/discount).
        /// </summary>
        Task ReconcileDiscountWindowsAndStripeCatalogAsync();
    }
}
