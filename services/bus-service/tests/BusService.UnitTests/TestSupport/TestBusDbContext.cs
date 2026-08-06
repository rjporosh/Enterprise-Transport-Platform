using BusService.Application.Common.Interfaces;
using BusService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusService.UnitTests.TestSupport;

public sealed class TestBusDbContext : DbContext, IBusDbContext
{
    public TestBusDbContext(DbContextOptions<TestBusDbContext> options) : base(options) { }

    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<Depot> Depots => Set<Depot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bus>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.PlateNumber).IsUnique();
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.Version);
        });

        modelBuilder.Entity<Depot>().HasKey(x => x.Id);
    }
}
