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

        builder.HasIndex(x => new { x.TripId, x.SeatNumber }).IsUnique();
    }
}
