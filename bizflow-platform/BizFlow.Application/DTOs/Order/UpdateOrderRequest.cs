namespace BizFlow.Application.DTOs.Order
{
    public class UpdateOrderRequest : CreateOrderRequest
    {
        public string? IdempotencyKey { get; set; }
    }
}
