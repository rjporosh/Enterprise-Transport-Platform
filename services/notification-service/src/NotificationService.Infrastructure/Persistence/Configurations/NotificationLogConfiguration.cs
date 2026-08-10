using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs", "notification");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProviderMessageId).HasMaxLength(200);
        builder.Property(l => l.Error).HasMaxLength(4000);

        builder.HasIndex(l => l.NotificationId);
    }
}
