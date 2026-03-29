namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IStripeWebhookEventRepository
    {
        Task<bool> TryCreateReceivedAsync(string eventId, string eventType, DateTime stripeCreatedAtUtc);
        Task MarkProcessedAsync(string eventId);
        Task MarkFailedAsync(string eventId, string error);
    }
}
