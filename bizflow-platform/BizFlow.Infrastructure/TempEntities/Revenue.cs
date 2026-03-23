using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

/// <summary>
/// Doanh thu cửa hàng - source-of-truth cho mọi khoản thu
/// </summary>
public partial class Revenues
{
    public long RevenueId { get; set; }

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// Soft reference to Order, nullable because some revenues are manual
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// sale | manual
    /// </summary>
    public string RevenueType { get; set; } = null!;

    /// <summary>
    /// Giá trị doanh thu
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Ngày ghi nhận doanh thu
    /// </summary>
    public DateOnly RevenueDate { get; set; }

    /// <summary>
    /// Mô tả nội dung doanh thu
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// cash | bank | debt
    /// </summary>
    public string? MoneyChannel { get; set; }

    /// <summary>
    /// UserId người tạo bản ghi
    /// </summary>
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Soft delete
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public virtual BusinessLocations BusinessLocation { get; set; } = null!;
}
