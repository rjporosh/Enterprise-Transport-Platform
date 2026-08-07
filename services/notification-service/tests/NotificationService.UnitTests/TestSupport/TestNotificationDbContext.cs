using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Domain.Entities;

namespace NotificationService.UnitTests.TestSupport;

/// <summary>EF Core InMemory-backed INotificationDbContext for handler tests -- exercises the real LINQ queries handlers issue (Where/Include/paging) without a real Postgres instance, matching AuthService.UnitTests' TestAuthDbContext pattern.</summary>
public sealed class TestNotificationDbContext : DbContext, INotificationDbContext
{
    public TestNotificationDbContext(DbContextOptions<TestNotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<RecipientPreference> RecipientPreferences => Set<RecipientPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(builder =>
        {
            builder.HasMany(n => n.Logs).WithOne().HasForeignKey(l => l.NotificationId);
            builder.Navigation(n => n.Logs).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasQueryFilter(n => !n.IsDeleted);
        });

        modelBuilder.Entity<NotificationTemplate>(builder => builder.HasQueryFilter(t => !t.IsDeleted));

        base.OnModelCreating(modelBuilder);
    }
}
