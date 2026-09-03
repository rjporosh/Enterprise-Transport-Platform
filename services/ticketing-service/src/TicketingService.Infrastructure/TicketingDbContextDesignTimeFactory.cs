using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TicketingService.Infrastructure.Persistence;

namespace TicketingService.Infrastructure;

public sealed class TicketingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TicketingDbContext>
{
    public TicketingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=ticketing_service;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly("TicketingService.Infrastructure")
                                .MigrationsHistoryTable("__ef_migrations_history", "ticketing"))
            .Options;
        return new TicketingDbContext(options);
    }
}
