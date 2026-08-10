using BusService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusService.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Resource).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Before).HasColumnType("text");
        builder.Property(x => x.After).HasColumnType("text");
        builder.Property(x => x.IpAddress).HasMaxLength(50);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.TraceId).HasMaxLength(100);

        builder.HasIndex(x => new { x.TenantId, x.Timestamp });
        builder.HasIndex(x => x.ResourceId);
        builder.HasIndex(x => x.Action);
    }
}
