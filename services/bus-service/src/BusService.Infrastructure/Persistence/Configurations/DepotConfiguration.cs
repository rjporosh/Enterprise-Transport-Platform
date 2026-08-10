using BusService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusService.Infrastructure.Persistence.Configurations;

public sealed class DepotConfiguration : IEntityTypeConfiguration<Depot>
{
    public void Configure(EntityTypeBuilder<Depot> builder)
    {
        builder.ToTable("depots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(300);

        builder.HasIndex(x => x.City);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
