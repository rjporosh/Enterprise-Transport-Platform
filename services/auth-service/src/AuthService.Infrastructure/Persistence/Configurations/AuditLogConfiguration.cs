using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.IpAddress).HasMaxLength(45); // IPv6 max length
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.Details).HasMaxLength(2000);

        // Two access patterns dominate: "show me this user's history" and
        // "show me everything from this IP" (incident response / abuse
        // investigation) — both get a dedicated index rather than relying on
        // a scan plus filter.
        builder.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.IpAddress, x.OccurredAtUtc });
        builder.HasIndex(x => x.Action);

        // Deliberately NO foreign key to Users: audit rows must outlive the
        // user they describe (e.g. a deleted/GDPR-purged account's login
        // history stays queryable for fraud investigation) — see
        // docs/architecture/auth-service-architecture.md, "Audit trail".
    }
}
