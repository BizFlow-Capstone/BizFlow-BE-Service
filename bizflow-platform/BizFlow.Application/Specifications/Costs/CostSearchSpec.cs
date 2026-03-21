using BizFlow.Application.Common.Specifications;
using BizFlow.Application.DTOs.Cost;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Specifications.Costs
{
    public class CostSearchSpec : BaseSpecification<Cost>
    {
        public CostSearchSpec(CostQueryParams query, bool isCount = false, bool filterOnly = false)
            : base(c => c.BusinessLocationId == query.BusinessLocationId)
        {
            if (!string.IsNullOrWhiteSpace(query.CostType))
            {
                var costType = query.CostType.Trim().ToLower();
                if (CostType.IsValid(costType))
                {
                    AddCriteria(c => c.CostType.ToLower() == costType);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.PaymentMethod))
            {
                var paymentMethod = query.PaymentMethod.Trim().ToLower();
                AddCriteria(c => c.PaymentMethod != null && c.PaymentMethod.ToLower() == paymentMethod);
            }

            if (query.FromDate.HasValue)
                AddCriteria(c => c.CostDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                AddCriteria(c => c.CostDate <= query.ToDate.Value);

            if (!isCount)
            {
                AddOrderByDescending(c => c.CreatedAt);
            }

            if (!isCount && !filterOnly)
            {
                var pageNumber = query.PageNumber ?? 1;
                var pageSize = query.PageSize ?? 20;
                ApplyPaging((pageNumber - 1) * pageSize, pageSize);
            }
        }
    }
}
