namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Response for product sale items
    /// </summary>
    public class ProductSaleItemsResponseDto
    {
        public long ProductId { get; set; }
        public List<SaleItemDto> SaleItems { get; set; } = new();
    }
}
