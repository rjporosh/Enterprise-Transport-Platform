using BusService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusService.Infrastructure.Persistence.Configurations;

public sealed class BusConfiguration : IEntityTypeConfiguration<Bus>
{
    public void Configure(EntityTypeBuilder<Bus> builder)
    {
        builder.ToTable("buses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlateNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.PlateNumber).IsUnique();

        builder.Property(x => x.BusType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Manufacturer).HasMaxLength(100);
        builder.Property(x => x.Model).HasMaxLength(100);

        builder.HasIndex(x => x.OperatorId);
        builder.HasIndex(x => x.DepotId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });

        builder.Ignore(x => x.Version);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}
