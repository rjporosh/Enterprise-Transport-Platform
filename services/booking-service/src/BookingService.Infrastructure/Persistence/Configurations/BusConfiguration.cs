using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Configurations;

public sealed class BusConfiguration : IEntityTypeConfiguration<Bus>
{
    public void Configure(EntityTypeBuilder<Bus> builder)
    {
        builder.ToTable("buses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlateNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.BusType).HasMaxLength(50).IsRequired();
    }
}
