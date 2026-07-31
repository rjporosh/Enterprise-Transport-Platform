using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Configurations;

public sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("routes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginCity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DestinationCity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DistanceKm).HasColumnType("decimal(8,2)");
        builder.HasIndex(x => new { x.OriginCity, x.DestinationCity });
    }
}
