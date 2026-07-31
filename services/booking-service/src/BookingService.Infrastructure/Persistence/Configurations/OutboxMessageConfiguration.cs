using BookingService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);

        // The background OutboxProcessor polls exactly this shape of query:
        // "give me unprocessed messages, oldest first". Index it directly.
        builder.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc });
    }
}
