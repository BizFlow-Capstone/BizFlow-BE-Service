using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.DataContext;

public partial class BizFlowDbContext : DbContext
{
    public BizFlowDbContext(DbContextOptions<BizFlowDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BusinessLocation> BusinessLocations { get; set; }

    public virtual DbSet<BusinessType> BusinessTypes { get; set; }

    public virtual DbSet<BusinessTypeTax> BusinessTypeTaxes { get; set; }

    public virtual DbSet<Hire> Hires { get; set; }

    public virtual DbSet<Import> Imports { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImport> ProductImports { get; set; }

    public virtual DbSet<ProductPricePolicy> ProductPricePolicies { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SaleItem> SaleItems { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserLocationAssignment> UserLocationAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<BusinessLocation>(entity =>
        {
            entity.HasKey(e => e.BusinessLocationId).HasName("PRIMARY");

            entity
                .ToTable("BusinessLocation")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.City, "idx_business_location_city");

            entity.HasIndex(e => e.IsActive, "idx_business_location_is_active");

            entity.HasIndex(e => e.Name, "idx_business_location_name");

            entity.Property(e => e.Address).HasColumnType("text");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.Name).HasComment("Location/store name");
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasComment("Tax identification number");
        });

        modelBuilder.Entity<BusinessType>(entity =>
        {
            entity.HasKey(e => e.BusinessTypeId).HasName("PRIMARY");

            entity
                .ToTable("BusinessType")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.CreatedById, "fk_business_type_created_by");

            entity.HasIndex(e => e.ModifiedById, "fk_business_type_modified_by");

            entity.HasIndex(e => e.Code, "idx_business_type_code");

            entity.HasIndex(e => e.Name, "idx_business_type_name");

            entity.HasIndex(e => e.Status, "idx_business_type_status");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.LastModifiedDate)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'active'");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.BusinessTypeCreatedBies)
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_business_type_created_by");

            entity.HasOne(d => d.ModifiedBy).WithMany(p => p.BusinessTypeModifiedBies)
                .HasForeignKey(d => d.ModifiedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_business_type_modified_by");
        });

        modelBuilder.Entity<BusinessTypeTax>(entity =>
        {
            entity.HasKey(e => e.BusinessTypeTaxId).HasName("PRIMARY");

            entity
                .ToTable("BusinessTypeTax")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.CreatedById, "fk_business_type_tax_created_by");

            entity.HasIndex(e => e.BusinessTypeId, "idx_business_type_tax_business_type");

            entity.HasIndex(e => new { e.EffectiveFrom, e.EffectiveTo }, "idx_business_type_tax_effective");

            entity.HasIndex(e => e.TaxType, "idx_business_type_tax_type");

            entity.Property(e => e.CreatedDate)
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

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.BusinessTypeTaxes)
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_business_type_tax_created_by");
        });

        modelBuilder.Entity<Hire>(entity =>
        {
            entity.HasKey(e => e.HireId).HasName("PRIMARY");

            entity
                .ToTable("Hire")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.EmployeeId, "idx_hire_employee");

            entity.HasIndex(e => e.IsActive, "idx_hire_is_active");

            entity.HasIndex(e => e.OwnerId, "idx_hire_owner");

            entity.HasIndex(e => new { e.OwnerId, e.EmployeeId }, "idx_hire_owner_employee").IsUnique();

            entity.HasIndex(e => e.StartAt, "idx_hire_start_at");

            entity.Property(e => e.EmployeeId).HasComment("Employee being hired");
            entity.Property(e => e.EndAt)
                .HasComment("End date of employment (NULL if still active)")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasComment("Employment status");
            entity.Property(e => e.OwnerId).HasComment("Owner who hired the employee");
            entity.Property(e => e.StartAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Start date of employment")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Employee).WithMany(p => p.HireEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("fk_hire_employee");

            entity.HasOne(d => d.Owner).WithMany(p => p.HireOwners)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_hire_owner");
        });

        modelBuilder.Entity<Import>(entity =>
        {
            entity.HasKey(e => e.ImportId).HasName("PRIMARY");

            entity
                .ToTable("Import")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.Date, "idx_import_date");

            entity.Property(e => e.Date)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Import date")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.ImportImage)
                .HasMaxLength(500)
                .HasComment("Import receipt image");
            entity.Property(e => e.SchemaJson)
                .HasComment("Import data schema")
                .HasColumnType("json");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(15, 2)
                .HasComment("Total amount");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PRIMARY");

            entity
                .ToTable("Product")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BusinessLocationId, "idx_product_business_location");

            entity.HasIndex(e => e.BusinessTypeId, "idx_product_business_type");

            entity.HasIndex(e => e.Manufacturer, "idx_product_manufacturer");

            entity.HasIndex(e => e.ProductName, "idx_product_name");

            entity.Property(e => e.BusinessLocationId).HasComment("Warehouse/location of product");
            entity.Property(e => e.BusinessTypeId).HasComment("Business type category");
            entity.Property(e => e.CostPrice)
                .HasPrecision(15, 2)
                .HasComment("Cost price");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Manufacturer).HasComment("Manufacturer name");
            entity.Property(e => e.Stock).HasComment("Stock quantity");
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
            entity.HasKey(e => new { e.ImportId, e.ProductId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity
                .ToTable("Product_Import")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ProductId, "idx_product_import_product");

            entity.Property(e => e.Quantity).HasComment("Import quantity");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(15, 2)
                .HasComment("Total price");

            entity.HasOne(d => d.Import).WithMany(p => p.ProductImports)
                .HasForeignKey(d => d.ImportId)
                .HasConstraintName("fk_product_import_import");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImports)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_import_product");
        });

        modelBuilder.Entity<ProductPricePolicy>(entity =>
        {
            entity.HasKey(e => e.ProductPricePolicyId).HasName("PRIMARY");

            entity
                .ToTable("ProductPricePolicy")
                .UseCollation("utf8mb4_unicode_ci");

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

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.Name, "idx_name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.SaleItemId).HasName("PRIMARY");

            entity
                .ToTable("SaleItem")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ProductId, "idx_sale_item_product");

            entity.Property(e => e.Quantity)
                .HasDefaultValueSql("'1'")
                .HasComment("Quantity");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Unit'")
                .HasComment("Unit of measurement");

            entity.HasOne(d => d.Product).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sale_item_product");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity
                .ToTable("User")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.Email, "Email").IsUnique();

            entity.HasIndex(e => e.FullName, "idx_user_full_name");

            entity.HasIndex(e => e.IsActive, "idx_user_is_active");

            entity.HasIndex(e => e.Phone, "idx_user_phone");

            entity.HasIndex(e => e.RoleId, "idx_user_role");

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasComment("Profile avatar URL");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasComment("User email (login)");
            entity.Property(e => e.EmailVerified).HasComment("Email verification status");
            entity.Property(e => e.FullName).HasComment("Full name");
            entity.Property(e => e.IsActive).HasComment("Account status");
            entity.Property(e => e.IsDeleted).HasComment("Soft delete flag");
            entity.Property(e => e.LastLoginAt)
                .HasComment("Last login timestamp")
                .HasColumnType("datetime");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasComment("Hashed password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasComment("Phone number");
            entity.Property(e => e.RoleId).HasComment("User role");
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasComment("Personal tax identification number");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_role");
        });

        modelBuilder.Entity<UserLocationAssignment>(entity =>
        {
            entity.HasKey(e => e.UserLocationAssignmentId).HasName("PRIMARY");

            entity
                .ToTable("UserLocationAssignment")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.BusinessLocationId, "idx_user_location_assignment_location");

            entity.HasIndex(e => e.UserId, "idx_user_location_assignment_user");

            entity.HasIndex(e => new { e.UserId, e.BusinessLocationId }, "idx_user_location_unique").IsUnique();

            entity.Property(e => e.BusinessLocationId).HasComment("Assigned location");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.IsOwner).HasComment("Is the owner of this location");
            entity.Property(e => e.UserId).HasComment("Assigned user");

            entity.HasOne(d => d.BusinessLocation).WithMany(p => p.UserLocationAssignments)
                .HasForeignKey(d => d.BusinessLocationId)
                .HasConstraintName("fk_user_location_assignment_location");

            entity.HasOne(d => d.User).WithMany(p => p.UserLocationAssignments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_location_assignment_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
