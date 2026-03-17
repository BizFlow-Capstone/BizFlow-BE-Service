using System;

namespace BizFlow.Domain.Entities;

public partial class AccountingPeriodAuditLog
{
    public long LogId { get; set; }

    public long PeriodId { get; set; }

    public string Action { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Reason { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AccountingPeriod Period { get; set; } = null!;
}