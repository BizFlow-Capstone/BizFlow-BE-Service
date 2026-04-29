using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Debtor list per business location
/// </summary>
public partial class Debtor
{
    public long DebtorId { get; set; }

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// Debtor name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Phone number (unique per location)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Internal note
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Allowed credit limit (NULL = unlimited)
    /// </summary>
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// Current balance semantics:
    /// (&gt;0 = debtor owes owner / accounts receivable, &lt;0 = owner owes debtor / payable-credit)
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Soft delete timestamp
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Creator user id (FK to Profiles); null if anonymized.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual ICollection<DebtorPaymentTransaction> DebtorPaymentTransactions { get; set; } = new List<DebtorPaymentTransaction>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
