-- =============================================
-- Migration  : 037_create_costs
-- Description: Tạo bảng Costs (chi phí) - nguồn dữ liệu chính
--              (source-of-truth) cho mọi khoản chi của cửa hàng.
--              Chi phí có thể phát sinh tự động từ phiếu nhập hàng
--              (CostType = 'import', liên kết ImportId) hoặc
--              được nhập thủ công (các CostType còn lại).
--              Mỗi bản ghi Cost sẽ tạo ra 1 bản ghi GeneralLedgerEntry.
-- Date       : 2025-06-09
-- =============================================

-- =============================================
-- 1. COSTS: Bảng chi phí nguồn
--    CostType 'import'      → liên kết ImportId, tạo tự động khi import được xác nhận
--    CostType khác          → nhân viên tạo thủ công
-- =============================================
CREATE TABLE IF NOT EXISTS Costs (
    CostId             BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL COMMENT 'FK to BusinessLocations',
    CostType           VARCHAR(30) NOT NULL COMMENT 'import | salary | rent | utilities | transport | marketing | maintenance | other | manual',
    ImportId           BIGINT DEFAULT NULL COMMENT 'FK to Imports (chỉ có khi CostType = import)',
    Description        VARCHAR(500) NOT NULL COMMENT 'Mô tả nội dung chi phí',
    Amount             DECIMAL(15,2) NOT NULL COMMENT 'Giá trị chi phí',
    CostDate           DATE NOT NULL COMMENT 'Ngày phát sinh chi phí',
    PaymentMethod      VARCHAR(20) DEFAULT NULL COMMENT 'cash | bank',
    DocumentUrl        VARCHAR(500) DEFAULT NULL COMMENT 'URL chứng từ/hóa đơn (Cloudinary)',
    DocumentPublicId   VARCHAR(255) DEFAULT NULL COMMENT 'Public ID Cloudinary của chứng từ',
    CreatedBy          CHAR(36) NOT NULL COMMENT 'UserId người tạo bản ghi',
    CreatedAt          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt          DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    DeletedAt          DATETIME DEFAULT NULL COMMENT 'Soft delete',

    CONSTRAINT fk_cost_location FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId) ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_cost_import FOREIGN KEY (ImportId)
        REFERENCES Imports(ImportId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_cost_location (BusinessLocationId),
    INDEX idx_cost_location_date (BusinessLocationId, CostDate),
    INDEX idx_cost_import (ImportId),
    INDEX idx_cost_type (BusinessLocationId, CostType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Chi phí cửa hàng - source-of-truth cho mọi khoản chi';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('037_create_costs', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
