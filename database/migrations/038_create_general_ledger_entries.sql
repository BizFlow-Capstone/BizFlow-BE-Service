-- =============================================
-- Migration  : 038_create_general_ledger_entries
-- Description: Create GeneralLedgerEntries table (accounting ledger).
--              This table is IMMUTABLE - no UPDATE/DELETE operations.
--              Any correction must be represented by reversal entries.
--              Each financial event (order completion, costs,
--              debt settlement, etc.) generates one or more
--              records in this table.
-- Date       : 2025-06-09
-- =============================================

-- =============================================
-- 1. GENERALLEDGERENTRIES: Immutable accounting ledger
--    TransactionType: financial transaction type
--    ReferenceType  : source entity type (polymorphic)
--    ReferenceId    : source entity ID (without hard FK)
--    IsReversal     : reversal record used to negate/adjust a prior entry
-- =============================================
CREATE TABLE IF NOT EXISTS GeneralLedgerEntries (
    EntryId            BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL COMMENT 'FK to BusinessLocations',
    TransactionType    VARCHAR(30) NOT NULL COMMENT 'sale | import_cost | manual_cost | debt_payment | manual_revenue | manual_expense',
    ReferenceType      VARCHAR(30) NOT NULL COMMENT 'order | cost | import | debtor_payment | revenue',
    ReferenceId        BIGINT DEFAULT NULL COMMENT 'Source entity ID (polymorphic, without hard FK)',
    EntryDate          DATE NOT NULL COMMENT 'Transaction date',
    Description        VARCHAR(500) NOT NULL COMMENT 'Ledger entry description',
    DebitAmount        DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Debit amount',
    CreditAmount       DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Credit amount',
    MoneyChannel       VARCHAR(10) DEFAULT NULL COMMENT 'cash | bank | debt',
    IsReversal         BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'TRUE when this is a reversal entry',
    ReversedEntryId    BIGINT DEFAULT NULL COMMENT 'Reversed EntryId (self reference)',
    CreatedAt          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'IMMUTABLE - must not change after creation',

    CONSTRAINT fk_gl_location FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId) ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_gl_reversed_entry FOREIGN KEY (ReversedEntryId)
        REFERENCES GeneralLedgerEntries(EntryId) ON DELETE SET NULL ON UPDATE CASCADE,

    INDEX idx_gl_location (BusinessLocationId),
    INDEX idx_gl_location_date (BusinessLocationId, EntryDate),
    INDEX idx_gl_reference (ReferenceType, ReferenceId),
    INDEX idx_gl_transaction_type (BusinessLocationId, TransactionType),
    INDEX idx_gl_reversal (ReversedEntryId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    COMMENT='Immutable accounting ledger - append only, no update/delete';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('038_create_general_ledger_entries', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
