-- =============================================
-- Migration 125: Add stock_movements to MappableEntities + MappableFields
--               and link S2d TemplateFieldMappings to these registry entries.
-- =============================================
-- Context:
--   Migration 060 seeded 6 MappableEntities (orders/order_details/gl_entries/costs/tax_payments/products)
--   but omitted stock_movements.  Migration 061 seeded S2d field mappings without SourceEntityId/SourceFieldId
--   because the entity did not exist yet.  Migration 082 wired DataSourceType='stock_movements' on the S2d
--   template, so rendering already works — this migration completes the metadata registry side.
-- =============================================

-- ── 1. Insert stock_movements entity ──────────────────────────────────────────
INSERT IGNORE INTO MappableEntities (EntityCode, DisplayName, Description, Category)
VALUES ('stock_movements', 'Biến động kho', 'Phiếu nhập / xuất kho và điều chỉnh tồn', 'general');

SET @smEntityId = (SELECT EntityId FROM MappableEntities WHERE EntityCode = 'stock_movements');

-- ── 2. Insert MappableFields for stock_movements ───────────────────────────────
-- FieldCode values must match ExtractBySourceFieldCode() keys in BookRenderingService.cs
INSERT IGNORE INTO MappableFields (EntityId, FieldCode, DisplayName, Description, DataType, AllowedAggregations) VALUES
(@smEntityId, 'MovementDate',  'Ngày chứng từ',     'Ngày phát sinh biến động kho',                         'date',    '["none"]'),
(@smEntityId, 'Description',   'Diễn giải',         'Memo / diễn giải phiếu kho',                           'text',    '["none"]'),
(@smEntityId, 'ReferenceId',   'Số chứng từ',       'ID phiếu nhập / đơn hàng tham chiếu',                  'text',    '["none"]'),
(@smEntityId, 'MovementType',  'Loại biến động',    'IN | OUT | ADJUSTMENT',                                 'text',    '["none"]'),
(@smEntityId, 'ProductId',     'Mã sản phẩm',       'ID sản phẩm',                                          'text',    '["none"]'),
(@smEntityId, 'ProductName',   'Tên sản phẩm',      'Tên sản phẩm từ danh mục',                             'text',    '["none"]'),
(@smEntityId, 'Unit',          'ĐVT',               'Đơn vị tính (cái, kg, cây...)',                         'text',    '["none"]'),
(@smEntityId, 'CostPrice',     'Đơn giá',           'Giá vốn / đơn giá nhập kho',                           'decimal', '["none","avg"]'),
(@smEntityId, 'Quantity',      'Số lượng (delta)',   'Số lượng thay đổi (dương=nhập, âm=xuất)',               'integer', '["sum","none"]'),
(@smEntityId, 'ImportQty',     'SL nhập',           'Số lượng nhập (NULL nếu là xuất)',                      'integer', '["sum","none"]'),
(@smEntityId, 'ImportValue',   'Tiền nhập',         'Giá trị nhập kho (ImportQty × CostPrice)',              'decimal', '["sum","none"]'),
(@smEntityId, 'ExportQty',     'SL xuất',           'Số lượng xuất (NULL nếu là nhập)',                      'integer', '["sum","none"]'),
(@smEntityId, 'ExportValue',   'Tiền xuất',         'Giá trị xuất kho (ExportQty × CostPrice)',              'decimal', '["sum","none"]'),
(@smEntityId, 'BalanceAfter',  'Tồn kho (SL)',       'Số lượng tồn sau giao dịch',                           'integer', '["none"]'),
(@smEntityId, 'BalanceValue',  'Tồn kho (GT)',       'Giá trị tồn sau giao dịch (BalanceAfter × CostPrice)', 'decimal', '["none"]');

-- ── 3. Link S2d TemplateFieldMappings to the new registry entries ─────────────
-- TemplateVersionId = 5 (S2d — Sổ kho XNT, from migration 061)
-- Match FieldCode → FieldCode in MappableFields to resolve SourceFieldId safely.

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'ReferenceId'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'so_hieu';

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'MovementDate'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'ngay';

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'Description'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'dien_giai';

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'Unit'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'dvt';

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'CostPrice'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'don_gia';

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'ImportQty'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'sl_nhap';

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'ImportValue'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'tien_nhap';

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'ExportQty'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'sl_xuat';

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'ExportValue'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'tien_xuat';

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'BalanceAfter'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'sl_ton';

UPDATE TemplateFieldMappings tfm
    JOIN MappableFields mf ON mf.EntityId = @smEntityId AND mf.FieldCode = 'BalanceValue'
SET tfm.SourceEntityId = @smEntityId,
    tfm.SourceFieldId  = mf.FieldId
WHERE tfm.TemplateVersionId = 5 AND tfm.FieldCode = 'tien_ton';

-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('125_seed_stock_movements_mappable_entity', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
