using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Common.Models;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    /// <summary>
    /// Reconciles stale pending transactions against Stripe authoritative state.
    /// </summary>
    public class StripePendingReconcileJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStripeService _stripeService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<StripePendingReconcileJob> _logger;

        public StripePendingReconcileJob(
            IUnitOfWork unitOfWork,
            IStripeService stripeService,
            ISubscriptionService subscriptionService,
            ILogger<StripePendingReconcileJob> logger)
        {
            _unitOfWork = unitOfWork;
            _stripeService = stripeService;
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var staleCutoff = DateTime.UtcNow.AddMinutes(-30);
            var stalePendingTransactions = await _unitOfWork.Transactions.GetStalePendingTransactionsAsync(staleCutoff);

            var markedFailed = 0;
            var healedToSuccess = 0;

            foreach (var transaction in stalePendingTransactions)
            {
                if (!string.IsNullOrWhiteSpace(transaction.StripeCheckoutSessionId))
                {
                    var statusBeforeReplay = transaction.Status;
                    var session = await _stripeService.GetCheckoutSessionAsync(transaction.StripeCheckoutSessionId);
                    if (session == null)
                    {
                        continue;
                    }

                    if (string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                    {
                        await _subscriptionService.HandleCheckoutCompletedAsync(new StripeCheckoutSessionPayload
                        {
                            SessionId = session.Id,
                            PaymentIntentId = session.PaymentIntentId,
                            PaymentStatus = session.PaymentStatus,
                            Status = session.Status,
                            Metadata = session.Metadata != null
                                ? new Dictionary<string, string>(session.Metadata)
                                : null
                        });
                        if (statusBeforeReplay == TransactionStatus.Pending
                            && transaction.Status == TransactionStatus.Success)
                        {
                            healedToSuccess++;
                        }
                        continue;
                    }

                    var isExpired = string.Equals(session.Status, "expired", StringComparison.OrdinalIgnoreCase);
                    var isExplicitFailure = string.Equals(session.PaymentStatus, "unpaid", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(session.Status, "complete", StringComparison.OrdinalIgnoreCase);

                    if (isExpired || isExplicitFailure)
                    {
                        transaction.Status = TransactionStatus.Failed;
                        transaction.UpdatedAt = DateTime.UtcNow;
                        markedFailed++;
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(transaction.StripePaymentIntentId))
                {
                    continue;
                }

                var paymentIntent = await _stripeService.GetPaymentIntentAsync(transaction.StripePaymentIntentId);
                if (paymentIntent == null)
                {
                    continue;
                }

                if (string.Equals(paymentIntent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Pending transaction {TransactionId} has succeeded payment_intent {PaymentIntentId} but no checkout session to replay activation.",
                        transaction.TransactionId,
                        paymentIntent.Id);
                    continue;
                }

                var terminalFailure = string.Equals(paymentIntent.Status, "canceled", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(paymentIntent.Status, "requires_payment_method", StringComparison.OrdinalIgnoreCase);

                if (terminalFailure)
                {
                    transaction.Status = TransactionStatus.Failed;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    markedFailed++;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "StripePendingReconcileJob reconciled pending transactions: healed {Healed}, marked failed {Failed}",
                healedToSuccess,
                markedFailed);
        }
    }
}
