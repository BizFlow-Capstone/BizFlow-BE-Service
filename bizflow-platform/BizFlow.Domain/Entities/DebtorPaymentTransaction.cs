using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Debtor payment transaction history
/// </summary>
public partial class DebtorPaymentTransaction
{
    public long DebtorPaymentTransactionId { get; set; }

    /// <summary>
    /// FK to Debtors
    /// </summary>
    public long DebtorId { get; set; }

    /// <summary>
    /// Payment amount for this transaction
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// cash | bank
    /// </summary>
    public string PaymentMethod { get; set; } = null!;

    /// <summary>
    /// Transaction notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Balance before transaction
    /// </summary>
    public decimal BalanceBefore { get; set; }

    /// <summary>
    /// Balance after transaction
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// UserId who recorded the payment
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Actual payment timestamp
    /// </summary>
    public DateTime PaidAt { get; set; }

    public virtual Debtor Debtor { get; set; } = null!;
}
