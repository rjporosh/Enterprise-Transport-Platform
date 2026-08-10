using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RouteService.Application.Common.Interfaces;
using RouteService.Domain.Entities;
using RouteService.Infrastructure.Persistence.Outbox;

namespace RouteService.Infrastructure.Persistence;

public sealed class RouteDbContext : DbContext, IRouteDbContext
{
    public RouteDbContext(DbContextOptions<RouteDbContext> options) : base(options) { }

    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Stop> Stops => Set<Stop>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasDefaultSchema("route");
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
