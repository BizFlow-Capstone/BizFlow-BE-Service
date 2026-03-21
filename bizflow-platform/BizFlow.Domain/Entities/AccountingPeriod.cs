using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class AccountingPeriod
{
    public long PeriodId { get; set; }

    public int BusinessLocationId { get; set; }

    public string PeriodType { get; set; } = null!;

    public short Year { get; set; }

    public int? Quarter { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal? OpeningCashBalance { get; set; }

    public decimal? OpeningBankBalance { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? FinalizedAt { get; set; }

    public Guid? FinalizedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual ICollection<AccountingPeriodAuditLog> AuditLogs { get; set; } = new List<AccountingPeriodAuditLog>();
}
