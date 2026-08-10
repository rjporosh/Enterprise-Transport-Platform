using System.Reflection;
using BusService.Application.Common.Interfaces;
using BusService.Domain.Entities;
using BusService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BusService.Infrastructure.Persistence;

public sealed class BusDbContext : DbContext, IBusDbContext
{
    public BusDbContext(DbContextOptions<BusDbContext> options) : base(options) { }

    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<Depot> Depots => Set<Depot>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasDefaultSchema("bus");
        base.OnModelCreating(modelBuilder);
    }
}
