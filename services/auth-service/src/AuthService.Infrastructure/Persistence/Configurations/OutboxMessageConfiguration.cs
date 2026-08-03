using AuthService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(300).IsRequired();
        // jsonb is Postgres-specific; portable providers fall back to a plain
        // string/nvarchar(max) column, still fine for our write-once,
        // read-sequentially outbox access pattern (we never query *inside*
        // the payload). See DependencyInjection.cs for the provider switch.
        builder.Property(x => x.Payload).HasColumnType("text").IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);

        builder.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc });
    }
}
