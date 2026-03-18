-- =============================================
-- Migration  : 035_create_orders_and_order_details
-- Description: Create Orders and OrderDetails tables
--              for the sales module.
--              Orders use self-reference via RefOrderId to track
--              replacement orders from "edit completed order" flow.
-- Date       : 2025-06-09
-- =============================================

-- =============================================
-- 1. ORDERS: Retail orders
--    Status: pending -> completed | cancelled
--    In "edit completed" flow, a NEW order is created as completed,
--    and the old order is cancelled with text CancelReason.
-- =============================================
CREATE TABLE IF NOT EXISTS Orders (
    OrderId           BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    OrderCode         VARCHAR(50) NOT NULL COMMENT 'Unique order code, format: ORD-YYYYMMDD-NNN',
    RefOrderId        BIGINT DEFAULT NULL COMMENT 'Self-reference FK: original order replaced by edit-completed flow',
    DebtorId          BIGINT DEFAULT NULL COMMENT 'FK to Debtors (optional)',
    CustomerName      VARCHAR(255) DEFAULT NULL COMMENT 'Walk-in customer name (not required in system)',
    CustomerPhone     VARCHAR(20) DEFAULT NULL COMMENT 'Walk-in customer phone number',
    SubTotal          DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Subtotal before discount',
    Discount          DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Order-level discount',
    TotalAmount       DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Total payable = SubTotal - Discount',
    CashAmount        DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Cash payment amount',
    BankAmount        DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Bank transfer payment amount',
    DebtAmount        DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Debt amount = TotalAmount - CashAmount - BankAmount',
    Status            VARCHAR(20) NOT NULL DEFAULT 'pending' COMMENT 'pending | completed | cancelled',
    BillMetadata      JSON DEFAULT NULL COMMENT 'Additional billing metadata (free-form JSON)',
    Note              TEXT DEFAULT NULL COMMENT 'Order notes',
    CreatedBy         CHAR(36) NOT NULL COMMENT 'Creator UserId',
    UpdatedBy         CHAR(36) DEFAULT NULL COMMENT 'Last updater UserId',
    CreatedAt         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CompletedAt       DATETIME DEFAULT NULL COMMENT 'Order completion timestamp',
    CompletedBy       CHAR(36) DEFAULT NULL COMMENT 'Completer UserId',
    CancelledAt       DATETIME DEFAULT NULL COMMENT 'Order cancellation timestamp',
    CancelledBy       CHAR(36) DEFAULT NULL COMMENT 'Canceller UserId',
    CancelReason      TEXT DEFAULT NULL COMMENT 'Detailed cancellation reason (free text)',

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
    COMMENT='Retail order records';

-- =============================================
-- 2. ORDERDETAILS: Line items in an order
--    Store snapshot values at order creation time to preserve
--    immutable sales history.
-- =============================================
CREATE TABLE IF NOT EXISTS OrderDetails (
    OrderDetailId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    OrderId       BIGINT NOT NULL COMMENT 'FK to Orders',
    SaleItemId    BIGINT NOT NULL COMMENT 'FK to SaleItems (live reference)',
        Quantity      INT NOT NULL DEFAULT 1 COMMENT 'Sold quantity',
        UnitPrice     DECIMAL(15,2) NOT NULL COMMENT 'Snapshot unit price at order creation time',
        Discount      DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Line-item discount',
        Amount        DECIMAL(15,2) NOT NULL COMMENT 'Line amount = Quantity * UnitPrice - Discount',
    CreatedAt     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_order_detail_order FOREIGN KEY (OrderId)
        REFERENCES Orders(OrderId) ON DELETE CASCADE ON UPDATE CASCADE,

    CONSTRAINT fk_order_detail_sale_item FOREIGN KEY (SaleItemId)
        REFERENCES SaleItems(SaleItemId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_order_detail_order (OrderId),
    INDEX idx_order_detail_sale_item (SaleItemId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    COMMENT='Order line items (snapshot pricing at sale time)';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('035_create_orders_and_order_details', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
