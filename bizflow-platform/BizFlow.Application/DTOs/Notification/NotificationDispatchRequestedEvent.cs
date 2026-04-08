namespace BizFlow.Application.DTOs.Notification
{
    public class NotificationDispatchRequestedEvent
    {
        public long DispatchId { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}