using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Configurations;

public sealed class BookingSeatConfiguration : IEntityTypeConfiguration<BookingSeat>
{
    public void Configure(EntityTypeBuilder<BookingSeat> builder)
    {
        builder.ToTable("booking_seats");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SeatNumber).HasMaxLength(10).IsRequired();
        builder.Property(x => x.PassengerFullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PassengerGender).HasMaxLength(20).IsRequired();
    }
}
