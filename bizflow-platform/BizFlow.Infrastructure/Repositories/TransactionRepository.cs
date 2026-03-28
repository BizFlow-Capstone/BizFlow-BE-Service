using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly BizFlowDbContext _context;

        public TransactionRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public Task AddAsync(Transaction transaction)
        {
            return _context.Transactions.AddAsync(transaction).AsTask();
        }

        public Task<Transaction?> GetByIdAsync(Guid transactionId)
        {
            return _context.Transactions
                .Include(t => t.SubscriptionPlan)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public Task<Transaction?> GetByCheckoutSessionIdAsync(string checkoutSessionId)
        {
            return _context.Transactions
                .FirstOrDefaultAsync(t => t.StripeCheckoutSessionId == checkoutSessionId);
        }

        public Task<Transaction?> GetByPaymentIntentIdAsync(string paymentIntentId)
        {
            return _context.Transactions
                .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntentId);
        }

        public Task<List<Transaction>> GetByProfileAsync(Guid profileId, int page, int pageSize)
        {
            var safePage = Math.Max(page, 1);
            var safeSize = Math.Clamp(pageSize, 1, 100);

            return _context.Transactions
                .Include(t => t.SubscriptionPlan)
                .Where(t => t.ProfileId == profileId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((safePage - 1) * safeSize)
                .Take(safeSize)
                .ToListAsync();
        }

        public Task<List<Transaction>> GetStalePendingTransactionsAsync(DateTime olderThanUtc)
        {
            return _context.Transactions
                .Where(t => t.Status == TransactionStatus.Pending && t.CreatedAt <= olderThanUtc)
                .ToListAsync();
        }
    }
}
