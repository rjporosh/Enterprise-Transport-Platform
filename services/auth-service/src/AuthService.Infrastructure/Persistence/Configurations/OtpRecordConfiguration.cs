using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class OtpRecordConfiguration : IEntityTypeConfiguration<OtpRecord>
{
    public void Configure(EntityTypeBuilder<OtpRecord> builder)
    {
        builder.ToTable("otp_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Channel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Destination).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.VerifiedAtUtc);
        builder.Property(x => x.AttemptCount).IsRequired();
        builder.Property(x => x.ResendCount).IsRequired();
        builder.Property(x => x.IsUsed).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.Channel, x.IsUsed });
    }
}
