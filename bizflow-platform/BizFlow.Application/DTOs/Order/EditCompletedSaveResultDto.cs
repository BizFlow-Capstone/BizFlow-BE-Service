namespace BizFlow.Application.DTOs.Order
{
    public class EditCompletedSaveResultDto
    {
        public long OldOrderId { get; set; }
        public string OldOrderStatus { get; set; } = null!;
        public long NewOrderId { get; set; }
        public string NewOrderStatus { get; set; } = null!;
    }
}
