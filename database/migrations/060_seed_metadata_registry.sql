-- =============================================
-- Migration 060: Seed Metadata Registry (MappableEntities + MappableFields)
-- =============================================

-- 6 MappableEntities
INSERT INTO MappableEntities (EntityCode, DisplayName, Description, Category) VALUES
('orders',         'Đơn hàng',              'Đơn hàng đã hoàn tất (completed)',     'revenue'),
('order_details',  'Chi tiết đơn hàng',     'Dòng sản phẩm trong đơn hàng',         'revenue'),
('gl_entries',     'Sổ cái (GL)',           'Bút toán sổ cái tài chính',            'general'),
('costs',          'Chi phí',               'Chi phí (nhập hàng + thủ công)',        'cost'),
('tax_payments',   'Thuế đã nộp',          'Ghi nhận nộp thuế VAT/TNCN',           'tax'),
('products',       'Sản phẩm',             'Thông tin sản phẩm',                    'revenue');

-- MappableFields (EntityId references above, auto-increment from 1)

-- Entity: orders (EntityId = 1)
INSERT INTO MappableFields (EntityId, FieldCode, DisplayName, Description, DataType, AllowedAggregations) VALUES
(1, 'TotalAmount',   'Tổng tiền',        'Tổng tiền đơn hàng (sau giảm giá)',         'decimal',  '["sum","avg","none"]'),
(1, 'CompletedAt',   'Ngày hoàn tất',    'Thời điểm đơn hàng được hoàn tất',          'date',     '["none"]'),
(1, 'OrderCode',     'Mã đơn hàng',      'Mã đơn hàng tự sinh',                        'text',     '["none","count"]'),
(1, 'Status',        'Trạng thái',       'completed | cancelled | ...',                 'text',     '["none"]'),
(1, 'CustomerName',  'Tên khách hàng',   'Tên khách hàng (nếu có)',                     'text',     '["none"]');

-- Entity: order_details (EntityId = 2)
INSERT INTO MappableFields (EntityId, FieldCode, DisplayName, Description, DataType, AllowedAggregations) VALUES
(2, 'ProductName',   'Tên sản phẩm',     'Tên sản phẩm trong đơn',                    'text',     '["none"]'),
(2, 'Quantity',      'Số lượng',         'Số lượng mua',                                'decimal',  '["sum","none"]'),
(2, 'Unit',          'Đơn vị tính',      'Bao, Kg, Cây...',                             'text',     '["none"]'),
(2, 'UnitPrice',     'Đơn giá',          'Giá bán 1 đơn vị',                            'decimal',  '["avg","none"]'),
(2, 'Amount',        'Thành tiền',       'Quantity × UnitPrice',                         'decimal',  '["sum","none"]');

-- Entity: gl_entries (EntityId = 3)
INSERT INTO MappableFields (EntityId, FieldCode, DisplayName, Description, DataType, AllowedAggregations) VALUES
(3, 'EntryDate',       'Ngày bút toán',     'Ngày ghi sổ',                               'date',     '["none"]'),
(3, 'Description',     'Diễn giải',        'Mô tả nội dung giao dịch',                   'text',     '["none"]'),
(3, 'DebitAmount',     'Số tiền ghi nợ',   'Tiền vào (thu)',                              'decimal',  '["sum","avg","none"]'),
(3, 'CreditAmount',   'Số tiền ghi có',   'Tiền ra (chi)',                               'decimal',  '["sum","avg","none"]'),
(3, 'TransactionType', 'Loại giao dịch',   'sale | import_cost | manual_cost | ...',      'text',     '["none"]'),
(3, 'MoneyChannel',    'Kênh tiền',        'cash | bank | debt',                          'text',     '["none"]');

-- Entity: costs (EntityId = 4)
INSERT INTO MappableFields (EntityId, FieldCode, DisplayName, Description, DataType, AllowedAggregations) VALUES
(4, 'CostDate',       'Ngày chi phí',     'Ngày phát sinh chi phí',                     'date',     '["none"]'),
(4, 'CostAmount',     'Số tiền',          'Giá trị chi phí',                             'decimal',  '["sum","avg","none"]'),
(4, 'CostCategory',   'Loại chi phí',     'rent | utilities | import | other',           'text',     '["none"]'),
(4, 'CostDescription','Mô tả',            'Nội dung chi phí',                            'text',     '["none"]'),
(4, 'HasInvoice',     'Có hóa đơn',       'Có chứng từ hay không',                       'boolean',  '["none"]');

-- Entity: tax_payments (EntityId = 5)
INSERT INTO MappableFields (EntityId, FieldCode, DisplayName, Description, DataType, AllowedAggregations) VALUES
(5, 'TaxType',         'Loại thuế',         'VAT | PIT',                                 'text',     '["none"]'),
(5, 'Amount',          'Số tiền nộp',       'Số tiền thuế đã nộp',                       'decimal',  '["sum","none"]'),
(5, 'PaidAt',          'Ngày nộp',          'Ngày nộp thuế',                              'date',     '["none"]'),
(5, 'PaymentMethod',   'Hình thức nộp',     'cash | bank',                               'text',     '["none"]'),
(5, 'ReferenceNumber', 'Số biên lai',       'Mã giao dịch / số biên lai',                'text',     '["none"]');

-- Entity: products (EntityId = 6)
INSERT INTO MappableFields (EntityId, FieldCode, DisplayName, Description, DataType, AllowedAggregations) VALUES
(6, 'ProductName',     'Tên sản phẩm',     'Tên sản phẩm trong danh mục',               'text',     '["none"]'),
(6, 'Unit',            'ĐVT',              'Đơn vị tính',                                'text',     '["none"]'),
(6, 'BusinessTypeId',  'Ngành nghề',       'ID ngành nghề (để tách sổ theo ngành)',       'text',     '["none"]');

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('060_seed_metadata_registry', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
