using BizFlow.Application.Common.Specifications;
using BizFlow.Application.DTOs.Product;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Specifications.Products
{
    public class ProductSearchSpec : BaseSpecification<Product>
    {
        public ProductSearchSpec(ProductQueryParams query, bool isCount = false, bool filterOnly = false)
            : base(p => p.BusinessLocationId == query.LocationId)
        {
            // Filters
            if (!string.IsNullOrWhiteSpace(query.Name))
                AddCriteria(p => p.ProductName.Contains(query.Name));

            if (!string.IsNullOrWhiteSpace(query.Sku))
                AddCriteria(p => p.Sku != null && p.Sku.Contains(query.Sku));

            if (query.MinCostPrice.HasValue)
                AddCriteria(p => p.CostPrice >= query.MinCostPrice.Value);

            if (query.MaxCostPrice.HasValue)
                AddCriteria(p => p.CostPrice <= query.MaxCostPrice.Value);

            if (query.MinStock.HasValue)
                AddCriteria(p => p.Stock >= query.MinStock.Value);

            if (query.MaxStock.HasValue)
                AddCriteria(p => p.Stock <= query.MaxStock.Value);

            if (!string.IsNullOrWhiteSpace(query.Status))
                AddCriteria(p => p.Status == query.Status.ToLower());

            if (query.TrackInventory.HasValue)
                AddCriteria(p => p.TrackInventory == query.TrackInventory.Value);

            // Apply Sorting (Needed for both Full and FilterOnly modes to ensure ID order matches)
            if (!isCount)
            {
                 AddOrderByDescending(p => p.ProductId);
            }

            // Apply Paging and Includes (Only if NOT counting AND NOT filterOnly)
            if (!isCount && !filterOnly)
            {
                // Paging
                var pageNumber = query.PageNumber ?? 1;
                var pageSize = query.PageSize ?? 10;
                ApplyPaging((pageNumber - 1) * pageSize, pageSize);

                // Includes
                // Using string include for nested path: SaleItems -> ProductPricePolicies
                AddInclude("SaleItems.ProductPricePolicies");
            }
        }
    }
}
