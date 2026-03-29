using BizFlow.Application.Common.Specifications;
using BizFlow.Application.DTOs.Subscription;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Specifications.SubscriptionPlans
{
    /// <summary>
    /// Specification for searching/filtering subscription plans.
    /// Follows the same pattern as ProductSearchSpec.
    /// </summary>
    public class SubscriptionPlanSearchSpec : BaseSpecification<SubscriptionPlan>
    {
        public SubscriptionPlanSearchSpec(SubscriptionPlanQueryParams query, bool isCount = false, bool filterOnly = false)
        {
            // Filter by status
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                if (query.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                    AddCriteria(p => p.IsActive == true);
                else if (query.Status.Equals("archived", StringComparison.OrdinalIgnoreCase))
                    AddCriteria(p => p.IsActive == false);
                // "all" => no filter
            }

            // Search by name
            if (!string.IsNullOrWhiteSpace(query.Search))
                AddCriteria(p => p.Name.Contains(query.Search));

            // Date range filters
            if (query.CreatedAtGte.HasValue)
                AddCriteria(p => p.CreatedAt >= query.CreatedAtGte.Value);

            if (query.CreatedAtLte.HasValue)
                AddCriteria(p => p.CreatedAt <= query.CreatedAtLte.Value);

            // Sorting
            if (!isCount)
            {
                AddOrderByDescending(p => p.CreatedAt);
            }

            // Paging and Includes (only for full query, not count or filterOnly)
            if (!isCount && !filterOnly)
            {
                var pageNumber = query.PageNumber ?? 1;
                var pageSize = query.PageSize ?? 10;
                ApplyPaging((pageNumber - 1) * pageSize, pageSize);

                AddInclude("Prices");
                AddInclude("PlanFeatures.Feature");
            }
        }
    }
}
