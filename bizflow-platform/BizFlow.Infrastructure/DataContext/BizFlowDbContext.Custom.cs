using BizFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BizFlow.Infrastructure.DataContext;

public partial class BizFlowDbContext
{
    public virtual DbSet<AccountingPeriod> AccountingPeriods { get; set; }

    public virtual DbSet<AccountingPeriodAuditLog> AccountingPeriodAuditLogs { get; set; }

    public virtual DbSet<NotificationTemplate> NotificationTemplates { get; set; }

    public virtual DbSet<NotificationDispatch> NotificationDispatches { get; set; }

    public virtual DbSet<NotificationRecord> Notifications { get; set; }

    public virtual DbSet<NotificationOutboxMessage> NotificationOutboxMessages { get; set; }

    public virtual DbSet<UserNotification> UserNotifications { get; set; }

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

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.NotificationTemplateId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.EventCode, "uq_notification_templates_event_code").IsUnique();
            entity.HasIndex(e => e.IsActive, "idx_notification_templates_is_active");
            entity.HasIndex(e => e.NotificationType, "idx_notification_templates_type");

            entity.Property(e => e.EventCode).HasMaxLength(100);
            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.TitleTemplate).HasMaxLength(250);
            entity.Property(e => e.ContentTemplate).HasColumnType("text");
            entity.Property(e => e.DefaultActionType).HasMaxLength(50);
            entity.Property(e => e.DefaultTargetScreen).HasMaxLength(100);
            entity.Property(e => e.DefaultActionPayloadJson).HasColumnType("json");
            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<NotificationDispatch>(entity =>
        {
            entity.HasKey(e => e.NotificationDispatchId).HasName("PRIMARY");

            entity.ToTable("NotificationCampaigns");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.NotificationTemplateId, "idx_notification_dispatches_template");
            entity.HasIndex(e => e.CreatedByUserId, "idx_notification_dispatches_created_by");
            entity.HasIndex(e => e.Status, "idx_notification_dispatches_status");
            entity.HasIndex(e => e.ScheduledAt, "idx_notification_dispatches_scheduled_at");
            entity.HasIndex(e => e.CreatedAt, "idx_notification_dispatches_created_at");

            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NORMAL'");
            entity.Property(e => e.DataJson).HasColumnType("json");
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.TargetScreen).HasMaxLength(100);
            entity.Property(e => e.ActionPayloadJson).HasColumnType("json");
            entity.Property(e => e.RecipientScope)
                .HasMaxLength(30)
                .HasDefaultValueSql("'SPECIFIC_USERS'");
            entity.Property(e => e.RecipientUserIdsJson).HasColumnType("json");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'");
            entity.Property(e => e.ErrorMessage).HasColumnType("text");
            entity.Property(e => e.ScheduledAt).HasColumnType("datetime");
            entity.Property(e => e.SentAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("datetime");

            entity.HasOne(d => d.NotificationTemplate)
                .WithMany(p => p.NotificationDispatches)
                .HasForeignKey(d => d.NotificationTemplateId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_notification_dispatches_template");

            entity.HasOne(d => d.CreatedByUser)
                .WithMany(p => p.NotificationDispatches)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_notification_dispatches_created_by");
        });

        modelBuilder.Entity<NotificationRecord>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PRIMARY");

            entity.ToTable("Notifications");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.CreatedAt, "idx_notifications_created_at");
            entity.HasIndex(e => e.NotificationType, "idx_notifications_type");

            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NORMAL'");
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.TargetScreen).HasMaxLength(100);
            entity.Property(e => e.ActionPayloadJson).HasColumnType("json");
            entity.Property(e => e.DataJson).HasColumnType("json");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.UserNotificationId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.UserId, "idx_user_notifications_user_id");
            entity.HasIndex(e => new { e.UserId, e.ReadAt }, "idx_user_notifications_user_read");
            entity.HasIndex(e => e.CreatedAt, "idx_user_notifications_created_at");
            entity.HasIndex(e => e.NotificationId, "idx_user_notifications_notification_id");

            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NORMAL'");
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.TargetScreen).HasMaxLength(100);
            entity.Property(e => e.ActionPayloadJson).HasColumnType("json");
            entity.Property(e => e.DeliveryStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'IN_QUEUE'");
            entity.Property(e => e.ErrorMessage).HasColumnType("text");
            entity.Property(e => e.SentAt).HasColumnType("datetime");
            entity.Property(e => e.ReadAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Notification)
                .WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_user_notifications_notification");

            entity.HasOne(d => d.User)
                .WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_notifications_user");
        });

        modelBuilder.Entity<NotificationOutboxMessage>(entity =>
        {
            entity.HasKey(e => e.NotificationOutboxMessageId).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.Status, "idx_notification_outbox_status");
            entity.HasIndex(e => e.CreatedAt, "idx_notification_outbox_created_at");

            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.PayloadJson).HasColumnType("json");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'");
            entity.Property(e => e.RetryCount).HasDefaultValueSql("'0'");
            entity.Property(e => e.LastError).HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.LastAttemptAt).HasColumnType("datetime");
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
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
