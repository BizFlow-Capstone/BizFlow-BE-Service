-- Migration: 105_nullable_audit_columns_account_purge
-- Purpose: Allow SET NULL on audit columns when purging accounts (employee-created rows kept).

ALTER TABLE Orders
    MODIFY COLUMN CreatedBy CHAR(36) NULL COMMENT 'Creator UserId';

ALTER TABLE Costs
    MODIFY COLUMN CreatedBy CHAR(36) NULL COMMENT 'UserId of the user who created the row';

ALTER TABLE Revenues
    MODIFY COLUMN CreatedBy CHAR(36) NULL COMMENT 'UserId of the user who created the row';

ALTER TABLE Debtors
    MODIFY COLUMN CreatedByUserId CHAR(36) NULL COMMENT 'UserId of creator (FK to Profiles)';

ALTER TABLE DebtorPaymentTransactions
    MODIFY COLUMN CreatedByUserId CHAR(36) NULL COMMENT 'UserId who recorded the payment';

ALTER TABLE TaxPayments
    MODIFY COLUMN CreatedByUserId CHAR(36) NULL COMMENT 'User who created the row';

ALTER TABLE AccountingBooks
    MODIFY COLUMN CreatedByUserId CHAR(36) NULL;

ALTER TABLE AccountingExports
    MODIFY COLUMN ExportedByUserId CHAR(36) NULL;

ALTER TABLE AccountingPeriodAuditLogs
    MODIFY COLUMN CreatedByUserId CHAR(36) NULL;

ALTER TABLE AccountingBookTaxOverrides
    MODIFY COLUMN UpdatedByUserId CHAR(36) NULL;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('105_nullable_audit_columns_account_purge', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('105_nullable_audit_columns_account_purge', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);