using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", "notification");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Recipient).HasMaxLength(320).IsRequired();
        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(n => n.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(500);
        builder.Property(n => n.Body).IsRequired();
        builder.Property(n => n.DataPayload);
        builder.Property(n => n.SourceReference).HasMaxLength(200);
        builder.Property(n => n.Locale).HasMaxLength(10);
        builder.Property(n => n.LastError).HasMaxLength(4000);

        // Optimistic concurrency (CLAUDE.md, "Optimistic Concurrency"): the
        // dispatch job and a manual-retry API call could race on the same
        // row. IsRowVersion() maps to Postgres xmin automatically under
        // Npgsql, and to a real rowversion/timestamp column on SqlServer;
        // under MySql (no native rowversion type) EF Core falls back to a
        // shadow concurrency check via a normal column -- see
        // docs/architecture, "Database portability" for that caveat.
        builder.Property(n => n.Version).IsRowVersion();

        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => new { n.Status, n.NextRetryAtUtc });
        builder.HasIndex(n => new { n.Status, n.ScheduledForUtc });
        builder.HasIndex(n => n.SourceReference);
        builder.HasIndex(n => n.CreatedAtUtc);
        builder.HasIndex(n => n.Recipient);

        builder.HasMany(n => n.Logs)
            .WithOne()
            .HasForeignKey(l => l.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Logs is exposed as IReadOnlyCollection backed by a private List<T>
        // field (see Notification.cs) -- field access mode so EF Core
        // materializes into that field directly rather than requiring a
        // public settable property that would break encapsulation.
        builder.Navigation(n => n.Logs).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
