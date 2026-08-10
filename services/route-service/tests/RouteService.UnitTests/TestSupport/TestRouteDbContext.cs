using Microsoft.EntityFrameworkCore;
using RouteService.Application.Common.Interfaces;

namespace RouteService.UnitTests.TestSupport;

public sealed class TestRouteDbContext : DbContext, IRouteDbContext
{
    public TestRouteDbContext(DbContextOptions<TestRouteDbContext> options) : base(options) { }

    public DbSet<RouteService.Domain.Entities.Route> Routes => Set<RouteService.Domain.Entities.Route>();
    public DbSet<RouteService.Domain.Entities.Stop> Stops => Set<RouteService.Domain.Entities.Stop>();
    public DbSet<RouteService.Domain.Entities.RouteStop> RouteStops => Set<RouteService.Domain.Entities.RouteStop>();
    public DbSet<RouteService.Domain.Entities.Schedule> Schedules => Set<RouteService.Domain.Entities.Schedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RouteService.Domain.Entities.Route>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Ignore(x => x.DomainEvents);
            builder.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<RouteService.Domain.Entities.Stop>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<RouteService.Domain.Entities.RouteStop>().HasKey(x => x.Id);
        modelBuilder.Entity<RouteService.Domain.Entities.Schedule>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Version).IsConcurrencyToken();
            builder.Ignore(x => x.DomainEvents);
        });
    }
}
