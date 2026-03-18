using BizFlow.Application.Common.Specifications;
using BizFlow.Application.DTOs.Debtor;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Specifications.Debtors
{
    public class DebtorSearchSpec : BaseSpecification<Debtor>
    {
        public DebtorSearchSpec(DebtorQueryParams query, IEnumerable<int>? allowedLocationIds = null, bool isCount = false, bool filterOnly = false)
            : base(d => d.DeletedAt == null)
        {
            if (allowedLocationIds != null && allowedLocationIds.Any())
            {
                AddCriteria(d => allowedLocationIds.Contains(d.BusinessLocationId));
            }

            if (query.IsActive.HasValue)
            {
                AddCriteria(d => d.IsActive == query.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                AddCriteria(d => d.Name.Contains(s) || (d.Phone != null && d.Phone.Contains(s)));
            }

            if (!isCount)
            {
                AddOrderByDescending(d => d.CreatedAt);
            }

            // Apply Paging (Only if NOT counting AND NOT filterOnly)
            if (!isCount && !filterOnly)
            {
                var pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;
                var pageSize = query.PageSize > 0 ? query.PageSize : 20;
                ApplyPaging((pageNumber - 1) * pageSize, pageSize);
            }
        }
    }
}
