-- =============================================
-- Migration  : 036_create_revenues
-- Description: Tạo bảng Revenues (doanh thu) - nguồn dữ liệu chính
--              (source-of-truth) cho mọi khoản thu của cửa hàng.
--              Doanh thu phát sinh từ đơn hàng (sale) hoặc
--              được nhập thủ công (manual).
--              Mỗi bản ghi Revenue sẽ tạo ra 1 bản ghi GeneralLedgerEntry
--              tương ứng.
-- Date       : 2025-06-09
-- =============================================

-- =============================================
-- 1. REVENUES: Bảng doanh thu nguồn
--    RevenueType = 'sale'   → doanh thu bán hàng
--    RevenueType = 'manual' → doanh thu nhập tay
-- =============================================
CREATE TABLE IF NOT EXISTS Revenues (
    RevenueId          BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL COMMENT 'FK to BusinessLocations',
    RevenueType        VARCHAR(20) NOT NULL COMMENT 'sale | manual',
    Amount             DECIMAL(15,2) NOT NULL COMMENT 'Giá trị doanh thu',
    RevenueDate        DATE NOT NULL COMMENT 'Ngày ghi nhận doanh thu',
    Description        VARCHAR(500) NOT NULL COMMENT 'Mô tả nội dung doanh thu',
    MoneyChannel       VARCHAR(10) DEFAULT NULL COMMENT 'cash | bank | debt',
    CreatedBy          CHAR(36) NOT NULL COMMENT 'UserId người tạo bản ghi',
    CreatedAt          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DeletedAt          DATETIME DEFAULT NULL COMMENT 'Soft delete',

    CONSTRAINT fk_revenue_location FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_revenue_location (BusinessLocationId),
    INDEX idx_revenue_location_date (BusinessLocationId, RevenueDate),
    INDEX idx_revenue_type (RevenueType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Doanh thu cửa hàng - source-of-truth cho mọi khoản thu';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('036_create_revenues', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
