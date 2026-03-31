using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Doanh thu cửa hàng - source-of-truth cho mọi khoản thu
/// </summary>
public partial class Revenue
{
    public long RevenueId { get; set; }

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// Optional business type classification for accounting book context
    /// </summary>
    public Guid? BusinessTypeId { get; set; }

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
    /// Số hiệu chứng từ (optional)
    /// </summary>
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Ngày chứng từ (optional)
    /// </summary>
    public DateOnly? DocumentDate { get; set; }

    /// <summary>
    /// UserId người tạo bản ghi
    /// </summary>
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Soft delete
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;
    public virtual BusinessType? BusinessType { get; set; }
}
