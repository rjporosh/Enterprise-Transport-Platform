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

        // See docs/architecture/bus-service-architecture.md, "Database
        // portability" — same trade-off as Auth Service's User aggregate.
        builder.Ignore(x => x.Version);
        builder.Ignore(x => x.DomainEvents);

        // No FK to Depot: Depot rows can be reassigned/retired independently
        // and a bus pointing at a depot that was since removed should still
        // be readable (DepotId becomes a "last known depot" pointer, not a
        // hard referential constraint) — RegisterBus/UpdateBusDetails
        // validate the depot exists at write time instead.
    }
}
