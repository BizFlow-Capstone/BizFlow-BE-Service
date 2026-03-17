-- =============================================
-- Migration  : 034_create_debtors_and_debtor_payment_transactions
-- Description: Tạo bảng Debtors (khách nợ) và DebtorPaymentTransactions
--              (lịch sử thanh toán nợ) phục vụ module Công nợ.
-- Date       : 2025-06-09
-- =============================================

-- =============================================
-- 1. DEBTORS: Danh sách khách nợ theo từng cửa hàng
--    DebtorId PK, BusinessLocationId FK, Name, Phone, Address,
--    Notes, CreditLimit, CurrentBalance, IsActive, DeletedAt,
--    CreatedByUserId, CreatedAt, UpdatedAt
-- =============================================
CREATE TABLE IF NOT EXISTS Debtors (
    DebtorId        BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL COMMENT 'FK to BusinessLocations',
    Name            VARCHAR(255) NOT NULL COMMENT 'Tên khách nợ',
    Phone           VARCHAR(20) DEFAULT NULL COMMENT 'Số điện thoại (unique per location)',
    Address         TEXT DEFAULT NULL COMMENT 'Địa chỉ',
    Notes           TEXT DEFAULT NULL COMMENT 'Ghi chú nội bộ',
    CreditLimit     DECIMAL(15,2) DEFAULT NULL COMMENT 'Hạn mức tín dụng cho phép (NULL = không giới hạn)',
    CurrentBalance  DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Số dư nợ hiện tại (>0 = đang nợ)',
    IsActive        BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Trạng thái hoạt động',
    DeletedAt       DATETIME DEFAULT NULL COMMENT 'Soft delete timestamp',
    CreatedByUserId CHAR(36) NOT NULL COMMENT 'UserId người tạo (FK to Profiles)',
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_debtor_location FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_debtor_location (BusinessLocationId),
    INDEX idx_debtor_active (BusinessLocationId, IsActive),
    -- Phone phải unique trong phạm vi cùng cửa hàng (bỏ qua NULL và soft-deleted)
    UNIQUE INDEX idx_debtor_phone_location (BusinessLocationId, Phone)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Danh sách khách nợ theo từng cửa hàng';

-- =============================================
-- 2. DEBTORPAYMENTTRANSACTIONS: Lịch sử giao dịch thanh toán nợ
--    TransactionId PK, DebtorId FK, Amount, PaymentMethod,
--    Notes, BalanceBefore, BalanceAfter, CreatedByUserId, PaidAt
-- =============================================
CREATE TABLE IF NOT EXISTS DebtorPaymentTransactions (
    DebtorPaymentTransactionId   BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    DebtorId        BIGINT NOT NULL COMMENT 'FK to Debtors',
    Amount          DECIMAL(15,2) NOT NULL COMMENT 'Số tiền thanh toán trong giao dịch này',
    PaymentMethod   VARCHAR(20) NOT NULL COMMENT 'cash | bank',
    Notes           TEXT DEFAULT NULL COMMENT 'Ghi chú của giao dịch',
    BalanceBefore   DECIMAL(15,2) NOT NULL COMMENT 'Số dư nợ trước giao dịch',
    BalanceAfter    DECIMAL(15,2) NOT NULL COMMENT 'Số dư nợ sau giao dịch',
    CreatedByUserId CHAR(36) NOT NULL COMMENT 'UserId người ghi nhận thanh toán',
    PaidAt          DATETIME NOT NULL COMMENT 'Thời điểm thanh toán thực tế',

    CONSTRAINT fk_debtor_payment_debtor FOREIGN KEY (DebtorId)
        REFERENCES Debtors(DebtorId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_debtor_payment_debtor (DebtorId),
    INDEX idx_debtor_payment_paid_at (PaidAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Lịch sử giao dịch thanh toán nợ của khách';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('034_create_debtors_and_debtor_payment_transactions', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
