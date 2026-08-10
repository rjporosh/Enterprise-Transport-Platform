using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("claims");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.Type, x.Value });
    }
}
