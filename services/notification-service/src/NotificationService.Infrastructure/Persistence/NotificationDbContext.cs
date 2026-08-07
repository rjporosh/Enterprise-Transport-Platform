using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Persistence.Outbox;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext, INotificationDbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<RecipientPreference> RecipientPreferences => Set<RecipientPreference>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);

        modelBuilder.Ignore<NotificationService.Domain.Common.DomainEvent>();

        // Soft-delete convention (CLAUDE.md, "Soft Delete"): every query
        // against a soft-deletable aggregate is filtered by default. Handlers
        // that need deleted rows (none currently do) call
        // .IgnoreQueryFilters() explicitly rather than this being opt-in.
        modelBuilder.Entity<Notification>().HasQueryFilter(n => !n.IsDeleted);
        modelBuilder.Entity<NotificationTemplate>().HasQueryFilter(t => !t.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}
