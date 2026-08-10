using RouteService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RouteService.Infrastructure.Persistence.Configurations;

public sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("schedules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DepartureTime).IsRequired();
        builder.Property(x => x.ArrivalTime).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(x => x.RouteId);
        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.Version).IsUnique(false);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Ignore(x => x.DomainEvents);
    }
}
