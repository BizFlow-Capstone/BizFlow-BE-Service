namespace BizFlow.Domain.Entities;

public partial class SaleItem
{
    public long SaleItemId { get; set; }

    public long ProductId { get; set; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public string Unit { get; set; } = null!;

    /// <summary>
    /// Quantity
    /// </summary>
    public int Quantity { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductPricePolicy> ProductPricePolicies { get; set; } = new List<ProductPricePolicy>();
}
