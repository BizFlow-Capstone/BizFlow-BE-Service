namespace BizFlow.Application.DTOs.Order
{
    public class CompleteOrderRequest
    {
        public bool ConfirmLowStock { get; set; } = false;
    }
}
