using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BizFlow.Infrastructure.DataContext;

public partial class BizFlowDbContext
{
    public virtual DbSet<AccountingPeriod> AccountingPeriods { get; set; }

    public virtual DbSet<AccountingPeriodAuditLog> AccountingPeriodAuditLogs { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Force all DateTimes from DB to have Kind = Utc so JSON correctly outputs 'Z'
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }

        modelBuilder.Entity<AccountingPeriod>(entity =>
        {
            entity.HasKey(e => e.PeriodId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.PeriodType)
                .HasMaxLength(10)
                .HasComment("quarter | year");

            entity.Property(e => e.StartDate).HasColumnType("date");

            entity.Property(e => e.EndDate).HasColumnType("date");

            entity.Property(e => e.OpeningCashBalance)
                .HasPrecision(15, 2)
                .HasComment("Opening cash balance");

            entity.Property(e => e.OpeningBankBalance)
                .HasPrecision(15, 2)
                .HasComment("Opening bank balance");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'open'")
                .HasComment("open | finalized | reopened");

            entity.Property(e => e.FinalizedAt).HasColumnType("datetime");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("datetime");

            entity.HasIndex(e => new { e.BusinessLocationId, e.PeriodType, e.Year, e.Quarter }, "idx_period_unique").IsUnique();
            entity.HasIndex(e => e.Status, "idx_period_status");
            entity.HasIndex(e => e.Year, "idx_period_year");

            entity.HasOne(d => d.BusinessLocation)
                .WithMany(p => p.AccountingPeriods)
                .HasForeignKey(d => d.BusinessLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_period_location");
        });

        modelBuilder.Entity<AccountingPeriodAuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .HasComment("period_created | period_finalized | period_reopened | book_created | book_exported | group_suggestion");

            entity.Property(e => e.OldValue).HasColumnType("json");

            entity.Property(e => e.NewValue).HasColumnType("json");

            entity.Property(e => e.Reason).HasColumnType("text");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasIndex(e => e.PeriodId, "idx_audit_period");
            entity.HasIndex(e => e.Action, "idx_audit_action");
            entity.HasIndex(e => e.CreatedAt, "idx_audit_date");

            entity.HasOne(d => d.Period)
                .WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.PeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_audit_period");
        });

        // Global Query Filter: Soft Delete for BusinessLocation
        modelBuilder.Entity<BusinessLocation>().HasQueryFilter(e => e.DeletedAt == null);

        // Global Query Filter: Soft Delete for Product (+ Parent Location Check)
        modelBuilder.Entity<Product>().HasQueryFilter(e => e.DeletedAt == null && e.BusinessLocation.DeletedAt == null);

        // Global Query Filter: Soft Delete for SaleItem (+ Parent Product Check)
        modelBuilder.Entity<SaleItem>().HasQueryFilter(s => s.DeletedAt == null && s.Product.DeletedAt == null);

        // Global Query Filter: Soft Delete for ImportSchema
        modelBuilder.Entity<ImportSchema>().HasQueryFilter(e => e.DeletedAt == null);

        // Global Query Filter: Soft Delete for Debtor
        modelBuilder.Entity<Debtor>().HasQueryFilter(e => e.DeletedAt == null);

        // Global Query Filter: Soft Delete for Revenue (manual only deletable)
        modelBuilder.Entity<Revenue>().HasQueryFilter(e => e.DeletedAt == null);

        // Global Query Filter: Soft Delete for Cost
        modelBuilder.Entity<Cost>().HasQueryFilter(e => e.DeletedAt == null);
    }
}
