using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Infrastructure.Persistence.Outbox;

namespace PaymentService.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType).IsRequired().HasMaxLength(500);
        builder.Property(o => o.Payload).IsRequired();
        builder.Property(o => o.OccurredOnUtc).IsRequired();
        builder.Property(o => o.ProcessedOnUtc);
        builder.Property(o => o.Error).HasMaxLength(2000);
        builder.Property(o => o.RetryCount).IsRequired();
        builder.Property(o => o.CorrelationId).HasMaxLength(100);

        builder.HasIndex(o => o.ProcessedOnUtc);
        builder.HasIndex(o => o.RetryCount);
        builder.HasIndex(o => o.OccurredOnUtc);
    }
}
