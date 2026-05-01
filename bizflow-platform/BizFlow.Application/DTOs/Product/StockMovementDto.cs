namespace BizFlow.Application.DTOs.Product;

public class StockMovementDto
{
    public long StockMovementId { get; set; }
    public long ProductId { get; set; }
    public string MovementType { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public string? Memo { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; }
}
