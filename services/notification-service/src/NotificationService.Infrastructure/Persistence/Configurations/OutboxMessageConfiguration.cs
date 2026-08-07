using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Infrastructure.Persistence.Outbox;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", "notification");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Payload).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(4000);

        builder.HasIndex(m => new { m.ProcessedOnUtc, m.RetryCount });
    }
}
