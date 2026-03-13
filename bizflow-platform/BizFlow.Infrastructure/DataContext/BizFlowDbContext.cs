using System;
using BizFlow.Domain.Entities;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.DataContext;

public partial class BizFlowDbContext : DbContext
{
    public BizFlowDbContext(DbContextOptions<BizFlowDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<BusinessLocation> BusinessLocations { get; set; }

    public virtual DbSet<BusinessTypeTax> BusinessTypeTaxes { get; set; }

    public virtual DbSet<BusinessType> BusinessTypes { get; set; }

    public virtual DbSet<Credential> Credentials { get; set; }

    public virtual DbSet<Hire> Hires { get; set; }

    public virtual DbSet<ImportSchemaVersion> ImportSchemaVersions { get; set; }

    public virtual DbSet<ImportSchema> ImportSchemas { get; set; }

    public virtual DbSet<Import> Imports { get; set; }

    public virtual DbSet<ProductPricePolicy> ProductPricePolicies { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImport> ProductsImports { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SaleItem> SaleItems { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<SystemConfig> SystemConfig { get; set; }

    public virtual DbSet<UserLocationAssignment> UserLocationAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Account>(entity =>
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

        modelBuilder.Entity<BusinessLocation>(entity =>
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

        modelBuilder.Entity<BusinessTypeTax>(entity =>
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

        modelBuilder.Entity<BusinessType>(entity =>
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

        modelBuilder.Entity<Credential>(entity =>
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

        modelBuilder.Entity<Hire>(entity =>
        {
            entity.HasKey(e => e.HireId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.EmployeeId, "idx_hire_employee");

            entity.HasIndex(e => e.IsActive, "idx_hire_is_active");

            entity.HasIndex(e => e.OwnerId, "idx_hire_owner");

            entity.HasIndex(e => new { e.OwnerId, e.EmployeeId }, "idx_hire_owner_employee").IsUnique();

            entity.Property(e => e.EmployeeId).HasComment("Employee being hired");
            entity.Property(e => e.EndAt)
                .HasComment("End date of employment (NULL if still active)")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("Hiring status");
            entity.Property(e => e.OwnerId).HasComment("Owner who hired the employee");
            entity.Property(e => e.StartAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Start date of employment")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Employee).WithMany(p => p.HiresEmployee)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("fk_hire_employee");

            entity.HasOne(d => d.Owner).WithMany(p => p.HiresOwner)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_hire_owner");
        });

        modelBuilder.Entity<ImportSchemaVersion>(entity =>
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

        modelBuilder.Entity<ImportSchema>(entity =>
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

        modelBuilder.Entity<Import>(entity =>
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

        modelBuilder.Entity<ProductPricePolicy>(entity =>
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

        modelBuilder.Entity<Product>(entity =>
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
                .HasComment("GiÃ¡ bÃ¡n theo base unit");
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

        modelBuilder.Entity<ProductImport>(entity =>
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_import_new_product");
        });

        modelBuilder.Entity<Profile>(entity =>
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
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasComment("Personal tax identification number");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithOne(p => p.Profile)
                .HasForeignKey<Profile>(d => d.AccountId)
                .HasConstraintName("fk_profile_account");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
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

        modelBuilder.Entity<Role>(entity =>
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

        modelBuilder.Entity<SaleItem>(entity =>
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sale_item_product");
        });

        modelBuilder.Entity<StockMovement>(entity =>
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_stock_movement_product");
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

        modelBuilder.Entity<UserLocationAssignment>(entity =>
        {
            entity.HasKey(e => e.UserLocationAssignmentId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

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
