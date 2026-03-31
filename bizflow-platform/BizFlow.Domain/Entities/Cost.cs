using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Chi phí cửa hàng - source-of-truth cho mọi khoản chi
/// </summary>
public partial class Cost
{
    public long CostId { get; set; }

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// FK to BusinessTypes – ngành nghề liên quan đến khoản chi (nullable for legacy rows)
    /// </summary>
    public Guid? BusinessTypeId { get; set; }

    /// <summary>
    /// import | salary | rent | utilities | transport | marketing | maintenance | other | manual
    /// </summary>
    public string CostType { get; set; } = null!;

    /// <summary>
    /// FK to Imports (chỉ có khi CostType = import)
    /// </summary>
    public long? ImportId { get; set; }

    /// <summary>
    /// Mô tả nội dung chi phí
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Giá trị chi phí
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Ngày phát sinh chi phí
    /// </summary>
    public DateOnly CostDate { get; set; }

    /// <summary>
    /// cash | bank
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// URL chứng từ/hóa đơn (Cloudinary)
    /// </summary>
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Public ID Cloudinary của chứng từ
    /// </summary>
    public string? DocumentPublicId { get; set; }

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

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual BusinessType? BusinessType { get; set; }

    public virtual Import? Import { get; set; }
}
