using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Danh sách khách nợ theo từng cửa hàng
/// </summary>
public partial class Debtor
{
    public long DebtorId { get; set; }

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// Tên khách nợ
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Số điện thoại (unique per location)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Địa chỉ
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Ghi chú nội bộ
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Hạn mức tín dụng cho phép (NULL = không giới hạn)
    /// </summary>
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// Số dư nợ hiện tại (&lt;0 = đang nợ, &gt;0 = chủ nợ)
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Soft delete timestamp
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// UserId người tạo (FK to Profiles)
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual ICollection<DebtorPaymentTransaction> DebtorPaymentTransactions { get; set; } = new List<DebtorPaymentTransaction>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
