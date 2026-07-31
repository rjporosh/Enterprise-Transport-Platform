using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Configurations;

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RouteId).IsRequired();
        builder.Property(x => x.BusId).IsRequired();
        builder.Property(x => x.DepartureUtc).IsRequired();
        builder.Property(x => x.ArrivalUtc).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(x => x.BasePrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("base_price_amount").HasColumnType("decimal(10,2)");
            money.Property(m => m.Currency).HasColumnName("base_price_currency").HasMaxLength(3);
        });

        // Optimistic concurrency via Postgres' native system column `xmin`,
        // which the server bumps on every row update for free — no extra
        // write, no extra index, no application code needed to maintain it.
        builder.Property(x => x.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();

        builder.HasMany(x => x.Seats)
            .WithOne()
            .HasForeignKey(s => s.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seats is exposed as a read-only wrapper over the private `_seats`
        // list, so EF must mutate the field directly rather than the getter-only property.
        builder.Navigation(x => x.Seats).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.RouteId, x.DepartureUtc });

        builder.Ignore(x => x.DomainEvents);
    }
}
