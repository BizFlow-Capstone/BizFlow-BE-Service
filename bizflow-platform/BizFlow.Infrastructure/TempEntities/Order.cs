using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

/// <summary>
/// Đơn hàng bán lẻ tại cửa hàng
/// </summary>
public partial class Orders
{
    public long OrderId { get; set; }

    /// <summary>
    /// Mã đơn hàng duy nhất, format: ORD-YYYYMMDD-NNN
    /// </summary>
    public string OrderCode { get; set; } = null!;

    /// <summary>
    /// FK tự tham chiếu: đơn gốc bị thay thế khi sửa đơn đã hoàn thành
    /// </summary>
    public long? RefOrderId { get; set; }

    /// <summary>
    /// FK to Debtors: khách nợ (nếu có)
    /// </summary>
    public long? DebtorId { get; set; }

    /// <summary>
    /// Tên khách hàng vãng lai (không cần trong hệ thống)
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// SĐT khách hàng vãng lai
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Tổng tiền hàng trước chiết khấu
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Chiết khấu tổng đơn hàng
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// Tổng tiền phải thanh toán = SubTotal - Discount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Số tiền thanh toán bằng tiền mặt
    /// </summary>
    public decimal CashAmount { get; set; }

    /// <summary>
    /// Số tiền thanh toán qua ngân hàng/chuyển khoản
    /// </summary>
    public decimal BankAmount { get; set; }

    /// <summary>
    /// Số tiền ghi nợ = TotalAmount - CashAmount - BankAmount
    /// </summary>
    public decimal DebtAmount { get; set; }

    /// <summary>
    /// pending | completed | cancelled
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Thông tin hóa đơn bổ sung (JSON tự do)
    /// </summary>
    public string? BillMetadata { get; set; }

    /// <summary>
    /// Ghi chú của đơn hàng
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// UserId người tạo đơn
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// UserId người cập nhật gần nhất
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Thời điểm đơn hàng hoàn thành
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// UserId người hoàn thành đơn
    /// </summary>
    public Guid? CompletedBy { get; set; }

    /// <summary>
    /// Thời điểm đơn hàng bị huỷ
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// UserId người huỷ đơn
    /// </summary>
    public Guid? CancelledBy { get; set; }

    /// <summary>
    /// Lý do huỷ chi tiết (free text)
    /// </summary>
    public string? CancelReason { get; set; }

    public virtual Debtors? Debtor { get; set; }

    public virtual ICollection<Orders> InverseRefOrder { get; set; } = new List<Orders>();

    public virtual ICollection<OrderDetails> OrderDetails { get; set; } = new List<OrderDetails>();

    public virtual Orders? RefOrder { get; set; }
}
