using System;
using System.Collections.Generic;
using BizFlow.Infrastructure.TempEntities;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.TempData;

public partial class BizFlowDbContext : DbContext
{
    public BizFlowDbContext(DbContextOptions<BizFlowDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Accounts> Accounts { get; set; }

    public virtual DbSet<BusinessLocations> BusinessLocations { get; set; }

    public virtual DbSet<BusinessTypeTaxes> BusinessTypeTaxes { get; set; }

    public virtual DbSet<BusinessTypes> BusinessTypes { get; set; }

    public virtual DbSet<Costs> Costs { get; set; }

    public virtual DbSet<Credentials> Credentials { get; set; }

    public virtual DbSet<DebtorPaymentTransactions> DebtorPaymentTransactions { get; set; }

    public virtual DbSet<Debtors> Debtors { get; set; }

    public virtual DbSet<DeviceTokens> DeviceTokens { get; set; }

    public virtual DbSet<FeatureUsages> FeatureUsages { get; set; }

    public virtual DbSet<Features> Features { get; set; }

    public virtual DbSet<GeneralLedgerEntries> GeneralLedgerEntries { get; set; }

    public virtual DbSet<Hires> Hires { get; set; }

    public virtual DbSet<ImportSchemaVersions> ImportSchemaVersions { get; set; }

    public virtual DbSet<ImportSchemas> ImportSchemas { get; set; }

    public virtual DbSet<Imports> Imports { get; set; }

    public virtual DbSet<OrderDetails> OrderDetails { get; set; }

    public virtual DbSet<Orders> Orders { get; set; }

    public virtual DbSet<PlanFeatures> PlanFeatures { get; set; }

    public virtual DbSet<ProductPricePolicies> ProductPricePolicies { get; set; }

    public virtual DbSet<Products> Products { get; set; }

    public virtual DbSet<ProductsImports> ProductsImports { get; set; }

    public virtual DbSet<Profiles> Profiles { get; set; }

    public virtual DbSet<RefreshTokens> RefreshTokens { get; set; }

    public virtual DbSet<Revenues> Revenues { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<SaleItems> SaleItems { get; set; }

    public virtual DbSet<StockMovements> StockMovements { get; set; }

    public virtual DbSet<SubscriptionAuditLogs> SubscriptionAuditLogs { get; set; }

    public virtual DbSet<SubscriptionPlans> SubscriptionPlans { get; set; }

    public virtual DbSet<Subscriptions> Subscriptions { get; set; }

    public virtual DbSet<SystemConfig> SystemConfig { get; set; }

    public virtual DbSet<Transactions> Transactions { get; set; }

    public virtual DbSet<UserLocationAssignments> UserLocationAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Accounts>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.DeletedAt, "idx_account_deleted_at");

            entity.HasIndex(e => e.IsActive, "idx_account_is_active");

            entity.HasIndex(e => e.RoleId, "idx_account_role");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.DeletedAt)
                .HasComment("Soft delete timestamp")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("Account status");
            entity.Property(e => e.LastLoginAt)
                .HasComment("Last login timestamp")
                .HasColumnType("datetime");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasComment("BCrypt hash, NULL for Google-only accounts");
            entity.Property(e => e.RoleId).HasComment("FK to Roles");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_account_role");
        });

        modelBuilder.Entity<BusinessLocations>(entity =>
        {
            entity.HasKey(e => e.BusinessLocationId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.City, "idx_business_location_city");

            entity.HasIndex(e => e.DeletedAt, "idx_business_location_deleted_at");

            entity.HasIndex(e => e.IsActive, "idx_business_location_is_active");

            entity.HasIndex(e => e.LocationName, "idx_business_location_name");

            entity.HasIndex(e => e.Status, "idx_business_location_status");

            entity.Property(e => e.Address).HasColumnType("text");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.DeletedAt)
                .HasComment("Soft delete timestamp")
                .HasColumnType("datetime");
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasComment("Location email");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.LocationName).HasComment("Location/store name");
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'active'")
                .HasComment("active, inactive");
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasComment("Business tax code");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<BusinessTypeTaxes>(entity =>
        {
            entity.HasKey(e => e.BusinessTypeTaxId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.CreatedBy, "fk_business_type_tax_created_by");

            entity.HasIndex(e => e.BusinessTypeId, "idx_business_type_tax_business_type");

            entity.HasIndex(e => new { e.EffectiveFrom, e.EffectiveTo }, "idx_business_type_tax_effective");

            entity.HasIndex(e => e.TaxType, "idx_business_type_tax_type");

            entity.Property(e => e.CalculationBase)
                .HasMaxLength(50)
                .HasDefaultValueSql("'price'")
                .HasComment("Calculation base: price, revenue");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.TaxRate)
                .HasPrecision(5, 2)
                .HasComment("Tax rate percentage");
            entity.Property(e => e.TaxType)
                .HasMaxLength(50)
                .HasComment("VAT, PIT");

            entity.HasOne(d => d.BusinessType).WithMany(p => p.BusinessTypeTaxes)
                .HasForeignKey(d => d.BusinessTypeId)
                .HasConstraintName("fk_business_type_tax_business_type");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BusinessTypeTaxes)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_business_type_tax_created_by");
        });

        modelBuilder.Entity<BusinessTypes>(entity =>
        {
            entity.HasKey(e => e.BusinessTypeId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.CreatedBy, "fk_business_type_created_by");

            entity.HasIndex(e => e.ModifiedBy, "fk_business_type_modified_by");

            entity.HasIndex(e => e.Code, "idx_business_type_code").IsUnique();

            entity.HasIndex(e => e.Name, "idx_business_type_name");

            entity.HasIndex(e => e.Status, "idx_business_type_status");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.LastModifiedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'active'")
                .HasComment("active, inactive");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BusinessTypesCreatedByNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_business_type_created_by");

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.BusinessTypesModifiedByNavigation)
                .HasForeignKey(d => d.ModifiedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_business_type_modified_by");
        });

        modelBuilder.Entity<Costs>(entity =>
        {
            entity.HasKey(e => e.CostId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Chi phí cửa hàng - source-of-truth cho mọi khoản chi"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ImportId, "idx_cost_import");

            entity.HasIndex(e => e.BusinessLocationId, "idx_cost_location");

            entity.HasIndex(e => new { e.BusinessLocationId, e.CostDate }, "idx_cost_location_date");

            entity.HasIndex(e => new { e.BusinessLocationId, e.CostType }, "idx_cost_type");

            entity.Property(e => e.Amount)
                .HasPrecision(15, 2)
                .HasComment("Giá trị chi phí");
            entity.Property(e => e.BusinessLocationId).HasComment("FK to BusinessLocations");
            entity.Property(e => e.CostDate).HasComment("Ngày phát sinh chi phí");
            entity.Property(e => e.CostType)
                .HasMaxLength(30)
                .HasComment("import | salary | rent | utilities | transport | marketing | maintenance | other | manual");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasComment("UserId người tạo bản ghi");
            entity.Property(e => e.DeletedAt)
                .HasComment("Soft delete")
                .HasColumnType("datetime");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasComment("Mô tả nội dung chi phí");
            entity.Property(e => e.DocumentPublicId)
                .HasMaxLength(255)
                .HasComment("Public ID Cloudinary của chứng từ");
            entity.Property(e => e.DocumentUrl)
                .HasMaxLength(500)
                .HasComment("URL chứng từ/hóa đơn (Cloudinary)");
            entity.Property(e => e.ImportId).HasComment("FK to Imports (chỉ có khi CostType = import)");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .HasComment("cash | bank");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("datetime");

            entity.HasOne(d => d.BusinessLocation).WithMany(p => p.Costs)
                .HasForeignKey(d => d.BusinessLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cost_location");

            entity.HasOne(d => d.Import).WithMany(p => p.Costs)
                .HasForeignKey(d => d.ImportId)
                .HasConstraintName("fk_cost_import");
        });

        modelBuilder.Entity<Credentials>(entity =>
        {
            entity.HasKey(e => e.CredentialId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.AccountId, "idx_credential_account");

            entity.HasIndex(e => e.Identifier, "idx_credential_identifier");

            entity.HasIndex(e => new { e.AccountId, e.Type }, "uq_credential_account_type").IsUnique();

            entity.HasIndex(e => new { e.Type, e.Identifier }, "uq_credential_type_identifier").IsUnique();

            entity.Property(e => e.AccountId).HasComment("FK to Accounts");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.EmailVerified).HasComment("Only used for type=email");
            entity.Property(e => e.GoogleEmail)
                .HasMaxLength(255)
                .HasComment("Only used for type=google (informational)");
            entity.Property(e => e.Identifier).HasComment("Phone: +84xxx, Email: user@mail, Google: sub-id");
            entity.Property(e => e.Type)
                .HasComment("Credential type")
                .HasColumnType("enum('phone','email','google')");

            entity.HasOne(d => d.Account).WithMany(p => p.Credentials)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("fk_credential_account");
        });

        modelBuilder.Entity<DebtorPaymentTransactions>(entity =>
        {
            entity.HasKey(e => e.DebtorPaymentTransactionId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Lịch sử giao dịch thanh toán nợ của khách"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.DebtorId, "idx_debtor_payment_debtor");

            entity.HasIndex(e => e.PaidAt, "idx_debtor_payment_paid_at");

            entity.Property(e => e.Amount)
                .HasPrecision(15, 2)
                .HasComment("Số tiền thanh toán trong giao dịch này");
            entity.Property(e => e.BalanceAfter)
                .HasPrecision(15, 2)
                .HasComment("Số dư nợ sau giao dịch");
            entity.Property(e => e.BalanceBefore)
                .HasPrecision(15, 2)
                .HasComment("Số dư nợ trước giao dịch");
            entity.Property(e => e.CreatedByUserId).HasComment("UserId người ghi nhận thanh toán");
            entity.Property(e => e.DebtorId).HasComment("FK to Debtors");
            entity.Property(e => e.Notes)
                .HasComment("Ghi chú của giao dịch")
                .HasColumnType("text");
            entity.Property(e => e.PaidAt)
                .HasComment("Thời điểm thanh toán thực tế")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .HasComment("cash | bank");

            entity.HasOne(d => d.Debtor).WithMany(p => p.DebtorPaymentTransactions)
                .HasForeignKey(d => d.DebtorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_debtor_payment_debtor");
        });

        modelBuilder.Entity<Debtors>(entity =>
        {
            entity.HasKey(e => e.DebtorId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Danh sách khách nợ theo từng cửa hàng"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => new { e.BusinessLocationId, e.IsActive }, "idx_debtor_active");

            entity.HasIndex(e => e.BusinessLocationId, "idx_debtor_location");

            entity.HasIndex(e => new { e.BusinessLocationId, e.Phone }, "idx_debtor_phone_location").IsUnique();

            entity.Property(e => e.Address)
                .HasComment("Địa chỉ")
                .HasColumnType("text");
            entity.Property(e => e.BusinessLocationId).HasComment("FK to BusinessLocations");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.CreatedByUserId).HasComment("UserId người tạo (FK to Profiles)");
            entity.Property(e => e.CreditLimit)
                .HasPrecision(15, 2)
                .HasComment("Hạn mức tín dụng cho phép (NULL = không giới hạn)");
            entity.Property(e => e.CurrentBalance)
                .HasPrecision(15, 2)
                .HasComment("Số dư nợ hiện tại (>0 = đang nợ)");
            entity.Property(e => e.DeletedAt)
                .HasComment("Soft delete timestamp")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("Trạng thái hoạt động");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasComment("Tên khách nợ");
            entity.Property(e => e.Notes)
                .HasComment("Ghi chú nội bộ")
                .HasColumnType("text");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasComment("Số điện thoại (unique per location)");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.BusinessLocation).WithMany(p => p.Debtors)
                .HasForeignKey(d => d.BusinessLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_debtor_location");
        });

        modelBuilder.Entity<DeviceTokens>(entity =>
        {
            entity.HasKey(e => e.DeviceTokenId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => new { e.ProfileId, e.Token }, "idx_device_token_unique")
                .IsUnique()
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 255 });

            entity.HasIndex(e => e.Platform, "idx_platform");

            entity.HasIndex(e => new { e.ProfileId, e.IsActive }, "idx_profile_active");

            entity.Property(e => e.DeviceTokenId).HasComment("UUID");
            entity.Property(e => e.DeviceName)
                .HasMaxLength(255)
                .HasComment("Device identifier");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.LastUsedAt).HasColumnType("datetime");
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .HasComment("iOS, Android, Web");
            entity.Property(e => e.ProfileId).HasComment("FK to Profiles");
            entity.Property(e => e.RegisteredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Token)
                .HasComment("FCM Token")
                .HasColumnType("text");

            entity.HasOne(d => d.Profile).WithMany(p => p.DeviceTokens)
                .HasForeignKey(d => d.ProfileId)
                .HasConstraintName("fk_device_token_profile");
        });

        modelBuilder.Entity<FeatureUsages>(entity =>
        {
            entity.HasKey(e => e.FeatureUsageId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Tracking usage of limited features per subscription period"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.FeatureId, "fk_feature_usage_feature");

            entity.HasIndex(e => new { e.SubscriptionId, e.FeatureId }, "idx_feature_usage_sub_feat").IsUnique();

            entity.Property(e => e.PeriodEnd).HasColumnType("datetime");
            entity.Property(e => e.PeriodStart).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Feature).WithMany(p => p.FeatureUsages)
                .HasForeignKey(d => d.FeatureId)
                .HasConstraintName("featureusages_ibfk_2");

            entity.HasOne(d => d.Subscription).WithMany(p => p.FeatureUsages)
                .HasForeignKey(d => d.SubscriptionId)
                .HasConstraintName("featureusages_ibfk_1");
        });

        modelBuilder.Entity<Features>(entity =>
        {
            entity.HasKey(e => e.FeatureId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("System features available to subscription plans"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.FeatureCode, "FeatureCode").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.FeatureCode).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<GeneralLedgerEntries>(entity =>
        {
            entity.HasKey(e => e.EntryId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Sổ cái kế toán bất biến - chỉ thêm, không sửa/xoá"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BusinessLocationId, "idx_gl_location");

            entity.HasIndex(e => new { e.BusinessLocationId, e.EntryDate }, "idx_gl_location_date");

            entity.HasIndex(e => new { e.ReferenceType, e.ReferenceId }, "idx_gl_reference");

            entity.HasIndex(e => e.ReversedEntryId, "idx_gl_reversal");

            entity.HasIndex(e => new { e.BusinessLocationId, e.TransactionType }, "idx_gl_transaction_type");

            entity.Property(e => e.BusinessLocationId).HasComment("FK to BusinessLocations");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("IMMUTABLE - không được thay đổi sau khi tạo")
                .HasColumnType("datetime");
            entity.Property(e => e.CreditAmount)
                .HasPrecision(15, 2)
                .HasComment("Số tiền Có (credit)");
            entity.Property(e => e.DebitAmount)
                .HasPrecision(15, 2)
                .HasComment("Số tiền Nợ (debit)");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasComment("Mô tả nội dung bút toán");
            entity.Property(e => e.EntryDate).HasComment("Ngày phát sinh nghiệp vụ");
            entity.Property(e => e.IsReversal).HasComment("TRUE nếu đây là bản ghi đảo (reversal entry)");
            entity.Property(e => e.MoneyChannel)
                .HasMaxLength(10)
                .HasComment("cash | bank | debt");
            entity.Property(e => e.ReferenceId).HasComment("ID của thực thể nguồn (polymorphic, không có FK cứng)");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(30)
                .HasComment("order | cost | import | debtor_payment | revenue");
            entity.Property(e => e.ReversedEntryId).HasComment("EntryId bị đảo ngược (tự tham chiếu)");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(30)
                .HasComment("sale | import_cost | manual_cost | debt_payment | manual_revenue | manual_expense");

            entity.HasOne(d => d.BusinessLocation).WithMany(p => p.GeneralLedgerEntries)
                .HasForeignKey(d => d.BusinessLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gl_location");

            entity.HasOne(d => d.ReversedEntry).WithMany(p => p.InverseReversedEntry)
                .HasForeignKey(d => d.ReversedEntryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_gl_reversed_entry");
        });

        modelBuilder.Entity<Hires>(entity =>
        {
            entity.HasKey(e => e.HireId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.EmployeeId, "idx_hire_employee");

            entity.HasIndex(e => e.InvitedAt, "idx_hire_invited_at");

            entity.HasIndex(e => e.IsActive, "idx_hire_is_active");

            entity.HasIndex(e => e.OwnerId, "idx_hire_owner");

            entity.HasIndex(e => new { e.OwnerId, e.EmployeeId }, "idx_hire_owner_employee");

            entity.HasIndex(e => e.Status, "idx_hire_status");

            entity.Property(e => e.EmployeeId).HasComment("Employee being hired");
            entity.Property(e => e.EndAt)
                .HasComment("End date of employment (NULL if still active)")
                .HasColumnType("datetime");
            entity.Property(e => e.InvitedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Invitation timestamp")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("Hiring status");
            entity.Property(e => e.OwnerId).HasComment("Owner who hired the employee");
            entity.Property(e => e.StartAt)
                .HasComment("Start date of employment (NULL when pending/rejected)")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'accepted'")
                .HasComment("pending, accepted, rejected");

            entity.HasOne(d => d.Employee).WithMany(p => p.HiresEmployee)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("fk_hire_employee");

            entity.HasOne(d => d.Owner).WithMany(p => p.HiresOwner)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_hire_owner");
        });

        modelBuilder.Entity<ImportSchemaVersions>(entity =>
        {
            entity.HasKey(e => e.ImportSchemaVersionId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.CreatedBy, "fk_import_schema_version_created_by");

            entity.HasIndex(e => e.IsActive, "idx_import_schema_version_is_active");

            entity.HasIndex(e => e.ImportSchemaId, "idx_import_schema_version_schema_id");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("When this version was created")
                .HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasComment("FK to Profiles - who created this version");
            entity.Property(e => e.EffectiveFrom)
                .HasComment("When this version becomes effective")
                .HasColumnType("datetime");
            entity.Property(e => e.ImportSchemaId).HasComment("Reference to parent ImportSchema");
            entity.Property(e => e.IsActive).HasComment("Only one active version per schema at a time");
            entity.Property(e => e.MappingJson).HasComment("JSON mapping definition for data transformation");
            entity.Property(e => e.SchemaJson).HasComment("JSON schema definition for the import template");
            entity.Property(e => e.TemplateFileUrl)
                .HasMaxLength(500)
                .HasComment("URL of the template file for this version");
            entity.Property(e => e.VersionLabel)
                .HasMaxLength(50)
                .HasComment("Human-readable version label");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ImportSchemaVersions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_import_schema_version_created_by");

            entity.HasOne(d => d.ImportSchema).WithMany(p => p.ImportSchemaVersions)
                .HasForeignKey(d => d.ImportSchemaId)
                .HasConstraintName("fk_import_schema_version_schema");
        });

        modelBuilder.Entity<ImportSchemas>(entity =>
        {
            entity.HasKey(e => e.ImportSchemaId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.DeletedAt, "idx_importschemas_deletedat");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("When this schema was first created")
                .HasColumnType("datetime");
            entity.Property(e => e.DeletedAt)
                .HasComment("Soft delete timestamp; NULL means not deleted")
                .HasColumnType("datetime");
            entity.Property(e => e.EverActivated).HasComment("True if this schema has ever been set as active (gates soft vs hard delete)");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("Whether this schema template is available for use");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasComment("Human-readable name of the template");
            entity.Property(e => e.TemplateCode)
                .HasMaxLength(50)
                .HasComment("Unique code identifying the template type");
        });

        modelBuilder.Entity<Imports>(entity =>
        {
            entity.HasKey(e => e.ImportId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BusinessLocationId, "idx_import_business_location");

            entity.HasIndex(e => e.ImportCode, "idx_import_code").IsUnique();

            entity.HasIndex(e => e.CreatedAt, "idx_import_created_at");

            entity.HasIndex(e => e.ReceivedAt, "idx_import_date");

            entity.HasIndex(e => e.SchemaVersionId, "idx_import_schema_version");

            entity.HasIndex(e => e.Status, "idx_import_status");

            entity.HasIndex(e => e.TotalAmount, "idx_import_total_amount");

            entity.HasIndex(e => e.ImportType, "idx_import_type");

            entity.Property(e => e.BusinessLocationId)
                .HasDefaultValueSql("'1'")
                .HasComment("FK to BusinessLocations");
            entity.Property(e => e.CancelReason)
                .HasComment("Reason for cancellation")
                .HasColumnType("text");
            entity.Property(e => e.CancelledAt)
                .HasComment("When the import was cancelled")
                .HasColumnType("datetime");
            entity.Property(e => e.ConfirmedAt)
                .HasComment("When the import was confirmed")
                .HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Record creation timestamp")
                .HasColumnType("datetime");
            entity.Property(e => e.HasInvoice).HasComment("Whether the import has an invoice attached");
            entity.Property(e => e.ImagePublicId)
                .HasMaxLength(100)
                .HasComment("Cloudinary public ID for image deletion");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasComment("URL of attached image/document");
            entity.Property(e => e.ImportCode)
                .HasMaxLength(50)
                .HasComment("Auto-generated import code (e.g. PNK-2026-001)");
            entity.Property(e => e.ImportType)
                .HasMaxLength(50)
                .HasDefaultValueSql("'INVOICE'")
                .HasComment("INVOICE, INVENTORY_ADJUSTMENT, RETURN");
            entity.Property(e => e.Note).HasColumnType("text");
            entity.Property(e => e.ReceivedAt).HasColumnType("datetime");
            entity.Property(e => e.SchemaDataJson).HasComment("Stored data captured from schema form");
            entity.Property(e => e.SchemaVersionId).HasComment("FK to ImportSchemaVersions");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'")
                .HasComment("DRAFT, CONFIRMED, CANCELLED");
            entity.Property(e => e.Supplier)
                .HasMaxLength(200)
                .HasComment("Supplier name (free text)");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(15, 2)
                .HasComment("Total amount");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasComment("Last update timestamp")
                .HasColumnType("datetime");

            entity.HasOne(d => d.BusinessLocation).WithMany(p => p.Imports)
                .HasForeignKey(d => d.BusinessLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_import_business_location");

            entity.HasOne(d => d.SchemaVersion).WithMany(p => p.Imports)
                .HasForeignKey(d => d.SchemaVersionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_import_schema_version");
        });

        modelBuilder.Entity<OrderDetails>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Chi tiết dòng sản phẩm trong đơn hàng (snapshot giá tại thời điểm bán)"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.OrderId, "idx_order_detail_order");

            entity.HasIndex(e => e.SaleItemId, "idx_order_detail_sale_item");

            entity.Property(e => e.Amount)
                .HasPrecision(15, 2)
                .HasComment("Thành tiền = Quantity * UnitPrice - Discount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Discount)
                .HasPrecision(15, 2)
                .HasComment("Chiết khấu theo dòng sản phẩm");
            entity.Property(e => e.OrderId).HasComment("FK to Orders");
            entity.Property(e => e.Quantity)
                .HasDefaultValueSql("'1'")
                .HasComment("Số lượng bán");
            entity.Property(e => e.SaleItemId).HasComment("FK to SaleItems (live reference)");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(15, 2)
                .HasComment("Snapshot đơn giá tại thời điểm tạo đơn");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_order_detail_order");

            entity.HasOne(d => d.SaleItem).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.SaleItemId)
                .HasConstraintName("fk_order_detail_sale_item");
        });

        modelBuilder.Entity<Orders>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Đơn hàng bán lẻ tại cửa hàng"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.OrderCode, "idx_order_code").IsUnique();

            entity.HasIndex(e => e.CreatedAt, "idx_order_created");

            entity.HasIndex(e => e.DebtorId, "idx_order_debtor");

            entity.HasIndex(e => e.RefOrderId, "idx_order_ref");

            entity.HasIndex(e => e.Status, "idx_order_status");

            entity.Property(e => e.BankAmount)
                .HasPrecision(15, 2)
                .HasComment("Số tiền thanh toán qua ngân hàng/chuyển khoản");
            entity.Property(e => e.BillMetadata)
                .HasComment("Thông tin hóa đơn bổ sung (JSON tự do)")
                .HasColumnType("json");
            entity.Property(e => e.CancelReason)
                .HasComment("Lý do huỷ chi tiết (free text)")
                .HasColumnType("text");
            entity.Property(e => e.CancelledAt)
                .HasComment("Thời điểm đơn hàng bị huỷ")
                .HasColumnType("datetime");
            entity.Property(e => e.CancelledBy).HasComment("UserId người huỷ đơn");
            entity.Property(e => e.CashAmount)
                .HasPrecision(15, 2)
                .HasComment("Số tiền thanh toán bằng tiền mặt");
            entity.Property(e => e.CompletedAt)
                .HasComment("Thời điểm đơn hàng hoàn thành")
                .HasColumnType("datetime");
            entity.Property(e => e.CompletedBy).HasComment("UserId người hoàn thành đơn");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasComment("UserId người tạo đơn");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(255)
                .HasComment("Tên khách hàng vãng lai (không cần trong hệ thống)");
            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(20)
                .HasComment("SĐT khách hàng vãng lai");
            entity.Property(e => e.DebtAmount)
                .HasPrecision(15, 2)
                .HasComment("Số tiền ghi nợ = TotalAmount - CashAmount - BankAmount");
            entity.Property(e => e.DebtorId).HasComment("FK to Debtors: khách nợ (nếu có)");
            entity.Property(e => e.Discount)
                .HasPrecision(15, 2)
                .HasComment("Chiết khấu tổng đơn hàng");
            entity.Property(e => e.Note)
                .HasComment("Ghi chú của đơn hàng")
                .HasColumnType("text");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(50)
                .HasComment("Mã đơn hàng duy nhất, format: ORD-YYYYMMDD-NNN");
            entity.Property(e => e.RefOrderId).HasComment("FK tự tham chiếu: đơn gốc bị thay thế khi sửa đơn đã hoàn thành");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'")
                .HasComment("pending | completed | cancelled");
            entity.Property(e => e.SubTotal)
                .HasPrecision(15, 2)
                .HasComment("Tổng tiền hàng trước chiết khấu");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(15, 2)
                .HasComment("Tổng tiền phải thanh toán = SubTotal - Discount");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy).HasComment("UserId người cập nhật gần nhất");

            entity.HasOne(d => d.Debtor).WithMany(p => p.Orders)
                .HasForeignKey(d => d.DebtorId)
                .HasConstraintName("fk_order_debtor");

            entity.HasOne(d => d.RefOrder).WithMany(p => p.InverseRefOrder)
                .HasForeignKey(d => d.RefOrderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_order_ref_order");
        });

        modelBuilder.Entity<PlanFeatures>(entity =>
        {
            entity.HasKey(e => new { e.SubscriptionPlanId, e.FeatureId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity
                .ToTable(tb => tb.HasComment("Mapping features to subscription plans and defining usage limits"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.FeatureId, "fk_plan_feature_feature");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UsageLimit).HasDefaultValueSql("'-1'");

            entity.HasOne(d => d.Feature).WithMany(p => p.PlanFeatures)
                .HasForeignKey(d => d.FeatureId)
                .HasConstraintName("planfeatures_ibfk_2");

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.PlanFeatures)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .HasConstraintName("planfeatures_ibfk_1");
        });

        modelBuilder.Entity<ProductPricePolicies>(entity =>
        {
            entity.HasKey(e => e.ProductPricePolicyId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => new { e.StartAt, e.EndAt }, "idx_price_policy_date_range");

            entity.HasIndex(e => e.IsDefault, "idx_price_policy_is_default");

            entity.HasIndex(e => e.SaleItemId, "idx_price_policy_sale_item");

            entity.Property(e => e.EndAt).HasColumnType("datetime");
            entity.Property(e => e.IsDefault).HasComment("Is default price");
            entity.Property(e => e.Price)
                .HasPrecision(15, 2)
                .HasComment("Applied price");
            entity.Property(e => e.StartAt).HasColumnType("datetime");

            entity.HasOne(d => d.SaleItem).WithMany(p => p.ProductPricePolicies)
                .HasForeignKey(d => d.SaleItemId)
                .HasConstraintName("fk_product_price_policy_sale_item");
        });

        modelBuilder.Entity<Products>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BusinessLocationId, "idx_product_business_location");

            entity.HasIndex(e => e.BusinessTypeId, "idx_product_business_type");

            entity.HasIndex(e => e.ImagePublicId, "idx_product_image_publicid");

            entity.HasIndex(e => e.Manufacturer, "idx_product_manufacturer");

            entity.HasIndex(e => e.ProductName, "idx_product_name");

            entity.HasIndex(e => e.Sku, "idx_product_sku");

            entity.HasIndex(e => e.Status, "idx_product_status");

            entity.Property(e => e.BusinessLocationId).HasComment("Warehouse/location of product");
            entity.Property(e => e.BusinessTypeId).HasComment("Business type category");
            entity.Property(e => e.CostPrice)
                .HasPrecision(15, 2)
                .HasComment("Cost price");
            entity.Property(e => e.DeletedAt)
                .HasComment("Soft delete timestamp")
                .HasColumnType("datetime");
            entity.Property(e => e.ImagePublicId).HasComment("Cloudinary public ID for image deletion");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Manufacturer).HasComment("Manufacturer name");
            entity.Property(e => e.SellingPrice)
                .HasPrecision(15, 2)
                .HasComment("Giá bán theo base unit");
            entity.Property(e => e.Sku)
                .HasMaxLength(100)
                .HasComment("Stock Keeping Unit code");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'active'")
                .HasComment("Product sale status")
                .HasColumnType("enum('active','inactive','discontinued')");
            entity.Property(e => e.Stock).HasComment("Quantity in stock");
            entity.Property(e => e.TrackInventory)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("Whether to track inventory quantity");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Unit'")
                .HasComment("Unit of measurement");

            entity.HasOne(d => d.BusinessLocation).WithMany(p => p.Products)
                .HasForeignKey(d => d.BusinessLocationId)
                .HasConstraintName("fk_product_business_location");

            entity.HasOne(d => d.BusinessType).WithMany(p => p.Products)
                .HasForeignKey(d => d.BusinessTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_business_type");
        });

        modelBuilder.Entity<ProductsImports>(entity =>
        {
            entity.HasKey(e => e.ProductImportId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ImportId, "idx_product_import_import");

            entity.HasIndex(e => e.ProductId, "idx_product_import_product");

            entity.Property(e => e.BaseUnit)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Unit'")
                .HasComment("Base/smallest inventory unit");
            entity.Property(e => e.CostPrice)
                .HasPrecision(15, 2)
                .HasComment("Cost price per import unit");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ImportId).HasComment("FK to Imports");
            entity.Property(e => e.ProductId).HasComment("FK to Products");
            entity.Property(e => e.Quantity).HasComment("Import quantity");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(15, 2)
                .HasComment("Total price");

            entity.HasOne(d => d.Import).WithMany(p => p.ProductsImports)
                .HasForeignKey(d => d.ImportId)
                .HasConstraintName("fk_product_import_new_import");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductsImports)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_product_import_new_product");
        });

        modelBuilder.Entity<Profiles>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.AccountId, "idx_profile_account").IsUnique();

            entity.HasIndex(e => e.FullName, "idx_profile_full_name");

            entity.Property(e => e.AccountId).HasComment("FK to Accounts");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasComment("Profile avatar URL");
            entity.Property(e => e.FullName).HasComment("Full name");
            entity.Property(e => e.StripeCustomerId)
                .HasMaxLength(255)
                .HasComment("Stripe Customer ID for payment orchestration");
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasComment("Personal tax identification number");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithOne(p => p.Profiles)
                .HasForeignKey<Profiles>(d => d.AccountId)
                .HasConstraintName("fk_profile_account");
        });

        modelBuilder.Entity<RefreshTokens>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.AccountId, "idx_rt_account_id");

            entity.HasIndex(e => e.ExpiresAt, "idx_rt_expires_at");

            entity.HasIndex(e => e.TokenHash, "idx_rt_token_hash");

            entity.Property(e => e.AccountId).HasComment("FK to Accounts");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(500)
                .HasComment("User-Agent or device identifier");
            entity.Property(e => e.ExpiresAt)
                .HasComment("Token expiry timestamp")
                .HasColumnType("datetime");
            entity.Property(e => e.RevokedAt)
                .HasComment("NULL = active, NOT NULL = revoked")
                .HasColumnType("datetime");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(512)
                .HasComment("SHA-256 hash of refresh token");
            entity.Property(e => e.TokenSalt)
                .HasMaxLength(128)
                .HasComment("Random salt (Base64)");

            entity.HasOne(d => d.Account).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("fk_refresh_token_account");
        });

        modelBuilder.Entity<Revenues>(entity =>
        {
            entity.HasKey(e => e.RevenueId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Doanh thu cửa hàng - source-of-truth cho mọi khoản thu"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BusinessLocationId, "idx_revenue_location");

            entity.HasIndex(e => new { e.BusinessLocationId, e.RevenueDate }, "idx_revenue_location_date");

            entity.HasIndex(e => e.RevenueType, "idx_revenue_type");

            entity.Property(e => e.Amount)
                .HasPrecision(15, 2)
                .HasComment("Giá trị doanh thu");
            entity.Property(e => e.BusinessLocationId).HasComment("FK to BusinessLocations");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasComment("UserId người tạo bản ghi");
            entity.Property(e => e.DeletedAt)
                .HasComment("Soft delete")
                .HasColumnType("datetime");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasComment("Mô tả nội dung doanh thu");
            entity.Property(e => e.MoneyChannel)
                .HasMaxLength(10)
                .HasComment("cash | bank | debt");
            entity.Property(e => e.OrderId).HasComment("Soft reference to Order, nullable because some revenues are manual");
            entity.Property(e => e.RevenueDate).HasComment("Ngày ghi nhận doanh thu");
            entity.Property(e => e.RevenueType)
                .HasMaxLength(20)
                .HasComment("sale | manual");

            entity.HasOne(d => d.BusinessLocation).WithMany(p => p.Revenues)
                .HasForeignKey(d => d.BusinessLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_revenue_location");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.Name, "idx_name").IsUnique();

            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdateAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<SaleItems>(entity =>
        {
            entity.HasKey(e => e.SaleItemId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ProductId, "idx_sale_item_product");

            entity.HasIndex(e => e.DeletedAt, "idx_saleitems_deletedat");

            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.Quantity).HasDefaultValueSql("'1'");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Unit'")
                .HasComment("Unit of measurement");

            entity.HasOne(d => d.Product).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_sale_item_product");
        });

        modelBuilder.Entity<StockMovements>(entity =>
        {
            entity.HasKey(e => e.StockMovementId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.CreatedAt, "idx_stock_movement_created_at");

            entity.HasIndex(e => e.ProductId, "idx_stock_movement_product");

            entity.HasIndex(e => new { e.ReferenceType, e.ReferenceId }, "idx_stock_movement_reference");

            entity.HasIndex(e => e.MovementType, "idx_stock_movement_type");

            entity.Property(e => e.BalanceAfter).HasComment("Stock balance after this movement");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Memo)
                .HasMaxLength(1000)
                .HasComment("Manual note/reason for this stock movement");
            entity.Property(e => e.MovementType)
                .HasMaxLength(50)
                .HasComment("IN, OUT, ADJUSTMENT");
            entity.Property(e => e.ProductId).HasComment("FK to Products");
            entity.Property(e => e.Quantity).HasComment("Quantity moved (positive for IN, negative for OUT)");
            entity.Property(e => e.ReferenceId).HasComment("ID of the reference entity (ImportId, OrderId, etc.)");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(50)
                .HasComment("IMPORT, ORDER, ADJUSTMENT");

            entity.HasOne(d => d.Product).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_stock_movement_product");
        });

        modelBuilder.Entity<SubscriptionAuditLogs>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Audit trail for subscription lifecycle events"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.SubscriptionId, "fk_audit_log_subscription");

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Details).HasColumnType("json");

            entity.HasOne(d => d.Subscription).WithMany(p => p.SubscriptionAuditLogs)
                .HasForeignKey(d => d.SubscriptionId)
                .HasConstraintName("subscriptionauditlogs_ibfk_1");
        });

        modelBuilder.Entity<SubscriptionPlans>(entity =>
        {
            entity.HasKey(e => e.SubscriptionPlanId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Available subscription plans"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.StripePriceId, "StripePriceId").IsUnique();

            entity.Property(e => e.BasePrice).HasPrecision(15, 2);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountedPrice).HasPrecision(15, 2);
            entity.Property(e => e.DurationDays).HasDefaultValueSql("'30'");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Subscriptions>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("User subscriptions tracking"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.SubscriptionPlanId, "fk_subscription_plan");

            entity.HasIndex(e => new { e.EndDate, e.Status }, "idx_sub_end_date_status");

            entity.HasIndex(e => new { e.OwnerProfileId, e.Status }, "idx_sub_owner_status");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.LastReminderSentAt).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.OwnerProfile).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.OwnerProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("subscriptions_ibfk_1");

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("subscriptions_ibfk_2");
        });

        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasKey(e => e.SystemConfigId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.UpdatedBy, "fk_system_config_updated_by");

            entity.HasIndex(e => e.Name, "idx_system_config_name").IsUnique();

            entity.Property(e => e.Description)
                .HasComment("Description of this config entry")
                .HasColumnType("text");
            entity.Property(e => e.Name).HasComment("Configuration key name");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy).HasComment("FK to Profiles - who last updated");
            entity.Property(e => e.Value)
                .HasComment("Configuration value")
                .HasColumnType("text");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.SystemConfig)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_system_config_updated_by");
        });

        modelBuilder.Entity<Transactions>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PRIMARY");

            entity
                .ToTable(tb => tb.HasComment("Payment transactions for subscriptions"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.IdempotencyKey, "IdempotencyKey").IsUnique();

            entity.HasIndex(e => e.SubscriptionPlanId, "fk_transaction_plan");

            entity.HasIndex(e => e.ProfileId, "fk_transaction_profile");

            entity.HasIndex(e => e.SubscriptionId, "fk_transaction_subscription");

            entity.HasIndex(e => e.StripeCheckoutSessionId, "idx_txn_stripe_session");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'VND'");
            entity.Property(e => e.FinalAmount).HasPrecision(15, 2);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(100);
            entity.Property(e => e.PaidAt).HasColumnType("datetime");
            entity.Property(e => e.PlanPrice).HasPrecision(15, 2);
            entity.Property(e => e.ProrationCredit).HasPrecision(15, 2);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'");
            entity.Property(e => e.StripePaymentIntentId).HasMaxLength(255);
            entity.Property(e => e.TransactionType).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Profile).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.ProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transactions_ibfk_1");

            entity.HasOne(d => d.Subscription).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("transactions_ibfk_3");

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transactions_ibfk_2");
        });

        modelBuilder.Entity<UserLocationAssignments>(entity =>
        {
            entity.HasKey(e => e.UserLocationAssignmentId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => new { e.BusinessLocationId, e.IsOwner, e.IsActive }, "idx_ula_location_owner_active");

            entity.HasIndex(e => e.BusinessLocationId, "idx_user_location_assignment_location");

            entity.HasIndex(e => e.UserId, "idx_user_location_assignment_user");

            entity.HasIndex(e => new { e.UserId, e.BusinessLocationId }, "idx_user_location_lookup");

            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("When employee/user was assigned to location")
                .HasColumnType("datetime");
            entity.Property(e => e.BusinessLocationId).HasComment("Assigned location");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.IsOwner).HasComment("Is the owner of this location");
            entity.Property(e => e.UnassignedAt)
                .HasComment("When employee/user was removed from location")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasComment("Assigned user");

            entity.HasOne(d => d.BusinessLocation).WithMany(p => p.UserLocationAssignments)
                .HasForeignKey(d => d.BusinessLocationId)
                .HasConstraintName("fk_user_location_assignment_location");

            entity.HasOne(d => d.User).WithMany(p => p.UserLocationAssignments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_location_assignment_profile");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
