using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Debtor payment and adjustment transaction history.
/// </summary>
public partial class DebtorPaymentTransaction
{
    public long DebtorPaymentTransactionId { get; set; }

    /// <summary>
    /// FK to Debtors
    /// </summary>
    public long DebtorId { get; set; }

    /// <summary>
    /// Signed amount for this transaction.
    /// Negative value reduces debt, positive value increases debt.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// cash | bank
    /// </summary>
    public string PaymentMethod { get; set; } = null!;

    /// <summary>
    /// Transaction notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Debt balance before the transaction.
    /// </summary>
    public decimal BalanceBefore { get; set; }

    /// <summary>
    /// Debt balance after the transaction.
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Recorder UserId.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Transaction timestamp.
    /// </summary>
    public DateTime PaidAt { get; set; }

    public virtual Debtor Debtor { get; set; } = null!;
}
