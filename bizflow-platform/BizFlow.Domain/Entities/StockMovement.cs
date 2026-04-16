using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class StockMovement
{
    public long StockMovementId { get; set; }

    /// <summary>
    /// FK to Products
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// IN, OUT, ADJUSTMENT
    /// </summary>
    public string MovementType { get; set; } = null!;

    /// <summary>
    /// Quantity moved (positive for IN, negative for OUT)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// IMPORT, ORDER, ADJUSTMENT
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// ID of the reference entity (ImportId, OrderId, etc.)
    /// </summary>
    public long? ReferenceId { get; set; }

    /// <summary>
    /// Manual note/reason for this stock movement
    /// </summary>
    public string? Memo { get; set; }

    /// <summary>
    /// Stock balance after this movement
    /// </summary>
    public decimal BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
