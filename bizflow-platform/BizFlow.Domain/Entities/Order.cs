using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Retail order record.
/// </summary>
public partial class Order
{
    public long OrderId { get; set; }

    /// <summary>
    /// Unique order code, format: ORD-YYYYMMDD-NNN.
    /// </summary>
    public string OrderCode { get; set; } = null!;

    /// <summary>
    /// Self-reference FK: original order replaced by edit-completed flow.
    /// </summary>
    public long? RefOrderId { get; set; }

    /// <summary>
    /// FK to Debtors (optional).
    /// </summary>
    public long? DebtorId { get; set; }

    /// <summary>
    /// Walk-in customer name (optional).
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Walk-in customer phone number.
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Subtotal before discount.
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Order-level discount.
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// Total payable amount = SubTotal - Discount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Cash payment amount.
    /// </summary>
    public decimal CashAmount { get; set; }

    /// <summary>
    /// Bank transfer payment amount.
    /// </summary>
    public decimal BankAmount { get; set; }

    /// <summary>
    /// Debt amount = TotalAmount - CashAmount - BankAmount.
    /// </summary>
    public decimal DebtAmount { get; set; }

    /// <summary>
    /// pending | completed | cancelled
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Additional billing metadata (free-form JSON).
    /// </summary>
    public string? BillMetadata { get; set; }

    /// <summary>
    /// Order notes.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Creator UserId.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Last updater UserId.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Order completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Completer UserId.
    /// </summary>
    public Guid? CompletedBy { get; set; }

    /// <summary>
    /// Order cancellation timestamp.
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Canceller UserId.
    /// </summary>
    public Guid? CancelledBy { get; set; }

    /// <summary>
    /// Detailed cancellation reason (free text).
    /// </summary>
    public string? CancelReason { get; set; }

    public virtual Debtor? Debtor { get; set; }

    public virtual ICollection<Order> InverseRefOrder { get; set; } = new List<Order>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Order? RefOrder { get; set; }
}
