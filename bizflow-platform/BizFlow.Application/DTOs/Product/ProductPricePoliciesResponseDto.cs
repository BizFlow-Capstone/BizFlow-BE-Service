namespace BizFlow.Application.DTOs.Product;

/// <summary>
/// Price policies for a product, grouped by sale item (unit tier).
/// </summary>
public class ProductPricePoliciesResponseDto
{
    public long ProductId { get; set; }
    public List<SaleItemPricePoliciesDto> SaleItems { get; set; } = new();
}

public class SaleItemPricePoliciesDto
{
    public long SaleItemId { get; set; }
    public string Unit { get; set; } = null!;
    public decimal Quantity { get; set; }
    public List<ProductPricePolicyItemDto> PricePolicies { get; set; } = new();
}

public class ProductPricePolicyItemDto
{
    public long ProductPricePolicyId { get; set; }
    public decimal Price { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
}
