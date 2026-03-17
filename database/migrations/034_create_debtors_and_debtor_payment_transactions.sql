-- =============================================
-- Migration  : 034_create_debtors_and_debtor_payment_transactions
-- Description: Create Debtors and DebtorPaymentTransactions tables
--              for the debt management module.
-- Date       : 2025-06-09
-- =============================================

-- =============================================
-- 1. DEBTORS: Debtor list per business location
--    DebtorId PK, BusinessLocationId FK, Name, Phone, Address,
--    Notes, CreditLimit, CurrentBalance, IsActive, DeletedAt,
--    CreatedByUserId, CreatedAt, UpdatedAt
-- =============================================
CREATE TABLE IF NOT EXISTS Debtors (
    DebtorId        BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL COMMENT 'FK to BusinessLocations',
  Name            VARCHAR(255) NOT NULL COMMENT 'Debtor name',
  Phone           VARCHAR(20) DEFAULT NULL COMMENT 'Phone number (unique per location)',
  Address         TEXT DEFAULT NULL COMMENT 'Address',
  Notes           TEXT DEFAULT NULL COMMENT 'Internal notes',
  CreditLimit     DECIMAL(15,2) DEFAULT NULL COMMENT 'Allowed credit limit (NULL = unlimited)',
  CurrentBalance  DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Current debt balance (>0 = owes money)',
  IsActive        BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Active status',
    DeletedAt       DATETIME DEFAULT NULL COMMENT 'Soft delete timestamp',
  CreatedByUserId CHAR(36) NOT NULL COMMENT 'Creator UserId (FK to Profiles)',
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_debtor_location FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_debtor_location (BusinessLocationId),
    INDEX idx_debtor_active (BusinessLocationId, IsActive),
    -- Phone must be unique within a business location (NULL and soft-deleted behavior follows DB unique index semantics)
    UNIQUE INDEX idx_debtor_phone_location (BusinessLocationId, Phone)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Debtor list per business location';

-- =============================================
-- 2. DEBTORPAYMENTTRANSACTIONS: Debtor payment transaction history
--    TransactionId PK, DebtorId FK, Amount, PaymentMethod,
--    Notes, BalanceBefore, BalanceAfter, CreatedByUserId, PaidAt
-- =============================================
CREATE TABLE IF NOT EXISTS DebtorPaymentTransactions (
    DebtorPaymentTransactionId   BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    DebtorId        BIGINT NOT NULL COMMENT 'FK to Debtors',
    Amount          DECIMAL(15,2) NOT NULL COMMENT 'Signed adjustment amount for this transaction',
    PaymentMethod   VARCHAR(20) NOT NULL COMMENT 'cash | bank',
    Notes           TEXT DEFAULT NULL COMMENT 'Transaction notes',
    BalanceBefore   DECIMAL(15,2) NOT NULL COMMENT 'Debt balance before transaction',
    BalanceAfter    DECIMAL(15,2) NOT NULL COMMENT 'Debt balance after transaction',
    CreatedByUserId CHAR(36) NOT NULL COMMENT 'Recorder UserId',
    PaidAt          DATETIME NOT NULL COMMENT 'Payment/adjustment timestamp',

    CONSTRAINT fk_debtor_payment_debtor FOREIGN KEY (DebtorId)
        REFERENCES Debtors(DebtorId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_debtor_payment_debtor (DebtorId),
    INDEX idx_debtor_payment_paid_at (PaidAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Debtor payment/adjustment transaction history';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('034_create_debtors_and_debtor_payment_transactions', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
