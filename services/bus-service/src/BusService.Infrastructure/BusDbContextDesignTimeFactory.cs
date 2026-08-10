using BusService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusService.Infrastructure;

public sealed class BusDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BusDbContext>
{
    public BusDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BusDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=bus_service;Username=postgres;Password=postgres");

        return new BusDbContext(optionsBuilder.Options);
    }
}
