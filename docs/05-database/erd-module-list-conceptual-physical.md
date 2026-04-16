# Danh sach module ERD (Conceptual + Physical)

Cap nhat: 2026-04-11
Pham vi: BizFlow-BE-Service (migrations SQL + EF DbContext runtime) va BizFlow-AI-Service (AI side-car tables)

Ghi chu su dung:
- Conceptual ERD: dung de ve nhanh luong nghiep vu va quan he cap domain.
- Physical ERD: dung de ve chi tiet bang/cot. Da uu tien schema dang duoc map o runtime.
- Ten bang trong tai lieu duoc uu tien viet dung theo migration (bao gom ca so it/so nhieu).
- Co mot so bang legacy da duoc loai bo, duoc danh dau ro o cuoi tai lieu.

## 1) Conceptual ERD theo module

### M01. Danh tinh va truy cap
- Thuc the: Roles, Accounts, Profiles, Credentials, RefreshTokens, otp_codes, DeviceTokens, SystemConfig.
- Quan he chinh:
  - Roles 1-N Accounts.
  - Accounts 1-1 Profiles.
  - Accounts 1-N Credentials.
  - Accounts 1-N RefreshTokens.
  - Profiles 1-N DeviceTokens.
  - Profiles 1-N SystemConfig.

### M02. To chuc va nhan su
- Thuc the: BusinessLocations, UserLocationAssignments, Hires.
- Quan he chinh:
  - BusinessLocations 1-N UserLocationAssignments.
  - Profiles 1-N UserLocationAssignments.
  - Profiles (Owner) 1-N Hires, Profiles (Employee) 1-N Hires.

### M03. Danh muc san pham va ton kho
- Thuc the: BusinessTypes, Products, SaleItems, ProductPricePolicies, StockMovements.
- Quan he chinh:
  - BusinessTypes 1-N Products.
  - BusinessLocations 1-N Products.
  - Products 1-N SaleItems.
  - SaleItems 1-N ProductPricePolicies.
  - Products 1-N StockMovements.

### M04. Nhap hang va chi phi
- Thuc the: ImportSchemas, ImportSchemaVersions, Imports, ProductsImports, Costs.
- Quan he chinh:
  - ImportSchemas 1-N ImportSchemaVersions.
  - ImportSchemaVersions 1-N Imports.
  - Imports 1-N ProductsImports.
  - Products 1-N ProductsImports.
  - Imports 1-N Costs.

### M05. Ban hang va cong no
- Thuc the: Debtors, Orders, OrderDetails, DebtorPaymentTransactions, Revenues.
- Quan he chinh:
  - Debtors 1-N Orders.
  - Orders 1-N OrderDetails.
  - SaleItems 1-N OrderDetails.
  - Debtors 1-N DebtorPaymentTransactions.
  - BusinessTypes 1-N Revenues.

### M06. Tai chinh nen tang va ky ke toan
- Thuc the: AccountingPeriods, AccountingPeriodAuditLogs, GeneralLedgerEntries, TaxPayments.
- Quan he chinh:
  - BusinessLocations 1-N AccountingPeriods.
  - AccountingPeriods 1-N AccountingPeriodAuditLogs.
  - BusinessLocations 1-N GeneralLedgerEntries.
  - GeneralLedgerEntries co self-reference (ReversedEntryId).
  - BusinessLocations 1-N TaxPayments; AccountingPeriods 1-N TaxPayments.

### M07. Rule engine va So ke toan
- Thuc the: TaxRulesets, TaxGroupRules, IndustryTaxRates, AccountingTemplates, AccountingTemplateVersions, MappableEntities, MappableFields, TemplateFieldMappings, TemplateRowDefinitions, FormulaDefinitions, FormulaResults, AccountingBooks, AccountingExports, AccountingBookTaxOverrides.
- Quan he chinh:
  - TaxRulesets 1-N TaxGroupRules.
  - TaxRulesets 1-N IndustryTaxRates.
  - AccountingTemplates 1-N AccountingTemplateVersions.
  - AccountingTemplateVersions 1-N TemplateFieldMappings.
  - AccountingTemplateVersions 1-N TemplateRowDefinitions.
  - FormulaDefinitions 1-N TemplateFieldMappings / TemplateRowDefinitions / FormulaResults.
  - AccountingBooks 1-N AccountingExports / FormulaResults / AccountingBookTaxOverrides.
  - AccountingBooks gan voi BusinessLocations + AccountingPeriods + AccountingTemplateVersions + TaxRulesets.

### M08. Subscription va thanh toan
- Thuc the: Features, SubscriptionPlans, SubscriptionPlanPrices, PlanFeatures, Subscriptions, Transactions, FeatureUsages, SubscriptionAuditLogs, StripeWebhookEvents.
- Quan he chinh:
  - SubscriptionPlans 1-N SubscriptionPlanPrices.
  - SubscriptionPlans N-N Features qua PlanFeatures.
  - Profiles 1-N Subscriptions.
  - Subscriptions 1-N Transactions.
  - Subscriptions 1-N FeatureUsages.
  - Subscriptions 1-N SubscriptionAuditLogs.

### M09. Notification Center
- Thuc the: NotificationTemplates, NotificationCampaigns, Notifications, UserNotifications, UserNotificationsArchive, NotificationOutboxMessages.
- Quan he chinh:
  - NotificationTemplates 1-N NotificationCampaigns.
  - Notifications 1-N UserNotifications.
  - Profiles 1-N UserNotifications.

### M10. AI Analytics side-car
- Thuc the: ai_revenue_forecasts, ai_anomaly_alerts, ai_reorder_suggestions, ai_product_insights.
- Quan he chinh:
  - Logical link theo location_id cho ca 4 bang.
  - product_id chi xuat hien o ai_reorder_suggestions va ai_product_insights (khong rang buoc FK cung trong BE migrations).

### M11. He thong va du lieu legacy
- Thuc the: __MigrationHistory, BusinessTypeTaxes_Archive.
- Muc dich: tracking migration va luu tru du lieu cu sau khi deprecate.

## 2) Physical ERD theo module (co field + value type)

### M01. Danh tinh va truy cap
- Roles: RoleId CHAR(36) (PK), Name VARCHAR(100), Description TEXT, CreateAt DATETIME, UpdateAt DATETIME.
- Accounts: AccountId CHAR(36) (PK), RoleId CHAR(36) (FK -> Roles), PasswordHash VARCHAR(255), IsActive BOOLEAN, LastLoginAt DATETIME NULL, MustChangePassword TINYINT(1), PasswordResetNonce CHAR(36) NULL, CreatedAt DATETIME, UpdatedAt DATETIME, DeletedAt DATETIME NULL.
- Profiles: ProfileId CHAR(36) (PK), AccountId CHAR(36) (FK -> Accounts), FullName VARCHAR(255), AvatarUrl VARCHAR(500) NULL, TaxCode VARCHAR(50) NULL, StripeCustomerId VARCHAR(255) NULL, UpdatedAt DATETIME.
- Credentials: CredentialId CHAR(36) (PK), AccountId CHAR(36) (FK -> Accounts), Type ENUM('phone','email','google'), Identifier VARCHAR(255), GoogleEmail VARCHAR(255) NULL, EmailVerified BOOLEAN, CreatedAt DATETIME.
- RefreshTokens: RefreshTokenId CHAR(36) (PK), AccountId CHAR(36) (FK -> Accounts), TokenHash VARCHAR(512), TokenSalt VARCHAR(128), DeviceInfo VARCHAR(500) NULL, ExpiresAt DATETIME, RevokedAt DATETIME NULL, CreatedAt DATETIME.
- otp_codes: id CHAR(36) (PK), email VARCHAR(255), code VARCHAR(10), created_at DATETIME, expired_at DATETIME, is_used TINYINT(1).
- DeviceTokens: DeviceTokenId CHAR(36) (PK), ProfileId CHAR(36) (FK -> Profiles), Token TEXT, Platform VARCHAR(50), DeviceName VARCHAR(255) NULL, RegisteredAt DATETIME, LastUsedAt DATETIME NULL, IsActive BOOLEAN.
- SystemConfig: SystemConfigId CHAR(36) (PK), Name VARCHAR(255), Value TEXT NULL, Description TEXT NULL, UpdatedBy CHAR(36) (FK -> Profiles), UpdatedAt DATETIME.

### M02. To chuc va nhan su
- BusinessLocations: BusinessLocationId INT (PK, AUTO_INCREMENT), LocationName VARCHAR(255), Address TEXT, District VARCHAR(100) NULL, City VARCHAR(100) NULL, Phone VARCHAR(20) NULL, Email VARCHAR(255) NULL, Status VARCHAR(20), TaxCode VARCHAR(50) NULL, IsActive BOOLEAN, CreatedAt DATETIME, UpdatedAt DATETIME, DeletedAt DATETIME NULL.
- UserLocationAssignments: UserLocationAssignmentId INT (PK, AUTO_INCREMENT), UserId CHAR(36) (FK -> Profiles), BusinessLocationId INT (FK -> BusinessLocations), IsOwner BOOLEAN, IsActive BOOLEAN, AssignedAt DATETIME, UnassignedAt DATETIME NULL.
- Hires: HireId INT (PK, AUTO_INCREMENT), OwnerId CHAR(36) (FK -> Profiles), EmployeeId CHAR(36) (FK -> Profiles), Status VARCHAR(20), InvitedAt DATETIME, StartAt DATETIME NULL, EndAt DATETIME NULL, IsActive BOOLEAN.

### M03. Danh muc san pham va ton kho
- BusinessTypes: BusinessTypeId CHAR(36) (PK), Code VARCHAR(50), Name VARCHAR(255), Description TEXT NULL, Status VARCHAR(20), CreatedBy CHAR(36) (FK -> Profiles), ModifiedBy CHAR(36) (FK -> Profiles), CreatedAt DATETIME, LastModifiedAt DATETIME.
- Products: ProductId BIGINT (PK, AUTO_INCREMENT), BusinessLocationId INT (FK -> BusinessLocations), BusinessTypeId CHAR(36) (FK -> BusinessTypes), ProductName VARCHAR(255), Sku VARCHAR(100) NULL, CostPrice DECIMAL(15,2), SellingPrice DECIMAL(15,2), Stock INT, Unit VARCHAR(50), ImageUrl VARCHAR(500) NULL, ImagePublicId VARCHAR(255) NULL, Manufacturer VARCHAR(255) NULL, TrackInventory BOOLEAN, Status ENUM('active','inactive','discontinued'), DeletedAt DATETIME NULL.
- SaleItems: SaleItemId BIGINT (PK, AUTO_INCREMENT), ProductId BIGINT (FK -> Products), Unit VARCHAR(50), Quantity INT, DeletedAt DATETIME NULL.
- ProductPricePolicies: ProductPricePolicyId BIGINT (PK, AUTO_INCREMENT), SaleItemId BIGINT (FK -> SaleItems), Price DECIMAL(15,2), IsDefault BOOLEAN, StartAt DATETIME NULL, EndAt DATETIME NULL.
- StockMovements: StockMovementId BIGINT (PK, AUTO_INCREMENT), ProductId BIGINT (FK -> Products), MovementType VARCHAR(50), Quantity INT, ReferenceType VARCHAR(50) NULL, ReferenceId BIGINT NULL, BalanceAfter INT, Memo VARCHAR(1000) NULL, CreatedAt DATETIME.

### M04. Nhap hang va chi phi
- ImportSchemas: ImportSchemaId INT (PK, AUTO_INCREMENT), TemplateCode VARCHAR(50), Name VARCHAR(100), IsActive BOOLEAN, EverActivated BOOLEAN, DeletedAt DATETIME NULL, CreatedAt DATETIME.
- ImportSchemaVersions: ImportSchemaVersionId INT (PK, AUTO_INCREMENT), ImportSchemaId INT (FK -> ImportSchemas), SchemaJson LONGTEXT, MappingJson LONGTEXT NULL, TemplateFileUrl VARCHAR(500) NULL, VersionLabel VARCHAR(50) NULL, EffectiveFrom DATETIME NULL, IsActive BOOLEAN, CreatedBy CHAR(36) NULL (FK -> Profiles), CreatedAt DATETIME.
- Imports: ImportId BIGINT (PK, AUTO_INCREMENT), ImportCode VARCHAR(50), ImportType VARCHAR(50), Status VARCHAR(20), BusinessLocationId INT (FK -> BusinessLocations), Supplier VARCHAR(200) NULL, HasInvoice BOOLEAN, TotalAmount DECIMAL(15,2), CreatedAt DATETIME, UpdatedAt DATETIME NULL, ConfirmedAt DATETIME NULL, CancelledAt DATETIME NULL, CancelReason TEXT NULL, ReceivedAt DATETIME NULL, Note TEXT NULL, ImageUrl VARCHAR(500) NULL, ImagePublicId VARCHAR(100) NULL, SchemaVersionId INT NULL (FK -> ImportSchemaVersions), SchemaDataJson LONGTEXT NULL.
- ProductsImports: ProductImportId BIGINT (PK, AUTO_INCREMENT), ImportId BIGINT (FK -> Imports), ProductId BIGINT (FK -> Products), Quantity INT, CostPrice DECIMAL(15,2), TotalPrice DECIMAL(15,2), BaseUnit VARCHAR(50), CreatedAt DATETIME.
- Costs: CostId BIGINT (PK, AUTO_INCREMENT), BusinessLocationId INT (FK -> BusinessLocations), ImportId BIGINT NULL (FK -> Imports), BusinessTypeId CHAR(36) NULL (FK -> BusinessTypes), CostType VARCHAR(30), Amount DECIMAL(15,2), CostDate DATE, Description VARCHAR(500), PaymentMethod VARCHAR(20) NULL, DocumentNumber VARCHAR(100) NULL, DocumentDate DATE NULL, DocumentUrl VARCHAR(500) NULL, DocumentPublicId VARCHAR(255) NULL, CreatedBy CHAR(36) NULL, CreatedAt DATETIME, UpdatedAt DATETIME NULL, DeletedAt DATETIME NULL.

### M05. Ban hang va cong no
- Debtors: DebtorId BIGINT (PK, AUTO_INCREMENT), BusinessLocationId INT (FK -> BusinessLocations), Name VARCHAR(255), Phone VARCHAR(20) NULL, Address TEXT NULL, CurrentBalance DECIMAL(15,2), CreditLimit DECIMAL(15,2) NULL, IsActive BOOLEAN, Notes TEXT NULL, CreatedByUserId CHAR(36) NULL, CreatedAt DATETIME, UpdatedAt DATETIME, DeletedAt DATETIME NULL.
- Orders: OrderId BIGINT (PK, AUTO_INCREMENT), DebtorId BIGINT NULL (FK -> Debtors), RefOrderId BIGINT NULL (FK -> Orders), OrderCode VARCHAR(50), Status VARCHAR(20), CustomerName VARCHAR(255) NULL, CustomerPhone VARCHAR(20) NULL, SubTotal DECIMAL(15,2), Discount DECIMAL(15,2), TotalAmount DECIMAL(15,2), DebtAmount DECIMAL(15,2), CashAmount DECIMAL(15,2), BankAmount DECIMAL(15,2), BillMetadata JSON NULL, Note TEXT NULL, CreatedBy CHAR(36) NULL, UpdatedBy CHAR(36) NULL, CompletedBy CHAR(36) NULL, CompletedAt DATETIME NULL, CancelledBy CHAR(36) NULL, CancelledAt DATETIME NULL, CancelReason TEXT NULL, CreatedAt DATETIME, UpdatedAt DATETIME.
- OrderDetails: OrderDetailId BIGINT (PK, AUTO_INCREMENT), OrderId BIGINT (FK -> Orders), SaleItemId BIGINT (FK -> SaleItems), Quantity INT, UnitPrice DECIMAL(15,2), Discount DECIMAL(15,2), Amount DECIMAL(15,2), CreatedAt DATETIME.
- DebtorPaymentTransactions: DebtorPaymentTransactionId BIGINT (PK, AUTO_INCREMENT), DebtorId BIGINT (FK -> Debtors), Amount DECIMAL(15,2), BalanceBefore DECIMAL(15,2), BalanceAfter DECIMAL(15,2), PaymentMethod VARCHAR(20), Notes TEXT NULL, CreatedByUserId CHAR(36) NULL, PaidAt DATETIME.
- Revenues: RevenueId BIGINT (PK, AUTO_INCREMENT), BusinessLocationId INT (FK -> BusinessLocations), BusinessTypeId CHAR(36) NULL (FK -> BusinessTypes), OrderId BIGINT NULL, RevenueType VARCHAR(20), MoneyChannel VARCHAR(10), Amount DECIMAL(15,2), RevenueDate DATE, Description VARCHAR(500), DocumentNumber VARCHAR(100) NULL, DocumentDate DATE NULL, CreatedBy CHAR(36) NULL, CreatedAt DATETIME, DeletedAt DATETIME NULL.

### M06. Tai chinh nen tang va ky ke toan
- AccountingPeriods: PeriodId BIGINT (PK, AUTO_INCREMENT), BusinessLocationId INT (FK -> BusinessLocations), PeriodType VARCHAR(10), Year SMALLINT, Quarter TINYINT NULL, StartDate DATE, EndDate DATE, OpeningCashBalance DECIMAL(15,2) NULL, OpeningBankBalance DECIMAL(15,2) NULL, Status VARCHAR(20), FinalizedAt DATETIME NULL, FinalizedByUserId CHAR(36) NULL, CreatedAt DATETIME, UpdatedAt DATETIME.
- AccountingPeriodAuditLogs: LogId BIGINT (PK, AUTO_INCREMENT), PeriodId BIGINT (FK -> AccountingPeriods), Action VARCHAR(50), OldValue JSON NULL, NewValue JSON NULL, Reason TEXT NULL, CreatedByUserId CHAR(36) NULL, CreatedAt DATETIME.
- GeneralLedgerEntries: EntryId BIGINT (PK, AUTO_INCREMENT), BusinessLocationId INT (FK -> BusinessLocations), ReversedEntryId BIGINT NULL (FK -> GeneralLedgerEntries), EntryDate DATE, TransactionType VARCHAR(30), ReferenceType VARCHAR(30), ReferenceId BIGINT NULL, MoneyChannel VARCHAR(10) NULL, DebitAmount DECIMAL(15,2), CreditAmount DECIMAL(15,2), Description VARCHAR(500), IsReversal BOOLEAN, CreatedAt DATETIME.
- TaxPayments: TaxPaymentId BIGINT (PK, AUTO_INCREMENT), BusinessLocationId INT (FK -> BusinessLocations), PeriodId BIGINT NULL (FK -> AccountingPeriods), TaxType VARCHAR(10), Amount DECIMAL(15,2), PaidAt DATE, PaymentMethod VARCHAR(20) NULL, ReferenceNumber VARCHAR(100) NULL, Notes TEXT NULL, CreatedByUserId CHAR(36) NULL, CreatedAt DATETIME, DeletedAt DATETIME NULL.

### M07. Rule engine va So ke toan
- TaxRulesets: RulesetId INT (PK, AUTO_INCREMENT), Code VARCHAR(50), Name VARCHAR(200), Description TEXT NULL, Version VARCHAR(20), EffectiveFrom DATE, EffectiveTo DATE NULL, IsActive BOOLEAN, CreatedByUserId CHAR(36) NULL, CreatedAt DATETIME.
- TaxGroupRules: RuleId INT (PK, AUTO_INCREMENT), RulesetId INT (FK -> TaxRulesets), GroupNumber TINYINT, GroupName VARCHAR(100), GroupDescription TEXT NULL, ConditionsJson JSON, OutcomesJson JSON, SortOrder INT.
- IndustryTaxRates: RateId INT (PK, AUTO_INCREMENT), RulesetId INT (FK -> TaxRulesets), BusinessTypeId CHAR(36) (FK -> BusinessTypes), TaxType VARCHAR(20), TaxRate DECIMAL(5,4), Description VARCHAR(200) NULL.
- AccountingTemplates: TemplateId INT (PK, AUTO_INCREMENT), TemplateCode VARCHAR(20), Name VARCHAR(200), Description TEXT NULL, ApplicableGroups JSON, ApplicableMethods JSON NULL, DataSourceType VARCHAR(30), IsActive BOOLEAN, CreatedAt DATETIME.
- AccountingTemplateVersions: TemplateVersionId INT (PK, AUTO_INCREMENT), TemplateId INT (FK -> AccountingTemplates), VersionLabel VARCHAR(50), IsActive BOOLEAN, EffectiveFrom DATE NULL, TemplateFileUrl VARCHAR(500) NULL, ChangeNotes TEXT NULL, CreatedByUserId CHAR(36) NULL, CreatedAt DATETIME.
- MappableEntities: EntityId INT (PK, AUTO_INCREMENT), EntityCode VARCHAR(50), DisplayName VARCHAR(200), Description TEXT NULL, Category VARCHAR(50), IsActive BOOLEAN, CreatedByUserId CHAR(36) NULL, CreatedAt DATETIME, UpdatedAt DATETIME.
- MappableFields: FieldId INT (PK, AUTO_INCREMENT), EntityId INT (FK -> MappableEntities), FieldCode VARCHAR(100), DisplayName VARCHAR(200), Description TEXT NULL, DataType VARCHAR(20), AllowedAggregations JSON, IsActive BOOLEAN, CreatedAt DATETIME, UpdatedAt DATETIME.
- TemplateFieldMappings: MappingId INT (PK, AUTO_INCREMENT), TemplateVersionId INT (FK -> AccountingTemplateVersions), FieldCode VARCHAR(50), FieldLabel VARCHAR(200), FieldType VARCHAR(20), SourceType VARCHAR(30), SourceEntityId INT NULL (FK -> MappableEntities), SourceFieldId INT NULL (FK -> MappableFields), FilterJson JSON NULL, AggregationType VARCHAR(20) NULL, FormulaId BIGINT NULL (FK -> FormulaDefinitions), FormulaExpression VARCHAR(500) NULL, DependsOn JSON NULL, CalculationOrder INT NULL, ExportColumn VARCHAR(10) NULL, SortOrder INT, IsRequired BOOLEAN.
- TemplateRowDefinitions: RowDefId INT (PK, AUTO_INCREMENT), TemplateVersionId INT (FK -> AccountingTemplateVersions), RowType VARCHAR(30), RowLabel VARCHAR(200) NULL, Position VARCHAR(20), SortOrder INT, GroupByField VARCHAR(50) NULL, SectionType VARCHAR(30) NULL, VisibleFieldCodes JSON NULL, FormulaId BIGINT NULL (FK -> FormulaDefinitions), TaxType VARCHAR(10) NULL, CreatedAt DATETIME.
- FormulaDefinitions: FormulaId BIGINT (PK, AUTO_INCREMENT), Code VARCHAR(50), Name VARCHAR(255), Description TEXT NULL, FormulaType VARCHAR(20), ExpressionJson JSON, ResultDataType VARCHAR(10), RoundingMode VARCHAR(20), RoundingPrecision TINYINT NULL, IsActive BOOLEAN, CreatedByUserId CHAR(36) NULL, CreatedAt DATETIME, UpdatedAt DATETIME.
- FormulaResults: ResultId BIGINT (PK, AUTO_INCREMENT), BookId BIGINT (FK -> AccountingBooks), FormulaId BIGINT (FK -> FormulaDefinitions), ProductId CHAR(36) NULL, BusinessTypeId CHAR(36) NULL, SectionCode VARCHAR(50) NULL, ResultValue DECIMAL(18,4), ComputedAt DATETIME, IsStale BOOLEAN.
- AccountingBooks: BookId BIGINT (PK, AUTO_INCREMENT), BusinessLocationId INT (FK -> BusinessLocations), PeriodId BIGINT (FK -> AccountingPeriods), TemplateVersionId INT (FK -> AccountingTemplateVersions), RulesetId INT (FK -> TaxRulesets), GroupNumber TINYINT, TaxMethod VARCHAR(20), Status VARCHAR(20), CreatedByUserId CHAR(36) NULL, CreatedAt DATETIME, ArchivedAt DATETIME NULL.
- AccountingExports: ExportId BIGINT (PK, AUTO_INCREMENT), BookId BIGINT (FK -> AccountingBooks), GroupNumber TINYINT, TaxMethod VARCHAR(20), RulesetVersion VARCHAR(20), SummaryJson LONGTEXT, DataRowCount INT, ExportFormat VARCHAR(10), FileUrl VARCHAR(500) NULL, FilePublicId VARCHAR(255) NULL, ExportedByUserId CHAR(36) NULL, ExportedAt DATETIME, Notes TEXT NULL.
- AccountingBookTaxOverrides: OverrideId BIGINT (PK, AUTO_INCREMENT), BookId BIGINT (FK -> AccountingBooks), BusinessTypeId CHAR(36) (FK -> BusinessTypes), VatRate DECIMAL(8,6), PitRate DECIMAL(8,6), Note TEXT, UpdatedByUserId CHAR(36) NULL, UpdatedAt DATETIME.

### M08. Subscription va thanh toan
- Features: FeatureId INT (PK, AUTO_INCREMENT), FeatureCode VARCHAR(50), Name VARCHAR(200), Description TEXT NULL, CreatedAt DATETIME, UpdatedAt DATETIME.
- SubscriptionPlans: SubscriptionPlanId INT (PK, AUTO_INCREMENT), Name VARCHAR(100), Description TEXT NULL, DurationDays INT, StripePriceId VARCHAR(255) NULL, StripeProductId VARCHAR(255) NULL, IsActive BOOLEAN, DeletedAt DATETIME NULL, CreatedAt DATETIME, UpdatedAt DATETIME.
- SubscriptionPlanPrices: PriceId INT (PK, AUTO_INCREMENT), SubscriptionPlanId INT (FK -> SubscriptionPlans), BasePrice DECIMAL(15,2), DiscountedPrice DECIMAL(15,2) NULL, DiscountStart DATETIME NULL, DiscountEnd DATETIME NULL, IsDiscountActive BOOLEAN, IsActive BOOLEAN, Currency VARCHAR(3), CreatedAt DATETIME, UpdatedAt DATETIME.
- PlanFeatures: SubscriptionPlanId INT (PK, FK -> SubscriptionPlans), FeatureId INT (PK, FK -> Features), UsageLimit INT, CreatedAt DATETIME.
- Subscriptions: SubscriptionId CHAR(36) (PK), OwnerProfileId CHAR(36) (FK -> Profiles), SubscriptionPlanId INT (FK -> SubscriptionPlans), Status VARCHAR(20), IsAutoRenew BOOLEAN, StartDate DATETIME, EndDate DATETIME, LastReminderSentAt DATETIME NULL, CreatedAt DATETIME, UpdatedAt DATETIME.
- Transactions: TransactionId CHAR(36) (PK), ProfileId CHAR(36) (FK -> Profiles), SubscriptionPlanId INT (FK -> SubscriptionPlans), SubscriptionId CHAR(36) (FK -> Subscriptions), StripeCheckoutSessionId VARCHAR(255) NULL, StripePaymentIntentId VARCHAR(255) NULL, IdempotencyKey VARCHAR(100) NULL, TransactionType VARCHAR(20), PlanPrice DECIMAL(15,2), ProrationCredit DECIMAL(15,2), FinalAmount DECIMAL(15,2), Currency VARCHAR(3), Status VARCHAR(20), PaidAt DATETIME NULL, CreatedAt DATETIME, UpdatedAt DATETIME.
- FeatureUsages: FeatureUsageId INT (PK, AUTO_INCREMENT), SubscriptionId CHAR(36) (FK -> Subscriptions), FeatureId INT (FK -> Features), UsedCount INT, AllocatedLimit INT NULL, PeriodStart DATETIME, PeriodEnd DATETIME, UpdatedAt DATETIME.
- SubscriptionAuditLogs: AuditLogId INT (PK, AUTO_INCREMENT), SubscriptionId CHAR(36) (FK -> Subscriptions), Action VARCHAR(50), Details JSON NULL, PerformedBy CHAR(36) NULL, CreatedAt DATETIME.
- StripeWebhookEvents: StripeWebhookEventId INT (PK, AUTO_INCREMENT), EventId VARCHAR(100), EventType VARCHAR(80), StripeCreatedAt DATETIME, ReceivedAt DATETIME, ProcessingStatus VARCHAR(20), AttemptCount INT, LastError TEXT NULL, ProcessedAt DATETIME NULL, UpdatedAt DATETIME.

### M09. Notification Center
- NotificationTemplates: NotificationTemplateId CHAR(36) (PK), EventCode VARCHAR(100), NotificationType VARCHAR(50), TitleTemplate VARCHAR(250), ContentTemplate TEXT, DefaultActionType VARCHAR(50) NULL, DefaultTargetScreen VARCHAR(100) NULL, DefaultActionPayloadJson JSON NULL, IsActive BIT(1), CreatedAt DATETIME, UpdatedAt DATETIME NULL.
- NotificationCampaigns: NotificationDispatchId BIGINT (PK, AUTO_INCREMENT), NotificationTemplateId CHAR(36) (FK -> NotificationTemplates), NotificationType VARCHAR(50), Priority VARCHAR(20), Title VARCHAR(250), Content TEXT, DataJson JSON NULL, ActionType VARCHAR(50) NULL, TargetScreen VARCHAR(100) NULL, ActionPayloadJson JSON NULL, RecipientScope VARCHAR(30), RecipientUserIdsJson JSON NULL, ScheduledAt DATETIME NULL, SentAt DATETIME NULL, Status VARCHAR(20), ErrorMessage TEXT NULL, CreatedByUserId CHAR(36) (FK -> Profiles), CreatedAt DATETIME, UpdatedAt DATETIME NULL.
- Notifications: NotificationId CHAR(36) (PK), NotificationType VARCHAR(50), Priority VARCHAR(20), Title VARCHAR(250), Content TEXT, ActionType VARCHAR(50) NULL, TargetScreen VARCHAR(100) NULL, ActionPayloadJson JSON NULL, DataJson JSON NULL, CreatedAt DATETIME.
- UserNotifications: UserNotificationId BIGINT (PK, AUTO_INCREMENT), UserId CHAR(36) (FK -> Profiles), NotificationId CHAR(36) (FK -> Notifications), NotificationType VARCHAR(50), Priority VARCHAR(20), Title VARCHAR(250), Content TEXT, ActionType VARCHAR(50) NULL, TargetScreen VARCHAR(100) NULL, ActionPayloadJson JSON NULL, DeliveryStatus VARCHAR(20), CreatedAt DATETIME, SentAt DATETIME NULL, ReadAt DATETIME NULL, ErrorMessage TEXT NULL.
- UserNotificationsArchive: UserNotificationId BIGINT (PK), UserId CHAR(36), NotificationId CHAR(36), NotificationType VARCHAR(50), Priority VARCHAR(20), Title VARCHAR(250), Content TEXT, ActionType VARCHAR(50) NULL, TargetScreen VARCHAR(100) NULL, ActionPayloadJson JSON NULL, DeliveryStatus VARCHAR(20), CreatedAt DATETIME, SentAt DATETIME NULL, ReadAt DATETIME NULL, ErrorMessage TEXT NULL.
- NotificationOutboxMessages: NotificationOutboxMessageId BIGINT (PK, AUTO_INCREMENT), EventType VARCHAR(100), PayloadJson JSON, Status VARCHAR(20), RetryCount INT, CreatedAt DATETIME, LastAttemptAt DATETIME NULL, ProcessedAt DATETIME NULL, LastError TEXT NULL.

### M10. AI Analytics side-car (tu BizFlow-AI-Service)
- ai_revenue_forecasts: id VARCHAR(36) (PK), location_id VARCHAR(36), forecast_date VARCHAR(10), predicted_revenue FLOAT, lower_bound FLOAT, upper_bound FLOAT, trend_note TEXT NULL, generated_at DATETIME.
- ai_anomaly_alerts: id VARCHAR(36) (PK), location_id VARCHAR(36), alert_type VARCHAR(100), severity VARCHAR(20), tier VARCHAR(20), reference_date DATE, description TEXT, reference_id VARCHAR(36) NULL, is_acknowledged BOOLEAN, generated_at DATETIME.
- ai_reorder_suggestions: id VARCHAR(36) (PK), location_id VARCHAR(36), product_id VARCHAR(36), current_stock FLOAT, days_until_stockout INT, suggested_quantity FLOAT, avg_daily_sales FLOAT, urgency VARCHAR(10), generated_at DATETIME.
- ai_product_insights: id VARCHAR(36) (PK), location_id VARCHAR(36), product_id VARCHAR(36), insight_type VARCHAR(50), `rank` INT, metric_value FLOAT, period_days INT, generated_at DATETIME.

### M11. He thong va legacy
- __MigrationHistory: MigrationId VARCHAR(150) (PK), ProductVersion VARCHAR(32), AppliedAt DATETIME.
- BusinessTypeTaxes_Archive: BusinessTypeTaxId CHAR(36) (PK), BusinessTypeId CHAR(36), TaxType VARCHAR(50), TaxRate DECIMAL(5,2), CalculationBase VARCHAR(50), EffectiveFrom DATE, EffectiveTo DATE NULL, CreatedBy CHAR(36) NULL, CreatedAt DATETIME, ArchivedAt DATETIME.
- AccountingBookBusinessTypes: da bi DROP o migration 072 (khong dua vao ERD hien hanh).

## 3) Thu tu ve ERD de de nhin
1. Ve Conceptual truoc theo M01 -> M11 (chi entity + cardinality).
2. Ve Physical theo tung module, bat dau M01, M02, M03 (core), sau do M04, M05, M06.
3. Ve khoi M07 (rule/accounting) thanh sub-diagram rieng roi moi merge vao full diagram.
4. Ve M08, M09, M10 thanh cac canh ben de giam roi.
5. Cuoi cung tao 1 full ERD tong quat, giu only PK/FK + 4-8 field nghiep vu moi bang de tranh qua tai khi thuyet trinh.
