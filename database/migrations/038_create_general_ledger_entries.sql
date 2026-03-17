-- =============================================
-- Migration  : 038_create_general_ledger_entries
-- Description: Tạo bảng GeneralLedgerEntries (sổ cái kế toán).
--              Bảng này là IMMUTABLE - không có UPDATE/DELETE.
--              Mọi điều chỉnh phải thực hiện qua bản ghi đảo (reversal).
--              Mỗi sự kiện tài chính (đơn hàng hoàn thành, chi phí
--              phát sinh, thanh toán nợ,...) đều tạo ra 1 hoặc nhiều
--              bản ghi trong bảng này.
-- Date       : 2025-06-09
-- =============================================

-- =============================================
-- 1. GENERALLEDGERENTRIES: Sổ cái kế toán bất biến
--    TransactionType: loại nghiệp vụ kinh tế phát sinh
--    ReferenceType  : loại thực thể nguồn (polymorphic)
--    ReferenceId    : ID của thực thể nguồn (không dùng FK cứng)
--    IsReversal     : bản ghi đảo dùng để huỷ/điều chỉnh bản ghi trước
-- =============================================
CREATE TABLE IF NOT EXISTS GeneralLedgerEntries (
    EntryId            BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL COMMENT 'FK to BusinessLocations',
    TransactionType    VARCHAR(30) NOT NULL COMMENT 'sale | import_cost | manual_cost | debt_payment | manual_revenue | manual_expense',
    ReferenceType      VARCHAR(30) NOT NULL COMMENT 'order | cost | import | debtor_payment | revenue',
    ReferenceId        BIGINT DEFAULT NULL COMMENT 'ID của thực thể nguồn (polymorphic, không có FK cứng)',
    EntryDate          DATE NOT NULL COMMENT 'Ngày phát sinh nghiệp vụ',
    Description        VARCHAR(500) NOT NULL COMMENT 'Mô tả nội dung bút toán',
    DebitAmount        DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Số tiền Nợ (debit)',
    CreditAmount       DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Số tiền Có (credit)',
    MoneyChannel       VARCHAR(10) DEFAULT NULL COMMENT 'cash | bank | debt',
    IsReversal         BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'TRUE nếu đây là bản ghi đảo (reversal entry)',
    ReversedEntryId    BIGINT DEFAULT NULL COMMENT 'EntryId bị đảo ngược (tự tham chiếu)',
    CreatedAt          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'IMMUTABLE - không được thay đổi sau khi tạo',

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
  COMMENT='Sổ cái kế toán bất biến - chỉ thêm, không sửa/xoá';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('038_create_general_ledger_entries', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
