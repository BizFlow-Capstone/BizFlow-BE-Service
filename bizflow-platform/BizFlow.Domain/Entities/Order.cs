using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// In-store retail order
/// </summary>
public partial class Order
{
    public long OrderId { get; set; }

    /// <summary>
    /// Unique order code, format: ORD-YYYYMMDD-NNN
    /// </summary>
    public string OrderCode { get; set; } = null!;

    /// <summary>
    /// Self-reference FK: original order replaced when editing a completed order
    /// </summary>
    public long? RefOrderId { get; set; }

    /// <summary>
    /// FK to Debtors: debtor customer (if any)
    /// </summary>
    public long? DebtorId { get; set; }

    /// <summary>
    /// Walk-in customer name (not required in the system)
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Walk-in customer phone
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Subtotal before discount
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Total order discount
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// Total payable amount = SubTotal - Discount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Amount paid by cash
    /// </summary>
    public decimal CashAmount { get; set; }

    /// <summary>
    /// Amount paid by bank transfer
    /// </summary>
    public decimal BankAmount { get; set; }

    /// <summary>
    /// Debt amount = TotalAmount - CashAmount - BankAmount
    /// </summary>
    public decimal DebtAmount { get; set; }

    /// <summary>
    /// pending | completed | cancelled
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Additional invoice metadata (free-form JSON)
    /// </summary>
    public string? BillMetadata { get; set; }

    /// <summary>
    /// Order notes
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// UserId of order creator
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// UserId of last updater
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Order completion timestamp
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// UserId who completed the order
    /// </summary>
    public Guid? CompletedBy { get; set; }

    /// <summary>
    /// Order cancellation timestamp
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// UserId who cancelled the order
    /// </summary>
    public Guid? CancelledBy { get; set; }

    /// <summary>
    /// Detailed cancellation reason (free text)
    /// </summary>
    public string? CancelReason { get; set; }

    public virtual Debtor? Debtor { get; set; }

    public virtual ICollection<Order> InverseRefOrder { get; set; } = new List<Order>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Order? RefOrder { get; set; }
}
