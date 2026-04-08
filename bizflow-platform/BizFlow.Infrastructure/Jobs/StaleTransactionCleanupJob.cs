using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class StaleTransactionCleanupJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StaleTransactionCleanupJob> _logger;

        public StaleTransactionCleanupJob(IUnitOfWork unitOfWork, ILogger<StaleTransactionCleanupJob> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            // Keep this as a final safety net. Faster healing is handled by Stripe reconcile jobs.
            var staleCutoff = DateTime.UtcNow.AddHours(-24);
            var staleTransactions = await _unitOfWork.Transactions.GetStalePendingTransactionsAsync(staleCutoff);

            foreach (var transaction in staleTransactions)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.UpdatedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("StaleTransactionCleanupJob marked {Count} stale transactions as failed", staleTransactions.Count);
        }
    }
}
