-- =============================================
-- Migration  : 035_create_orders_and_order_details
-- Description: Tạo bảng Orders (đơn hàng) và OrderDetails (chi tiết
--              đơn hàng) phục vụ module Bán hàng.
--              Orders tự tham chiếu qua RefOrderId để theo dõi
--              đơn hàng được tạo từ thao tác "Sửa đơn đã hoàn thành".
-- Date       : 2025-06-09
-- =============================================

-- =============================================
-- 1. ORDERS: Đơn hàng bán lẻ
--    Trạng thái: pending → completed | cancelled
--    Đơn hàng "sửa đã hoàn thành" tạo bản ghi NEW ở trạng thái completed,
--    đơn cũ chuyển sang cancelled kèm CancelReason dạng text.
-- =============================================
CREATE TABLE IF NOT EXISTS Orders (
    OrderId           BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    OrderCode         VARCHAR(50) NOT NULL COMMENT 'Mã đơn hàng duy nhất, format: ORD-YYYYMMDD-NNN',
    RefOrderId        BIGINT DEFAULT NULL COMMENT 'FK tự tham chiếu: đơn gốc bị thay thế khi sửa đơn đã hoàn thành',
    DebtorId          BIGINT DEFAULT NULL COMMENT 'FK to Debtors: khách nợ (nếu có)',
    CustomerName      VARCHAR(255) DEFAULT NULL COMMENT 'Tên khách hàng vãng lai (không cần trong hệ thống)',
    CustomerPhone     VARCHAR(20) DEFAULT NULL COMMENT 'SĐT khách hàng vãng lai',
    SubTotal          DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Tổng tiền hàng trước chiết khấu',
    Discount          DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Chiết khấu tổng đơn hàng',
    TotalAmount       DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Tổng tiền phải thanh toán = SubTotal - Discount',
    CashAmount        DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Số tiền thanh toán bằng tiền mặt',
    BankAmount        DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Số tiền thanh toán qua ngân hàng/chuyển khoản',
    DebtAmount        DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Số tiền ghi nợ = TotalAmount - CashAmount - BankAmount',
    Status            VARCHAR(20) NOT NULL DEFAULT 'pending' COMMENT 'pending | completed | cancelled',
    BillMetadata      JSON DEFAULT NULL COMMENT 'Thông tin hóa đơn bổ sung (JSON tự do)',
    Note              TEXT DEFAULT NULL COMMENT 'Ghi chú của đơn hàng',
    CreatedBy         CHAR(36) NOT NULL COMMENT 'UserId người tạo đơn',
    UpdatedBy         CHAR(36) DEFAULT NULL COMMENT 'UserId người cập nhật gần nhất',
    CreatedAt         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CompletedAt       DATETIME DEFAULT NULL COMMENT 'Thời điểm đơn hàng hoàn thành',
    CompletedBy       CHAR(36) DEFAULT NULL COMMENT 'UserId người hoàn thành đơn',
    CancelledAt       DATETIME DEFAULT NULL COMMENT 'Thời điểm đơn hàng bị huỷ',
    CancelledBy       CHAR(36) DEFAULT NULL COMMENT 'UserId người huỷ đơn',
    CancelReason      TEXT DEFAULT NULL COMMENT 'Lý do huỷ chi tiết (free text)',

    CONSTRAINT fk_order_debtor FOREIGN KEY (DebtorId)
        REFERENCES Debtors(DebtorId) ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_order_ref_order FOREIGN KEY (RefOrderId)
        REFERENCES Orders(OrderId) ON DELETE SET NULL ON UPDATE CASCADE,

    UNIQUE INDEX idx_order_code (OrderCode),
    INDEX idx_order_status (Status),
    INDEX idx_order_created (CreatedAt),
    INDEX idx_order_debtor (DebtorId),
    INDEX idx_order_ref (RefOrderId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Đơn hàng bán lẻ tại cửa hàng';

-- =============================================
-- 2. ORDERDETAILS: Chi tiết từng dòng sản phẩm trong đơn hàng
--    Snapshot giá/tên/đơn vị tại thời điểm tạo đơn để đảm bảo
--    tính bất biến của lịch sử bán hàng.
-- =============================================
CREATE TABLE IF NOT EXISTS OrderDetails (
    OrderDetailId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    OrderId       BIGINT NOT NULL COMMENT 'FK to Orders',
    SaleItemId    BIGINT NOT NULL COMMENT 'FK to SaleItems (live reference)',
    Quantity      INT NOT NULL DEFAULT 1 COMMENT 'Số lượng bán',
    UnitPrice     DECIMAL(15,2) NOT NULL COMMENT 'Snapshot đơn giá tại thời điểm tạo đơn',
    Discount      DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Chiết khấu theo dòng sản phẩm',
    Amount        DECIMAL(15,2) NOT NULL COMMENT 'Thành tiền = Quantity * UnitPrice - Discount',
    CreatedAt     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_order_detail_order FOREIGN KEY (OrderId)
        REFERENCES Orders(OrderId) ON DELETE CASCADE ON UPDATE CASCADE,

    CONSTRAINT fk_order_detail_sale_item FOREIGN KEY (SaleItemId)
        REFERENCES SaleItems(SaleItemId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_order_detail_order (OrderId),
    INDEX idx_order_detail_sale_item (SaleItemId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Chi tiết dòng sản phẩm trong đơn hàng (snapshot giá tại thời điểm bán)';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('035_create_orders_and_order_details', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
