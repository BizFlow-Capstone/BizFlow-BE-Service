using System;

namespace BizFlow.Domain.Entities;

public partial class TaxPayment
{
    public long TaxPaymentId { get; set; }
    public int BusinessLocationId { get; set; }
    public long? PeriodId { get; set; }

    /// <summary>
    /// VAT | PIT
    /// </summary>
    public string TaxType { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateOnly PaidAt { get; set; }

    /// <summary>
    /// cash | bank
    /// </summary>
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public virtual BusinessLocation BusinessLocation { get; set; } = null!;
    public virtual AccountingPeriod? Period { get; set; }
}
