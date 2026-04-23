using System;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Synthetic lock row used to serialize concurrent DocumentNumber uniqueness
/// checks per (Owner, DocumentNumberNormalized).
///
/// <para>
/// The row is created the very first time a given Owner uses a particular
/// document number and is NEVER deleted. Write paths (Create/Update for
/// Cost/Revenue/Import/Order) always:
/// <list type="number">
///   <item>Ensure the lock row exists (INSERT IGNORE semantics via repository).</item>
///   <item><c>SELECT ... FOR UPDATE</c> on this row inside a DB transaction.</item>
///   <item>Perform the duplicate-check query across Costs + Revenues.</item>
///   <item>Persist the target entity and commit.</item>
/// </list>
/// This guarantees the "DocumentNumber unique per Owner — even for cancelled
/// rows" invariant even under high concurrency.
/// </para>
/// </summary>
public partial class AccountingDocumentLock
{
    /// <summary>Synthetic PK (Guid).</summary>
    public Guid LockId { get; set; }

    /// <summary>Owner (Profile/Account) that owns the DocumentNumber namespace.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Normalized document number (UPPER + TRIM).</summary>
    public string DocumentNumberNormalized { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
