-- =============================================
-- Migration  : 041_create_accounting_period_tables
-- Description: Create AccountingPeriods and AccountingPeriodAuditLogs tables
-- Date       : 2026-03-17
-- =============================================

CREATE TABLE IF NOT EXISTS AccountingPeriods (
    PeriodId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL,

    PeriodType VARCHAR(10) NOT NULL COMMENT 'quarter | year',
    Year SMALLINT NOT NULL,
    Quarter TINYINT DEFAULT NULL COMMENT '1-4 for quarterly, NULL for annual',
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,

    OpeningCashBalance DECIMAL(15,2) DEFAULT NULL COMMENT 'Opening cash balance',
    OpeningBankBalance DECIMAL(15,2) DEFAULT NULL COMMENT 'Opening bank balance',

    Status VARCHAR(20) NOT NULL DEFAULT 'open' COMMENT 'open | finalized | reopened',

    FinalizedAt DATETIME DEFAULT NULL,
    FinalizedByUserId CHAR(36) DEFAULT NULL,

    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_period_location FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId) ON DELETE RESTRICT ON UPDATE CASCADE,

    UNIQUE INDEX idx_period_unique (BusinessLocationId, PeriodType, Year, Quarter),
    INDEX idx_period_status (Status),
    INDEX idx_period_year (Year)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AccountingPeriodAuditLogs (
    LogId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    PeriodId BIGINT NOT NULL,

    Action VARCHAR(50) NOT NULL COMMENT 'period_created | period_finalized | period_reopened | book_created | book_exported | group_suggestion',

    OldValue JSON DEFAULT NULL COMMENT 'Value before change',
    NewValue JSON DEFAULT NULL COMMENT 'Value after change',
    Reason TEXT DEFAULT NULL COMMENT 'Reason is required for reopen',

    CreatedByUserId CHAR(36) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_audit_period FOREIGN KEY (PeriodId)
        REFERENCES AccountingPeriods(PeriodId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_audit_period (PeriodId),
    INDEX idx_audit_action (Action),
    INDEX idx_audit_date (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('041_create_accounting_period_tables', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';