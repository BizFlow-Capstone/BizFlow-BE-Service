namespace BizFlow.Domain.Entities;

public partial class ProductImport
{
    public long ImportId { get; set; }

    public long ProductId { get; set; }

    /// <summary>
    /// Import quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Total price
    /// </summary>
    public decimal TotalPrice { get; set; }

    public virtual Import Import { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
