using BookingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookingService.Infrastructure;

/// <summary>
/// Used by <c>dotnet ef migrations add</c> / <c>database update</c> at design
/// time. Postgres only — migrations for this service are Postgres-specific
/// (see docs/programmers-guide/database-provider-factory.md). The connection
/// string is a design-time placeholder; the runtime string comes from
/// configuration via <see cref="DependencyInjection"/>.
/// </summary>
public sealed class BookingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BookingDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=booking_service;Username=postgres;Password=postgres",
            npgsql => npgsql
                .MigrationsAssembly("BookingService.Infrastructure")
                .MigrationsHistoryTable("__ef_migrations_history", "booking"));

        return new BookingDbContext(optionsBuilder.Options);
    }
}
