using BookingService.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => new { x.Id, x.Consumer });
        builder.Property(x => x.Consumer).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RoutingKey).HasMaxLength(100).IsRequired();
    }
}
