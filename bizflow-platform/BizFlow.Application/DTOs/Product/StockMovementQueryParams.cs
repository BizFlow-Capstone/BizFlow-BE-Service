using BizFlow.Application.Common.Models;

namespace BizFlow.Application.DTOs.Product;

/// <summary>
/// Query stock movements for a single product (paginated, optional date range).
/// </summary>
public class StockMovementQueryParams : PaginationParams
{
    /// <summary>Inclusive start date (calendar day).</summary>
    public DateOnly? From { get; set; }

    /// <summary>Inclusive end date (calendar day).</summary>
    public DateOnly? To { get; set; }
}
