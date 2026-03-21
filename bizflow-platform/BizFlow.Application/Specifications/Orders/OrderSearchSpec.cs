using BizFlow.Application.Common.Specifications;
using BizFlow.Application.DTOs.Order;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Specifications.Orders
{
    public class OrderSearchSpec : BaseSpecification<Order>
    {
        public OrderSearchSpec(OrderQueryParams query, bool isCount, bool filterOnly = false)
            : base(o => o.OrderDetails.Any(od => od.SaleItem.Product.BusinessLocationId == query.BusinessLocationId))
        {
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLower();
                AddCriteria(o => o.Status.ToLower() == status);
            }

            if (query.FromDate.HasValue)
            {
                AddCriteria(o => o.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                AddCriteria(o => o.CreatedAt <= query.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                // Simple Contains search, DB should ignore case based on collation (e.g. utf8mb4_general_ci in MySQL/MariaDB)
                AddCriteria(o => o.OrderCode.Contains(s)
                    || (o.CustomerName != null && o.CustomerName.Contains(s))
                    || (o.CustomerPhone != null && o.CustomerPhone.Contains(s)));
            }

            if (!filterOnly)
            {
                // Typically you don't add includes in count spec, but filterOnly explicit guard is good
                AddInclude(x => x.OrderDetails);
                AddInclude("OrderDetails.SaleItem");
                AddInclude("OrderDetails.SaleItem.Product");
            }

            if (!isCount)
            {
                AddOrderByDescending(o => o.CreatedAt);
            }
        }
    }
}
