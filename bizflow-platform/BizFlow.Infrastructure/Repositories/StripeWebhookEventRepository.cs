using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class StripeWebhookEventRepository : IStripeWebhookEventRepository
    {
        private readonly BizFlowDbContext _context;

        public StripeWebhookEventRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public async Task<bool> TryCreateReceivedAsync(string eventId, string eventType, DateTime stripeCreatedAtUtc)
        {
            var now = DateTime.UtcNow;

            var entity = new StripeWebhookEvent
            {
                EventId = eventId,
                EventType = eventType,
                StripeCreatedAt = stripeCreatedAtUtc,
                ReceivedAt = now,
                ProcessingStatus = "Received",
                AttemptCount = 1,
                UpdatedAt = now
            };

            _context.StripeWebhookEvents.Add(entity);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                var existing = await _context.StripeWebhookEvents
                    .FirstOrDefaultAsync(x => x.EventId == eventId);

                // If we previously failed processing this event, allow retry.
                if (existing != null && string.Equals(existing.ProcessingStatus, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    await _context.StripeWebhookEvents
                        .Where(x => x.EventId == eventId)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(x => x.ProcessingStatus, "Received")
                            .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                            .SetProperty(x => x.LastError, (string?)null)
                            .SetProperty(x => x.UpdatedAt, now));
                    return true;
                }

                await _context.StripeWebhookEvents
                    .Where(x => x.EventId == eventId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                        .SetProperty(x => x.UpdatedAt, now));
                return false;
            }
        }

        public async Task MarkProcessedAsync(string eventId)
        {
            var now = DateTime.UtcNow;
            await _context.StripeWebhookEvents
                .Where(x => x.EventId == eventId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.ProcessingStatus, "Processed")
                    .SetProperty(x => x.ProcessedAt, now)
                    .SetProperty(x => x.UpdatedAt, now)
                    .SetProperty(x => x.LastError, (string?)null));
        }

        public async Task MarkFailedAsync(string eventId, string error)
        {
            var now = DateTime.UtcNow;
            await _context.StripeWebhookEvents
                .Where(x => x.EventId == eventId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.ProcessingStatus, "Failed")
                    .SetProperty(x => x.LastError, error)
                    .SetProperty(x => x.UpdatedAt, now));
        }
    }
}
