namespace BizFlow.Application.DTOs.Order
{
    public class OrderActionResultDto
    {
        public bool RequiresConfirmation { get; set; }
        public List<string> Warnings { get; set; } = new();
        public OrderDto? Order { get; set; }
    }
}
