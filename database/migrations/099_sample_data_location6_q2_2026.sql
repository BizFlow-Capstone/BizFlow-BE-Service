-- Migration 096: Sample Data for Business Location ID 6 — Q2/2026 (Apr–Jun 2026)
-- Covers 5 business types: Beauty Salon, Dịch vụ, Grocery Store, Phân phối, Restaurant & Cafe
-- Revenues + Costs both carry BusinessTypeId for per-industry tax calculation

-- Normalize session collation to avoid cross-version collation conflicts.
SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;
SET SESSION collation_connection = 'utf8mb4_unicode_ci';

SET @seed_profile_id = 'ff45309c-7b0b-4012-933b-042405d75685';
SET @seed_account_id = 'ff45309c-7b0b-4012-933b-042405d75685';
SET @seed_credential_id = 'ff45309c-7b0b-4012-933b-042405d75686';
SET @seed_email = 'seed.location6@bizflow.local';
SET @seed_full_name = 'Seed Operator Location 6';
SET @seed_password_hash = '$2a$12$ZXWiNvtS/vwl4JJWmso.j.pNoFbpI9gLDWpiPFaWny45pR0aIIaea';

SET @target_location_id = 6;
SET @seed_role_id = (
    SELECT RoleId
    FROM Roles
    WHERE Name COLLATE utf8mb4_unicode_ci = 'user' COLLATE utf8mb4_unicode_ci
    LIMIT 1
);
SET @seed_role_id = IFNULL(@seed_role_id, (SELECT RoleId FROM Roles LIMIT 1));

INSERT INTO Accounts (AccountId, RoleId, PasswordHash, IsActive, LastLoginAt, CreatedAt, UpdatedAt, DeletedAt)
SELECT @seed_account_id, @seed_role_id, @seed_password_hash, TRUE, NULL, NOW(), NOW(), NULL
WHERE NOT EXISTS (
    SELECT 1
    FROM Accounts
    WHERE AccountId = @seed_account_id
);

UPDATE Accounts
SET RoleId = COALESCE(@seed_role_id, RoleId),
    IsActive = TRUE,
    DeletedAt = NULL,
    UpdatedAt = NOW()
WHERE AccountId = @seed_account_id;

INSERT INTO Profiles (ProfileId, AccountId, FullName, AvatarUrl, TaxCode, UpdatedAt)
SELECT @seed_profile_id, @seed_account_id, @seed_full_name, NULL, NULL, NOW()
WHERE NOT EXISTS (
    SELECT 1
    FROM Profiles
    WHERE ProfileId = @seed_profile_id
);

UPDATE Profiles
SET FullName = @seed_full_name,
    UpdatedAt = NOW()
WHERE ProfileId = @seed_profile_id;

INSERT INTO Credentials (CredentialId, AccountId, Type, Identifier, EmailVerified, GoogleEmail, CreatedAt)
SELECT @seed_credential_id, @seed_account_id, 'email', @seed_email, TRUE, NULL, NOW()
WHERE NOT EXISTS (
    SELECT 1
    FROM Credentials
    WHERE Type = 'email'
      AND Identifier COLLATE utf8mb4_unicode_ci = @seed_email COLLATE utf8mb4_unicode_ci
)
AND NOT EXISTS (
    SELECT 1
    FROM Credentials
    WHERE AccountId = @seed_account_id
      AND Type = 'email'
);

INSERT INTO BusinessLocations (
    BusinessLocationId,
    LocationName,
    Address,
    Status,
    IsActive,
    TaxCode,
    CreatedAt,
    UpdatedAt,
    DeletedAt
)
SELECT
    @target_location_id,
    'BizFlow Sample Location 6',
    'Sample address for location 6',
    'active',
    TRUE,
    'TAX-LOC6-SEED',
    NOW(),
    NOW(),
    NULL
WHERE NOT EXISTS (
    SELECT 1
    FROM BusinessLocations
    WHERE BusinessLocationId = @target_location_id
);

INSERT INTO UserLocationAssignments (UserId, BusinessLocationId, IsOwner, IsActive, AssignedAt, UnassignedAt)
SELECT @seed_profile_id, @target_location_id, TRUE, TRUE, NOW(), NULL
WHERE NOT EXISTS (
    SELECT 1
    FROM UserLocationAssignments
    WHERE BusinessLocationId = @target_location_id
      AND IsOwner = TRUE
      AND IsActive = TRUE
);

SET @createdBy  = @seed_profile_id;
SET @beautyId   = '650e8400-e29b-41d4-a716-446655440004'; -- Beauty Salon
SET @serviceId  = '11111111-1111-1111-1111-111111111002'; -- Dịch vụ
SET @groceryId  = '650e8400-e29b-41d4-a716-446655440003'; -- Grocery Store
SET @retailId   = '11111111-1111-1111-1111-111111111001'; -- Phân phối, cung cấp hàng hóa
SET @restaurantId = '650e8400-e29b-41d4-a716-446655440002'; -- Restaurant & Cafe

-- ─────────────────────────────────────────────────────────────
-- 0. ACCOUNTING PERIOD Q2/2026 for location 6
-- ─────────────────────────────────────────────────────────────
INSERT IGNORE INTO AccountingPeriods
    (BusinessLocationId, PeriodType, Year, Quarter, StartDate, EndDate, Status,
     OpeningCashBalance, OpeningBankBalance, CreatedAt)
VALUES
    (6, 'quarterly', 2026, 2, '2026-04-01', '2026-06-30', 'open',
     28000000.00, 95000000.00, NOW());

-- ─────────────────────────────────────────────────────────────
-- 1. REVENUES — Q2/2026 (50 entries, 5 business types)
-- ─────────────────────────────────────────────────────────────
INSERT INTO Revenues (BusinessLocationId, BusinessTypeId, RevenueDate, Amount, RevenueType, Description, MoneyChannel, CreatedBy, CreatedAt) VALUES
-- ── Beauty Salon ──────────────────────────────────────────
(6, @beautyId, '2026-04-03', 2500000, 'sale', 'Beauty: Dich vu cat toc thang 4',   'cash',  @createdBy, NOW()),
(6, @beautyId, '2026-04-12', 3200000, 'sale', 'Beauty: Uon nhuom thang 4',          'bank',  @createdBy, NOW()),
(6, @beautyId, '2026-04-25', 1800000, 'sale', 'Beauty: Cham soc da thang 4',        'cash',  @createdBy, NOW()),
(6, @beautyId, '2026-05-07', 3500000, 'sale', 'Beauty: Dich vu lam dep thang 5',   'cash',  @createdBy, NOW()),
(6, @beautyId, '2026-05-18', 2800000, 'sale', 'Beauty: Uon toc thang 5',            'bank',  @createdBy, NOW()),
(6, @beautyId, '2026-05-29', 4200000, 'sale', 'Beauty: Nhuom highlight thang 5',    'cash',  @createdBy, NOW()),
(6, @beautyId, '2026-06-06', 2200000, 'sale', 'Beauty: Cham soc toc thang 6',       'bank',  @createdBy, NOW()),
(6, @beautyId, '2026-06-17', 3800000, 'sale', 'Beauty: Uon nhuom thang 6',          'cash',  @createdBy, NOW()),
(6, @beautyId, '2026-06-28', 5600000, 'sale', 'Beauty: Goi dau + massage thang 6',  'bank',  @createdBy, NOW()),

-- ── Dịch vụ ───────────────────────────────────────────────
(6, @serviceId, '2026-04-05', 5000000, 'sale',   'Dich vu: Tu van ke toan thang 4',    'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-04-20', 8500000, 'sale',   'Dich vu: Phan mem quan ly thang 4',  'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-04-28', 2000000, 'manual', 'Dich vu: Phi duy tri he thong T4',   'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-05-10', 6200000, 'sale',   'Dich vu: Tu van thue thang 5',       'cash',  @createdBy, NOW()),
(6, @serviceId, '2026-05-22', 9800000, 'sale',   'Dich vu: Trien khai phan mem T5',    'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-05-31', 3500000, 'manual', 'Dich vu: Phi duy tri he thong T5',   'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-06-08', 7300000, 'sale',   'Dich vu: Ho tro ky thuat thang 6',   'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-06-25', 11200000, 'sale',  'Dich vu: Nang cap he thong T6',      'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-06-30', 2500000, 'manual', 'Dich vu: Phi duy tri he thong T6',   'bank',  @createdBy, NOW()),

-- ── Grocery Store ─────────────────────────────────────────
(6, @groceryId, '2026-04-02', 12500000, 'sale', 'Grocery: Ban hang dau T4',           'cash',  @createdBy, NOW()),
(6, @groceryId, '2026-04-10', 8300000,  'sale', 'Grocery: Ban hang 10/4',             'bank',  @createdBy, NOW()),
(6, @groceryId, '2026-04-18', 9700000,  'sale', 'Grocery: Ban hang 18/4',             'cash',  @createdBy, NOW()),
(6, @groceryId, '2026-04-30', 15200000, 'sale', 'Grocery: Ban hang cuoi T4',          'bank',  @createdBy, NOW()),
(6, @groceryId, '2026-05-06', 11800000, 'sale', 'Grocery: Ban hang dau T5',           'cash',  @createdBy, NOW()),
(6, @groceryId, '2026-05-14', 13500000, 'sale', 'Grocery: Ban hang 14/5',             'bank',  @createdBy, NOW()),
(6, @groceryId, '2026-05-22', 7200000,  'sale', 'Grocery: Ban hang 22/5',             'cash',  @createdBy, NOW()),
(6, @groceryId, '2026-05-31', 16900000, 'sale', 'Grocery: Ban hang cuoi T5',          'bank',  @createdBy, NOW()),
(6, @groceryId, '2026-06-04', 14200000, 'sale', 'Grocery: Ban hang dau T6',           'cash',  @createdBy, NOW()),
(6, @groceryId, '2026-06-14', 10800000, 'sale', 'Grocery: Ban hang 14/6',             'bank',  @createdBy, NOW()),
(6, @groceryId, '2026-06-22', 12600000, 'sale', 'Grocery: Ban hang 22/6',             'cash',  @createdBy, NOW()),
(6, @groceryId, '2026-06-30', 19500000, 'sale', 'Grocery: Ban hang cuoi Q2',          'bank',  @createdBy, NOW()),

-- ── Phân phối, cung cấp hàng hóa ─────────────────────────
(6, @retailId, '2026-04-05', 25000000, 'sale', 'Phan phoi: Lo hang T4 dot 1',        'bank',  @createdBy, NOW()),
(6, @retailId, '2026-04-22', 18500000, 'sale', 'Phan phoi: Lo hang T4 dot 2',        'bank',  @createdBy, NOW()),
(6, @retailId, '2026-05-08', 32000000, 'sale', 'Phan phoi: Lo hang T5 dot 1',        'bank',  @createdBy, NOW()),
(6, @retailId, '2026-05-20', 22800000, 'sale', 'Phan phoi: Lo hang T5 dot 2',        'bank',  @createdBy, NOW()),
(6, @retailId, '2026-05-30', 15200000, 'sale', 'Phan phoi: Lo hang T5 dot 3',        'cash',  @createdBy, NOW()),
(6, @retailId, '2026-06-10', 28500000, 'sale', 'Phan phoi: Lo hang T6 dot 1',        'bank',  @createdBy, NOW()),
(6, @retailId, '2026-06-28', 19300000, 'sale', 'Phan phoi: Lo hang T6 dot 2',        'bank',  @createdBy, NOW()),

-- ── Restaurant & Cafe ─────────────────────────────────────
(6, @restaurantId, '2026-04-04', 4500000,  'sale', 'Restaurant: Doanh thu an uong T4W1', 'cash', @createdBy, NOW()),
(6, @restaurantId, '2026-04-13', 3200000,  'sale', 'Restaurant: Doanh thu an uong T4W2', 'cash', @createdBy, NOW()),
(6, @restaurantId, '2026-04-27', 5800000,  'sale', 'Restaurant: Doanh thu an uong T4W4', 'cash', @createdBy, NOW()),
(6, @restaurantId, '2026-05-09', 6200000,  'sale', 'Restaurant: Doanh thu an uong T5W1', 'cash', @createdBy, NOW()),
(6, @restaurantId, '2026-05-24', 4800000,  'sale', 'Restaurant: Doanh thu an uong T5W3', 'cash', @createdBy, NOW()),
(6, @restaurantId, '2026-05-31', 7500000,  'sale', 'Restaurant: Tiec cuoi thang 5',      'bank', @createdBy, NOW()),
(6, @restaurantId, '2026-06-07', 5500000,  'sale', 'Restaurant: Doanh thu an uong T6W1', 'cash', @createdBy, NOW()),
(6, @restaurantId, '2026-06-20', 8200000,  'sale', 'Restaurant: Tiec sinh nhat T6',      'bank', @createdBy, NOW()),
(6, @restaurantId, '2026-06-30', 6300000,  'sale', 'Restaurant: Doanh thu an uong T6W4', 'cash', @createdBy, NOW());

-- ─────────────────────────────────────────────────────────────
-- 2. COSTS — Q2/2026 (35 entries, tagged by BusinessTypeId)
-- ─────────────────────────────────────────────────────────────
INSERT INTO Costs (BusinessLocationId, BusinessTypeId, CostDate, Amount, CostType, Description, PaymentMethod, CreatedBy, CreatedAt) VALUES
-- ── Beauty Salon ──────────────────────────────────────────
(6, @beautyId, '2026-04-02', 800000,  'manual',      'Beauty: Nguyen lieu my pham T4',     'cash',  @createdBy, NOW()),
(6, @beautyId, '2026-05-03', 950000,  'manual',      'Beauty: Nguyen lieu my pham T5',     'cash',  @createdBy, NOW()),
(6, @beautyId, '2026-06-02', 1100000, 'manual',      'Beauty: Nguyen lieu my pham T6',     'cash',  @createdBy, NOW()),
(6, @beautyId, '2026-04-15', 500000,  'utilities',   'Beauty: Tien dien + nuoc T4',        'bank',  @createdBy, NOW()),
(6, @beautyId, '2026-05-15', 500000,  'utilities',   'Beauty: Tien dien + nuoc T5',        'bank',  @createdBy, NOW()),
(6, @beautyId, '2026-06-15', 500000,  'utilities',   'Beauty: Tien dien + nuoc T6',        'bank',  @createdBy, NOW()),

-- ── Dịch vụ ───────────────────────────────────────────────
(6, @serviceId, '2026-04-06', 1500000, 'manual',     'Dich vu: Chi phi phan mem T4',       'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-05-06', 1500000, 'manual',     'Dich vu: Chi phi phan mem T5',       'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-06-06', 1500000, 'manual',     'Dich vu: Chi phi phan mem T6',       'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-04-20', 600000,  'marketing',  'Dich vu: Chi phi quang cao Q2T4',    'bank',  @createdBy, NOW()),
(6, @serviceId, '2026-06-20', 600000,  'marketing',  'Dich vu: Chi phi quang cao Q2T6',    'bank',  @createdBy, NOW()),

-- ── Grocery Store ─────────────────────────────────────────
(6, @groceryId, '2026-04-01', 28000000, 'manual',    'Grocery: Nhap hang dau T4',          'cash',  @createdBy, NOW()),
(6, @groceryId, '2026-04-16', 12000000, 'manual',    'Grocery: Nhap them hang T4',         'bank',  @createdBy, NOW()),
(6, @groceryId, '2026-05-01', 30000000, 'manual',    'Grocery: Nhap hang dau T5',          'cash',  @createdBy, NOW()),
(6, @groceryId, '2026-05-18', 10000000, 'manual',    'Grocery: Nhap them hang T5',         'bank',  @createdBy, NOW()),
(6, @groceryId, '2026-06-01', 35000000, 'manual',    'Grocery: Nhap hang dau T6',          'cash',  @createdBy, NOW()),
(6, @groceryId, '2026-06-18', 14000000, 'manual',    'Grocery: Nhap them hang T6',         'bank',  @createdBy, NOW()),
(6, @groceryId, '2026-04-08', 700000,   'utilities', 'Grocery: Dien nuoc T4',              'bank',  @createdBy, NOW()),
(6, @groceryId, '2026-05-08', 700000,   'utilities', 'Grocery: Dien nuoc T5',              'bank',  @createdBy, NOW()),
(6, @groceryId, '2026-06-08', 700000,   'utilities', 'Grocery: Dien nuoc T6',              'bank',  @createdBy, NOW()),

-- ── Phân phối, cung cấp hàng hóa ─────────────────────────
(6, @retailId, '2026-04-03', 35000000, 'manual',     'Phan phoi: Mua hang si T4',          'bank',  @createdBy, NOW()),
(6, @retailId, '2026-05-05', 52000000, 'manual',     'Phan phoi: Mua hang si T5',          'bank',  @createdBy, NOW()),
(6, @retailId, '2026-06-08', 38000000, 'manual',     'Phan phoi: Mua hang si T6',          'bank',  @createdBy, NOW()),
(6, @retailId, '2026-04-10', 1200000,  'transport',  'Phan phoi: Van chuyen T4',           'cash',  @createdBy, NOW()),
(6, @retailId, '2026-05-12', 1500000,  'transport',  'Phan phoi: Van chuyen T5',           'cash',  @createdBy, NOW()),
(6, @retailId, '2026-06-12', 1800000,  'transport',  'Phan phoi: Van chuyen T6',           'cash',  @createdBy, NOW()),

-- ── Restaurant & Cafe ─────────────────────────────────────
(6, @restaurantId, '2026-04-01', 3500000,  'manual',   'Restaurant: Nguyen lieu nau an T4',  'cash',  @createdBy, NOW()),
(6, @restaurantId, '2026-05-01', 4200000,  'manual',   'Restaurant: Nguyen lieu nau an T5',  'cash',  @createdBy, NOW()),
(6, @restaurantId, '2026-06-01', 4800000,  'manual',   'Restaurant: Nguyen lieu nau an T6',  'cash',  @createdBy, NOW()),
(6, @restaurantId, '2026-04-08', 600000,   'utilities','Restaurant: Dien nuoc T4',           'bank',  @createdBy, NOW()),
(6, @restaurantId, '2026-05-08', 600000,   'utilities','Restaurant: Dien nuoc T5',           'bank',  @createdBy, NOW()),
(6, @restaurantId, '2026-06-08', 600000,   'utilities','Restaurant: Dien nuoc T6',           'bank',  @createdBy, NOW()),
(6, @restaurantId, '2026-04-20', 400000,   'salary',   'Restaurant: Nhan cong part-time T4', 'cash',  @createdBy, NOW()),
(6, @restaurantId, '2026-05-20', 450000,   'salary',   'Restaurant: Nhan cong part-time T5', 'cash',  @createdBy, NOW()),
(6, @restaurantId, '2026-06-20', 500000,   'salary',   'Restaurant: Nhan cong part-time T6', 'cash',  @createdBy, NOW());

-- ─────────────────────────────────────────────────────────────
-- 3. GENERAL LEDGER — Q2/2026 (key cash & bank flows)
-- ─────────────────────────────────────────────────────────────
INSERT INTO GeneralLedgerEntries
    (BusinessLocationId, EntryDate, DebitAmount, CreditAmount, MoneyChannel,
     TransactionType, Description, ReferenceId, ReferenceType, CreatedAt)
VALUES
-- ── Cash: Thu ─────────────────────────────────────────────
(6,'2026-04-03', 2500000, 0,'cash','manual_revenue','Thu tien mat Beauty T4W1',     NULL,'revenue',NOW()),
(6,'2026-04-04', 4500000, 0,'cash','manual_revenue','Thu tien mat Restaurant T4W1', NULL,'revenue',NOW()),
(6,'2026-04-18', 9700000, 0,'cash','manual_revenue','Thu tien mat Grocery 18/4',    NULL,'revenue',NOW()),
(6,'2026-05-07', 3500000, 0,'cash','manual_revenue','Thu tien mat Beauty 7/5',      NULL,'revenue',NOW()),
(6,'2026-05-10', 6200000, 0,'cash','manual_revenue','Thu tien mat DichVu 10/5',     NULL,'revenue',NOW()),
(6,'2026-05-30', 15200000,0,'cash','manual_revenue','Thu tien mat Retail 30/5',     NULL,'revenue',NOW()),
(6,'2026-06-06', 2200000, 0,'cash','manual_revenue','Thu tien mat Beauty 6/6',      NULL,'revenue',NOW()),
(6,'2026-06-07', 5500000, 0,'cash','manual_revenue','Thu tien mat Restaurant 7/6',  NULL,'revenue',NOW()),
(6,'2026-06-04',14200000, 0,'cash','manual_revenue','Thu tien mat Grocery 4/6',     NULL,'revenue',NOW()),
(6,'2026-06-30', 6300000, 0,'cash','manual_revenue','Thu tien mat Restaurant cuoi Q2',NULL,'revenue',NOW()),
-- ── Cash: Chi ─────────────────────────────────────────────
(6,'2026-04-01', 0, 3500000,'cash','manual_cost','Chi nhap nguyen lieu Restaurant T4', NULL,'cost',NOW()),
(6,'2026-04-02', 0,  800000,'cash','manual_cost','Chi my pham Beauty T4',              NULL,'cost',NOW()),
(6,'2026-04-10', 0, 1200000,'cash','manual_cost','Chi van chuyen Retail T4',           NULL,'cost',NOW()),
(6,'2026-05-01', 0, 4200000,'cash','manual_cost','Chi nguyen lieu Restaurant T5',      NULL,'cost',NOW()),
(6,'2026-05-20', 0,  450000,'cash','manual_cost','Chi nhan cong Restaurant T5',        NULL,'cost',NOW()),
(6,'2026-06-01', 0, 4800000,'cash','manual_cost','Chi nguyen lieu Restaurant T6',      NULL,'cost',NOW()),
(6,'2026-06-12', 0, 1800000,'cash','manual_cost','Chi van chuyen Retail T6',           NULL,'cost',NOW()),
-- ── Bank: Thu ─────────────────────────────────────────────
(6,'2026-04-05',25000000, 0,'bank','manual_revenue','Thu NH Retail T4 dot 1',          NULL,'revenue',NOW()),
(6,'2026-04-12', 3200000, 0,'bank','manual_revenue','Thu NH Beauty 12/4',              NULL,'revenue',NOW()),
(6,'2026-04-20', 8500000, 0,'bank','manual_revenue','Thu NH DichVu 20/4',              NULL,'revenue',NOW()),
(6,'2026-04-30',15200000, 0,'bank','manual_revenue','Thu NH Grocery cuoi T4',          NULL,'revenue',NOW()),
(6,'2026-05-08',32000000, 0,'bank','manual_revenue','Thu NH Retail 8/5',               NULL,'revenue',NOW()),
(6,'2026-05-22', 9800000, 0,'bank','manual_revenue','Thu NH DichVu 22/5',              NULL,'revenue',NOW()),
(6,'2026-05-31',16900000, 0,'bank','manual_revenue','Thu NH Grocery cuoi T5',          NULL,'revenue',NOW()),
(6,'2026-05-31', 7500000, 0,'bank','manual_revenue','Thu NH Restaurant tiec cuoi T5',  NULL,'revenue',NOW()),
(6,'2026-06-10',28500000, 0,'bank','manual_revenue','Thu NH Retail 10/6',              NULL,'revenue',NOW()),
(6,'2026-06-25',11200000, 0,'bank','manual_revenue','Thu NH DichVu 25/6',              NULL,'revenue',NOW()),
(6,'2026-06-30',19500000, 0,'bank','manual_revenue','Thu NH Grocery cuoi Q2',          NULL,'revenue',NOW()),
(6,'2026-06-28',19300000, 0,'bank','manual_revenue','Thu NH Retail cuoi Q2',           NULL,'revenue',NOW()),
-- ── Bank: Chi ─────────────────────────────────────────────
(6,'2026-04-03', 0,35000000,'bank','manual_cost','Chi NH mua hang si Retail T4',       NULL,'cost',NOW()),
(6,'2026-04-15', 0,  500000,'bank','manual_cost','Chi NH dien nuoc Beauty T4',         NULL,'cost',NOW()),
(6,'2026-04-16', 0,12000000,'bank','manual_cost','Chi NH nhap them Grocery T4',        NULL,'cost',NOW()),
(6,'2026-05-05', 0,52000000,'bank','manual_cost','Chi NH mua hang si Retail T5',       NULL,'cost',NOW()),
(6,'2026-05-06', 0, 1500000,'bank','manual_cost','Chi NH phan mem DichVu T5',          NULL,'cost',NOW()),
(6,'2026-05-08', 0,  700000,'bank','manual_cost','Chi NH dien nuoc Grocery T5',        NULL,'cost',NOW()),
(6,'2026-06-08', 0,38000000,'bank','manual_cost','Chi NH mua hang si Retail T6',       NULL,'cost',NOW()),
(6,'2026-06-08', 0,  600000,'bank','manual_cost','Chi NH dien nuoc Restaurant T6',     NULL,'cost',NOW());

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('099_sample_data_location6_q2_2026', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
