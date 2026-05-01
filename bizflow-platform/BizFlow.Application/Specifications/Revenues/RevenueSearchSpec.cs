using BizFlow.Application.Common.Specifications;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Specifications.Revenues
{
    public class RevenueSearchSpec : BaseSpecification<Revenue>
    {
        public RevenueSearchSpec(RevenueQueryParams query, bool isCount, bool filterOnly = false)
            : base(r => r.BusinessLocationId == query.BusinessLocationId)
        {
            if (!string.IsNullOrWhiteSpace(query.RevenueType))
            {
                var type = query.RevenueType.Trim().ToLower();
                AddCriteria(r => r.RevenueType.ToLower() == type);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLower();
                AddCriteria(r => r.Status.ToLower() == status);
            }

            if (!string.IsNullOrWhiteSpace(query.MoneyChannel))
            {
                var channel = query.MoneyChannel.Trim().ToLower();
                AddCriteria(r => r.MoneyChannel != null && r.MoneyChannel.ToLower() == channel);
            }

            if (query.FromDate.HasValue)
                AddCriteria(r => r.RevenueDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                AddCriteria(r => r.RevenueDate <= query.ToDate.Value);

            if (!filterOnly)
            {
                // No specific includes for Revenue Search currently required
            }

            if (!isCount)
            {
                AddOrderByDescending(r => r.CreatedAt);
            }
        }
    }
}
