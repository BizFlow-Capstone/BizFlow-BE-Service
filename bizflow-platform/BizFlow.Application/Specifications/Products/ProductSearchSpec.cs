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
            // Unified search: match Name OR SKU
            if (!string.IsNullOrWhiteSpace(query.Search))
                AddCriteria(p => p.ProductName.Contains(query.Search)
                    || (p.Sku != null && p.Sku.Contains(query.Search)));

            // Specific filters (for advanced filtering)
            if (!string.IsNullOrWhiteSpace(query.Name))
                AddCriteria(p => p.ProductName.Contains(query.Name));

            if (!string.IsNullOrWhiteSpace(query.Sku))
                AddCriteria(p => p.Sku != null && p.Sku.Contains(query.Sku));

            if (query.MinSellingPrice.HasValue)
                AddCriteria(p => p.SellingPrice >= query.MinSellingPrice.Value);

            if (query.MaxSellingPrice.HasValue)
                AddCriteria(p => p.SellingPrice <= query.MaxSellingPrice.Value);

            if (query.MinStock.HasValue)
                AddCriteria(p => p.Stock >= query.MinStock.Value);

            if (query.MaxStock.HasValue)
                AddCriteria(p => p.Stock <= query.MaxStock.Value);

            if (!string.IsNullOrWhiteSpace(query.Status))
                AddCriteria(p => p.Status == query.Status.ToLower());

            if (query.TrackInventory.HasValue)
                AddCriteria(p => p.TrackInventory == query.TrackInventory.Value);

            if (query.BusinessTypeId.HasValue)
                AddCriteria(p => p.BusinessTypeId == query.BusinessTypeId.Value);

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
