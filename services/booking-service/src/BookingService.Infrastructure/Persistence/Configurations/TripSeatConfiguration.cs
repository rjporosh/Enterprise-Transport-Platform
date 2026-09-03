using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Configurations;

public sealed class TripSeatConfiguration : IEntityTypeConfiguration<TripSeat>
{
    public void Configure(EntityTypeBuilder<TripSeat> builder)
    {
        builder.ToTable("trip_seats");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SeatNumber).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Deck).HasMaxLength(10);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        // Per-seat optimistic concurrency — two customers racing for the same
        // seat conflict on the second commit (see TripSeat.Version). Mapped to
        // Postgres' native xmin so there is no extra column to maintain.
        builder.Property(x => x.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();

        builder.HasIndex(x => new { x.TripId, x.SeatNumber }).IsUnique();
    }
}
