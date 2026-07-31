using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TripId).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);

        builder.OwnsOne(x => x.TotalAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("total_amount").HasColumnType("decimal(10,2)");
            money.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3);
        });

        builder.Property(x => x.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();

        builder.HasMany(x => x.Seats)
            .WithOne()
            .HasForeignKey(s => s.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Seats).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.TripId);

        // Domain events are transient (dispatched to the outbox, never persisted
        // as a column), so explicitly tell EF Core to ignore them.
        builder.Ignore(x => x.DomainEvents);
    }
}
