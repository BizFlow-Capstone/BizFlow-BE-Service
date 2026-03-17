using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Lịch sử giao dịch thanh toán nợ của khách
/// </summary>
public partial class DebtorPaymentTransaction
{
    public long DebtorPaymentTransactionId { get; set; }

    /// <summary>
    /// FK to Debtors
    /// </summary>
    public long DebtorId { get; set; }

    /// <summary>
    /// Số tiền thanh toán trong giao dịch này
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// cash | bank
    /// </summary>
    public string PaymentMethod { get; set; } = null!;

    /// <summary>
    /// Ghi chú của giao dịch
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Số dư nợ trước giao dịch
    /// </summary>
    public decimal BalanceBefore { get; set; }

    /// <summary>
    /// Số dư nợ sau giao dịch
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// UserId người ghi nhận thanh toán
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Thời điểm thanh toán thực tế
    /// </summary>
    public DateTime PaidAt { get; set; }

    public virtual Debtor Debtor { get; set; } = null!;
}
