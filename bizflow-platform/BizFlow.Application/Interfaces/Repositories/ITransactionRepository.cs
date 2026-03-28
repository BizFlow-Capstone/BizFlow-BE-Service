using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface ITransactionRepository
    {
        Task AddAsync(Transaction transaction);
        Task<Transaction?> GetByIdAsync(Guid transactionId);
        Task<Transaction?> GetByCheckoutSessionIdAsync(string checkoutSessionId);
        Task<Transaction?> GetByPaymentIntentIdAsync(string paymentIntentId);
        Task<List<Transaction>> GetByProfileAsync(Guid profileId, int page, int pageSize);
        Task<List<Transaction>> GetStalePendingTransactionsAsync(DateTime olderThanUtc);
    }
}
