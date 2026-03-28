-- =============================================
-- Migration 061: Seed TemplateFieldMappings for all 6 templates
-- =============================================
-- Assumes TemplateVersionId 1-6 from migration 059
-- Assumes MappableEntities EntityId 1-6, MappableFields FieldId 1-28 from migration 060

-- ═══ S1a (TemplateVersionId = 1) — Sổ chi tiết bán hàng ═══
-- orders.CompletedAt FieldId=2, orders.OrderCode FieldId=3, orders.TotalAmount FieldId=1
INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, SourceEntityId, SourceFieldId, AggregationType, ExportColumn, SortOrder, IsRequired) VALUES
(1, 'stt',         'STT',              'auto_increment', 'auto',    NULL, NULL, NULL,   'A', 1, TRUE),
(1, 'date',        'Ngày tháng',       'date',           'query',   1,    2,    'none', 'B', 2, TRUE),
(1, 'description', 'Nội dung',         'text',           'query',   1,    3,    'none', 'C', 3, TRUE),
(1, 'revenue',     'Doanh thu bán hàng','decimal',       'query',   1,    1,    'none', 'D', 4, TRUE);


-- ═══ S2a (TemplateVersionId = 2) — Sổ doanh thu Cách 1 ═══
INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, SourceEntityId, SourceFieldId, AggregationType, ExportColumn, SortOrder, IsRequired) VALUES
(2, 'stt',         'STT',              'auto_increment', 'auto',    NULL, NULL, NULL,   'A', 1, TRUE),
(2, 'so_hieu',     'Chứng từ - Số hiệu', 'text',        'query',   1,    3,    'none', 'B', 2, TRUE),
(2, 'ngay_thang',  'Chứng từ - Ngày tháng', 'date',     'query',   1,    2,    'none', 'C', 3, TRUE),
(2, 'dien_giai',   'Diễn giải',        'text',           'query',   1,    3,    'none', 'D', 4, TRUE),
(2, 'so_tien',     'Số tiền (1)',       'decimal',        'query',   1,    1,    'none', 'E', 5, TRUE);

-- S2a computed rows (FormulaId will be set after 062)
INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, FormulaExpression, CalculationOrder, ExportColumn, SortOrder, IsRequired) VALUES
(2, 'cong_quy',    'Cộng quý',              'decimal', 'formula', 'SUM(so_tien)',                         1, 'E', 10, TRUE),
(2, 'thue_gtgt',   'Thuế GTGT phải nộp',    'decimal', 'formula', 'cong_quy * VAT_RATE',                  2, 'E', 11, TRUE),
(2, 'thue_tncn',   'Thuế TNCN phải nộp',    'decimal', 'formula', 'MAX(0, TOTAL_REVENUE - 500M) * PIT_RATE', 3, 'E', 12, TRUE);


-- ═══ S2b (TemplateVersionId = 3) — Sổ doanh thu Cách 2 ═══
INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, SourceEntityId, SourceFieldId, AggregationType, ExportColumn, SortOrder, IsRequired) VALUES
(3, 'stt',         'STT',              'auto_increment', 'auto',    NULL, NULL, NULL,   'A', 1, TRUE),
(3, 'so_hieu',     'Chứng từ - Số hiệu', 'text',        'query',   1,    3,    'none', 'B', 2, TRUE),
(3, 'ngay_thang',  'Chứng từ - Ngày tháng', 'date',     'query',   1,    2,    'none', 'C', 3, TRUE),
(3, 'dien_giai',   'Diễn giải',        'text',           'query',   1,    3,    'none', 'D', 4, TRUE),
(3, 'so_tien',     'Số tiền (1)',       'decimal',        'query',   1,    1,    'none', 'E', 5, TRUE);

INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, FormulaExpression, CalculationOrder, ExportColumn, SortOrder, IsRequired) VALUES
(3, 'cong_quy',    'Cộng quý',              'decimal', 'formula', 'SUM(so_tien)',       1, 'E', 10, TRUE),
(3, 'thue_gtgt',   'Thuế GTGT phải nộp',    'decimal', 'formula', 'cong_quy * VAT_RATE', 2, 'E', 11, TRUE);


-- ═══ S2c (TemplateVersionId = 4) — Sổ chi tiết DT, CP ═══
INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, SourceEntityId, SourceFieldId, AggregationType, ExportColumn, SortOrder, IsRequired) VALUES
(4, 'stt',         'STT',              'auto_increment', 'auto',    NULL, NULL, NULL,   'A', 1, TRUE),
(4, 'so_hieu',     'Chứng từ - Số hiệu', 'text',        'query',   1,    3,    'none', 'B', 2, TRUE),
(4, 'ngay_thang',  'Chứng từ - Ngày tháng', 'date',     'query',   1,    2,    'none', 'C', 3, TRUE),
(4, 'dien_giai',   'Diễn giải',        'text',           'query',   1,    3,    'none', 'D', 4, TRUE),
(4, 'so_tien',     'Số tiền (1)',       'decimal',        'query',   1,    1,    'none', 'E', 5, TRUE),
(4, 'section',     'Phần',             'text',           'static',  NULL, NULL, NULL,   NULL, 6, FALSE);

INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, FormulaExpression, CalculationOrder, ExportColumn, SortOrder, IsRequired) VALUES
(4, 'tong_dt',     'Tổng doanh thu',        'decimal', 'formula', 'SUM(so_tien WHERE section=revenue)',    1, 'E', 10, TRUE),
(4, 'tong_cp',     'Tổng chi phí hợp lý',   'decimal', 'formula', 'SUM(so_tien WHERE section=cost)',       2, 'E', 11, TRUE),
(4, 'chenh_lech',  'Chênh lệch (DT - CP)', 'decimal',  'formula', 'tong_dt - tong_cp',                     3, 'E', 12, TRUE),
(4, 'thue_tncn',   'Thuế TNCN phải nộp',    'decimal', 'formula', 'MAX(0, chenh_lech) * PIT_RATE_M2',      4, 'E', 13, TRUE);


-- ═══ S2d (TemplateVersionId = 5) — Sổ kho XNT ═══
INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, ExportColumn, SortOrder, IsRequired) VALUES
(5, 'so_hieu',     'Chứng từ - Số hiệu',        'text',    'query',  'A', 1, TRUE),
(5, 'ngay',        'Chứng từ - Ngày',            'date',    'query',  'B', 2, TRUE),
(5, 'dien_giai',   'Diễn giải',                  'text',    'query',  'C', 3, TRUE),
(5, 'dvt',         'ĐVT',                        'text',    'query',  'D', 4, TRUE),
(5, 'don_gia',     'Đơn giá (1)',                 'decimal', 'query',  'E', 5, TRUE),
(5, 'sl_nhap',     'Nhập - SL (2)',              'decimal', 'query',  'F', 6, FALSE),
(5, 'tien_nhap',   'Nhập - Tiền (3)',            'decimal', 'query',  'G', 7, FALSE),
(5, 'sl_xuat',     'Xuất - SL (4)',              'decimal', 'query',  'H', 8, FALSE),
(5, 'tien_xuat',   'Xuất - Tiền (5)',            'decimal', 'query',  'I', 9, FALSE),
(5, 'sl_ton',      'Tồn - SL (6)',              'decimal', 'query',  'J', 10, TRUE),
(5, 'tien_ton',    'Tồn - Tiền (7)',            'decimal', 'query',  'K', 11, TRUE);

INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, FormulaExpression, CalculationOrder, ExportColumn, SortOrder, IsRequired) VALUES
(5, 'ton_dau_ky_sl',  'Tồn đầu kỳ - SL',       'decimal', 'formula', 'SUM(qty_delta BEFORE period)',     1, 'J', 20, TRUE),
(5, 'ton_dau_ky_gt',  'Tồn đầu kỳ - GT',       'decimal', 'formula', 'SUM(value BEFORE period)',          2, 'K', 21, TRUE),
(5, 'don_gia_xuat',   'Đơn giá xuất BQ',        'decimal', 'formula', '(ton_dau_ky_gt + nhap_gt) / (ton_dau_ky_sl + nhap_sl)', 3, 'E', 22, TRUE),
(5, 'ton_cuoi_ky_sl', 'Tồn cuối kỳ - SL',      'decimal', 'formula', 'ton_dau_ky_sl + nhap_sl - xuat_sl', 4, 'J', 23, TRUE),
(5, 'ton_cuoi_ky_gt', 'Tồn cuối kỳ - GT',      'decimal', 'formula', 'ton_cuoi_ky_sl * don_gia_xuat',     5, 'K', 24, TRUE);


-- ═══ S2e (TemplateVersionId = 6) — Sổ chi tiết tiền ═══
INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, SourceEntityId, SourceFieldId, AggregationType, ExportColumn, SortOrder, IsRequired) VALUES
(6, 'stt',         'STT',                   'auto_increment', 'auto',    NULL, NULL, NULL,   'A', 1, TRUE),
(6, 'so_hieu',     'Chứng từ - Số hiệu',   'text',           'query',   3,    12,   'none', 'B', 2, TRUE),
(6, 'ngay_thang',  'Chứng từ - Ngày tháng', 'date',           'query',   3,    11,   'none', 'C', 3, TRUE),
(6, 'dien_giai',   'Diễn giải',             'text',           'query',   3,    12,   'none', 'D', 4, TRUE),
(6, 'thu_vao',     'Thu/Gửi vào (1)',       'decimal',        'query',   3,    13,   'none', 'E', 5, FALSE),
(6, 'chi_ra',      'Chi/Rút ra (2)',        'decimal',        'query',   3,    14,   'none', 'F', 6, FALSE),
(6, 'section',     'Phần (cash|bank)',      'text',           'static',  NULL, NULL, NULL,   NULL, 7, FALSE);

INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, FormulaExpression, CalculationOrder, ExportColumn, SortOrder, IsRequired) VALUES
(6, 'cash_opening',  'Tiền mặt đầu kỳ',          'decimal', 'formula', 'LOOKUP(period.OpeningCashBalance)',  1, 'E', 20, TRUE),
(6, 'cash_in',       'Tổng thu tiền mặt',         'decimal', 'formula', 'SUM(DebitAmount WHERE cash)',        2, 'E', 21, TRUE),
(6, 'cash_out',      'Tổng chi tiền mặt',         'decimal', 'formula', 'SUM(CreditAmount WHERE cash)',       3, 'F', 22, TRUE),
(6, 'cash_closing',  'Tiền mặt tồn cuối kỳ',     'decimal', 'formula', 'cash_opening + cash_in - cash_out',  4, 'E', 23, TRUE),
(6, 'bank_opening',  'Tiền gửi đầu kỳ',          'decimal', 'formula', 'LOOKUP(period.OpeningBankBalance)',  5, 'E', 24, TRUE),
(6, 'bank_in',       'Tổng gửi vào',             'decimal', 'formula', 'SUM(DebitAmount WHERE bank)',        6, 'E', 25, TRUE),
(6, 'bank_out',      'Tổng rút ra',              'decimal', 'formula', 'SUM(CreditAmount WHERE bank)',       7, 'F', 26, TRUE),
(6, 'bank_closing',  'Dư tiền gửi cuối kỳ',      'decimal', 'formula', 'bank_opening + bank_in - bank_out',  8, 'E', 27, TRUE);

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('061_seed_template_field_mappings', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
