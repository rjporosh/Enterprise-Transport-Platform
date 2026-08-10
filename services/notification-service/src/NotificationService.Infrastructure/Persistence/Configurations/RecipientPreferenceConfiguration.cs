using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public sealed class RecipientPreferenceConfiguration : IEntityTypeConfiguration<RecipientPreference>
{
    public void Configure(EntityTypeBuilder<RecipientPreference> builder)
    {
        builder.ToTable("recipient_preferences", "notification");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.RecipientId).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Locale).HasMaxLength(10).IsRequired();

        builder.HasIndex(p => p.RecipientId).IsUnique();
    }
}
