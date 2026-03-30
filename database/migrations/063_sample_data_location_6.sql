-- Migration 063: Sample Data for Business Location ID 6
-- Revenue, Costs, GeneralLedger entries for Q1/2026 (Jan-Mar 2026)

SET @createdBy = 'ff45309c-7b0b-4012-933b-042405d75685';

-- ─────────────────────────────────────────────────────
-- 1. REVENUES (20 entries, Q1/2026)
-- ─────────────────────────────────────────────────────
INSERT INTO Revenues (BusinessLocationId, RevenueDate, Amount, RevenueType, Description, MoneyChannel, CreatedBy, CreatedAt) VALUES
(6,'2026-01-05',3500000,'sale','Ban hang 5/1','cash',@createdBy,NOW()),
(6,'2026-01-10',5200000,'sale','Ban hang 10/1','bank',@createdBy,NOW()),
(6,'2026-01-15',4800000,'sale','Ban hang 15/1','cash',@createdBy,NOW()),
(6,'2026-01-20',6100000,'sale','Ban hang 20/1','bank',@createdBy,NOW()),
(6,'2026-01-25',3900000,'sale','Ban hang 25/1','cash',@createdBy,NOW()),
(6,'2026-01-31',7200000,'sale','Ban hang cuoi T1','bank',@createdBy,NOW()),
(6,'2026-02-03',4100000,'sale','Ban hang dau T2','cash',@createdBy,NOW()),
(6,'2026-02-10',5500000,'sale','Ban hang 10/2','bank',@createdBy,NOW()),
(6,'2026-02-14',8900000,'sale','Valentine 14/2','cash',@createdBy,NOW()),
(6,'2026-02-20',4600000,'sale','Ban hang 20/2','bank',@createdBy,NOW()),
(6,'2026-02-28',6300000,'sale','Ban hang cuoi T2','cash',@createdBy,NOW()),
(6,'2026-03-05',4200000,'sale','Ban hang 5/3','bank',@createdBy,NOW()),
(6,'2026-03-10',5800000,'sale','Ban hang 10/3','cash',@createdBy,NOW()),
(6,'2026-03-15',6700000,'sale','Ban hang 15/3','bank',@createdBy,NOW()),
(6,'2026-03-20',4900000,'sale','Ban hang 20/3','cash',@createdBy,NOW()),
(6,'2026-03-25',3800000,'sale','Ban hang 25/3','bank',@createdBy,NOW()),
(6,'2026-03-31',9500000,'sale','Ban hang cuoi Q1','cash',@createdBy,NOW()),
(6,'2026-01-15',1200000,'manual','Phi dich vu T1','bank',@createdBy,NOW()),
(6,'2026-02-15',1200000,'manual','Phi dich vu T2','cash',@createdBy,NOW()),
(6,'2026-03-15',1500000,'manual','Phi dich vu T3','bank',@createdBy,NOW());

-- ─────────────────────────────────────────────────────
-- 2. COSTS (17 entries, Q1/2026)
-- ─────────────────────────────────────────────────────
INSERT INTO Costs (BusinessLocationId, CostDate, Amount, CostType, Description, PaymentMethod, CreatedBy, CreatedAt) VALUES
(6,'2026-01-02',2100000,'manual','Nhap nguyen lieu T1','cash',@createdBy,NOW()),
(6,'2026-01-08',800000,'utilities','Tien dien T1','bank',@createdBy,NOW()),
(6,'2026-01-08',600000,'utilities','Tien nuoc T1','bank',@createdBy,NOW()),
(6,'2026-01-10',1500000,'manual','Nhap them hang T1','cash',@createdBy,NOW()),
(6,'2026-01-15',400000,'transport','Chi phi van chuyen T1','cash',@createdBy,NOW()),
(6,'2026-01-25',500000,'maintenance','Sua chua thiet bi T1','cash',@createdBy,NOW()),
(6,'2026-02-01',1800000,'manual','Nhap nguyen lieu T2','cash',@createdBy,NOW()),
(6,'2026-02-08',750000,'utilities','Tien dien T2','bank',@createdBy,NOW()),
(6,'2026-02-08',550000,'utilities','Tien nuoc T2','bank',@createdBy,NOW()),
(6,'2026-02-12',300000,'marketing','Chi phi marketing T2','bank',@createdBy,NOW()),
(6,'2026-02-20',1200000,'manual','Nhap them hang T2','cash',@createdBy,NOW()),
(6,'2026-03-01',2200000,'manual','Nhap nguyen lieu T3','cash',@createdBy,NOW()),
(6,'2026-03-08',820000,'utilities','Tien dien T3','bank',@createdBy,NOW()),
(6,'2026-03-08',580000,'utilities','Tien nuoc T3','bank',@createdBy,NOW()),
(6,'2026-03-15',700000,'salary','Chi nhan cong T3','cash',@createdBy,NOW()),
(6,'2026-03-20',1600000,'manual','Nhap them hang cuoi Q1','cash',@createdBy,NOW()),
(6,'2026-03-30',450000,'maintenance','Bao tri thiet bi T3','bank',@createdBy,NOW());

-- ─────────────────────────────────────────────────────
-- 3. GENERAL LEDGER (40 entries: cash + bank)
-- ─────────────────────────────────────────────────────
INSERT INTO GeneralLedgerEntries (BusinessLocationId, EntryDate, DebitAmount, CreditAmount, MoneyChannel, TransactionType, Description, ReferenceId, ReferenceType, CreatedAt) VALUES
-- Cash: Thu
(6,'2026-01-05',3500000,0,'cash','manual_revenue','Thu tien mat 5/1',NULL,'revenue',NOW()),
(6,'2026-01-15',4800000,0,'cash','manual_revenue','Thu tien mat 15/1',NULL,'revenue',NOW()),
(6,'2026-01-25',3900000,0,'cash','manual_revenue','Thu tien mat 25/1',NULL,'revenue',NOW()),
(6,'2026-02-03',4100000,0,'cash','manual_revenue','Thu tien mat 3/2',NULL,'revenue',NOW()),
(6,'2026-02-14',8900000,0,'cash','manual_revenue','Thu Valentine 14/2',NULL,'revenue',NOW()),
(6,'2026-02-28',6300000,0,'cash','manual_revenue','Thu tien mat cuoi T2',NULL,'revenue',NOW()),
(6,'2026-03-10',5800000,0,'cash','manual_revenue','Thu tien mat 10/3',NULL,'revenue',NOW()),
(6,'2026-03-20',4900000,0,'cash','manual_revenue','Thu tien mat 20/3',NULL,'revenue',NOW()),
(6,'2026-03-31',9500000,0,'cash','manual_revenue','Thu tien mat cuoi Q1',NULL,'revenue',NOW()),
-- Cash: Chi
(6,'2026-01-02',0,2100000,'cash','manual_cost','Chi nhap hang T1',NULL,'cost',NOW()),
(6,'2026-01-10',0,1500000,'cash','manual_cost','Chi nhap them T1',NULL,'cost',NOW()),
(6,'2026-01-15',0,400000,'cash','manual_cost','Chi van chuyen T1',NULL,'cost',NOW()),
(6,'2026-01-25',0,500000,'cash','manual_cost','Chi sua chua T1',NULL,'cost',NOW()),
(6,'2026-02-01',0,1800000,'cash','manual_cost','Chi nhap hang T2',NULL,'cost',NOW()),
(6,'2026-02-20',0,1200000,'cash','manual_cost','Chi nhap them T2',NULL,'cost',NOW()),
(6,'2026-03-01',0,2200000,'cash','manual_cost','Chi nhap hang T3',NULL,'cost',NOW()),
(6,'2026-03-15',0,700000,'cash','manual_cost','Chi nhan cong T3',NULL,'cost',NOW()),
(6,'2026-03-20',0,1600000,'cash','manual_cost','Chi nhap them T3',NULL,'cost',NOW()),
-- Bank: Thu
(6,'2026-01-10',5200000,0,'bank','manual_revenue','Thu NH 10/1',NULL,'revenue',NOW()),
(6,'2026-01-20',6100000,0,'bank','manual_revenue','Thu NH 20/1',NULL,'revenue',NOW()),
(6,'2026-01-31',7200000,0,'bank','manual_revenue','Thu NH cuoi T1',NULL,'revenue',NOW()),
(6,'2026-01-15',1200000,0,'bank','manual_revenue','Thu NH phi dich vu T1',NULL,'revenue',NOW()),
(6,'2026-02-10',5500000,0,'bank','manual_revenue','Thu NH 10/2',NULL,'revenue',NOW()),
(6,'2026-02-20',4600000,0,'bank','manual_revenue','Thu NH 20/2',NULL,'revenue',NOW()),
(6,'2026-03-05',4200000,0,'bank','manual_revenue','Thu NH 5/3',NULL,'revenue',NOW()),
(6,'2026-03-15',6700000,0,'bank','manual_revenue','Thu NH 15/3',NULL,'revenue',NOW()),
(6,'2026-03-25',3800000,0,'bank','manual_revenue','Thu NH 25/3',NULL,'revenue',NOW()),
(6,'2026-03-15',1500000,0,'bank','manual_revenue','Thu NH phi dich vu T3',NULL,'revenue',NOW()),
-- Bank: Chi
(6,'2026-01-08',0,800000,'bank','manual_cost','Chi NH dien T1',NULL,'cost',NOW()),
(6,'2026-01-08',0,600000,'bank','manual_cost','Chi NH nuoc T1',NULL,'cost',NOW()),
(6,'2026-02-08',0,750000,'bank','manual_cost','Chi NH dien T2',NULL,'cost',NOW()),
(6,'2026-02-08',0,550000,'bank','manual_cost','Chi NH nuoc T2',NULL,'cost',NOW()),
(6,'2026-02-12',0,300000,'bank','manual_cost','Chi NH marketing T2',NULL,'cost',NOW()),
(6,'2026-03-08',0,820000,'bank','manual_cost','Chi NH dien T3',NULL,'cost',NOW()),
(6,'2026-03-08',0,580000,'bank','manual_cost','Chi NH nuoc T3',NULL,'cost',NOW()),
(6,'2026-03-30',0,450000,'bank','manual_cost','Chi NH bao tri T3',NULL,'cost',NOW());

-- ─────────────────────────────────────────────────────
-- 4. Ensure AccountingPeriod 1 is 'open' for location 6
-- ─────────────────────────────────────────────────────
UPDATE AccountingPeriods SET Status = 'open'
WHERE PeriodId = 1 AND BusinessLocationId = 6;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('063_sample_data_location_6', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
