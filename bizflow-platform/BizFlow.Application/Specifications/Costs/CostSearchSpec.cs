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
            if (!query.IncludeReversal)
                AddCriteria(c => !c.IsReversal);

            if (!string.IsNullOrWhiteSpace(query.CostType))
            {
                var costType = query.CostType.Trim().ToLower();
                if (CostType.IsValid(costType))
                {
                    AddCriteria(c => c.CostType.ToLower() == costType);
                }
            }

            if (query.BusinessTypeId.HasValue)
            {
                var btId = query.BusinessTypeId.Value;
                AddCriteria(c => c.BusinessTypeId == btId);
            }

            if (!string.IsNullOrWhiteSpace(query.PaymentMethod))
            {
                var paymentMethod = query.PaymentMethod.Trim().ToLower();
                AddCriteria(c => c.PaymentMethod != null && c.PaymentMethod.ToLower() == paymentMethod);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLower();
                AddCriteria(c => c.Status.ToLower() == status);
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
