-- Link FormulaId for S2d (TemplateVersionId = 5) formula field mappings.
-- Previously these mappings stored only a human-readable FormulaExpression string
-- and were never evaluated. This migration wires each mapping to the correct
-- FormulaDefinition row so BuildFormulaFieldValues can resolve them.

UPDATE TemplateFieldMappings fm
JOIN FormulaDefinitions fd ON fd.Code = 'S2D_OPENING_QTY'
SET fm.FormulaId = fd.FormulaId
WHERE fm.TemplateVersionId = 5 AND fm.FieldCode = 'ton_dau_ky_sl';

UPDATE TemplateFieldMappings fm
JOIN FormulaDefinitions fd ON fd.Code = 'S2D_OPENING_VALUE'
SET fm.FormulaId = fd.FormulaId
WHERE fm.TemplateVersionId = 5 AND fm.FieldCode = 'ton_dau_ky_gt';

UPDATE TemplateFieldMappings fm
JOIN FormulaDefinitions fd ON fd.Code = 'S2D_WEIGHTED_AVG'
SET fm.FormulaId = fd.FormulaId
WHERE fm.TemplateVersionId = 5 AND fm.FieldCode = 'don_gia_xuat';

UPDATE TemplateFieldMappings fm
JOIN FormulaDefinitions fd ON fd.Code = 'S2D_CLOSING_QTY'
SET fm.FormulaId = fd.FormulaId
WHERE fm.TemplateVersionId = 5 AND fm.FieldCode = 'ton_cuoi_ky_sl';

UPDATE TemplateFieldMappings fm
JOIN FormulaDefinitions fd ON fd.Code = 'S2D_CLOSING_VALUE'
SET fm.FormulaId = fd.FormulaId
WHERE fm.TemplateVersionId = 5 AND fm.FieldCode = 'ton_cuoi_ky_gt';

-- Fix VisibleFieldCodes for S2d balance_row definitions.
-- Previously said ["dien_giai","so_tien"] but the rendering code writes to sl_ton/tien_ton.
UPDATE TemplateRowDefinitions
SET VisibleFieldCodes = '["dien_giai","sl_ton","tien_ton"]'
WHERE TemplateVersionId = 5 AND RowType = 'balance_row';

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('133_link_s2d_formula_ids', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
