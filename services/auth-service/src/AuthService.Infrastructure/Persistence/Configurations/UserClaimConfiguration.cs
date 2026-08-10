using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.ToTable("user_claims");
        builder.HasKey(x => new { x.UserId, x.Type, x.Value });
        builder.Property(x => x.Type).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
    }
}
