using RouteService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RouteService.Infrastructure.Persistence.Configurations;

public sealed class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("route_stops");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StopOrder).IsRequired();
        builder.Property(x => x.ArrivalTimeOffset);
        builder.Property(x => x.DepartureTimeOffset);

        builder.HasOne(x => x.Route)
            .WithMany(r => r.Stops)
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Stop)
            .WithMany()
            .HasForeignKey(x => x.StopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RouteId, x.StopOrder }).IsUnique();
    }
}
