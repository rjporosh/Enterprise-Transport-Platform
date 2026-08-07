using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates", "notification");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Key).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Locale).HasMaxLength(10).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.Subject).HasMaxLength(500);
        builder.Property(t => t.Body).IsRequired();

        builder.Property(t => t.RowVersion).IsRowVersion();

        // A given (Key, Channel, Locale) triple identifies exactly one
        // active template -- soft-deleted rows are excluded via a filtered
        // unique index so a deleted+recreated template with the same key
        // does not collide with its own tombstone.
        // Plain (non-filtered) unique index deliberately, not a partial/filtered
        // index on "not deleted" -- MySQL has no filtered-index support at all,
        // and a filtered-index predicate's SQL syntax differs across
        // Postgres/SqlServer, which would break the "switch provider via
        // config only" goal this service follows from AuthService's
        // precedent (see docs/architecture, "Database portability"). Trade-off:
        // once a template's (Key, Channel, Locale) is soft-deleted, that exact
        // triple cannot be reused for a new template -- acceptable, and worth
        // it for zero-code-change provider switching.
        builder.HasIndex(t => new { t.Key, t.Channel, t.Locale }).IsUnique();
    }
}
