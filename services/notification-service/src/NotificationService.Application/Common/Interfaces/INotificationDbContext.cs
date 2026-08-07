using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Common.Interfaces;

/// <summary>
/// The slice of NotificationDbContext that Application handlers are allowed
/// to see — DbSet access only, never SaveChanges (handlers commit via the
/// UnitOfWork-style pattern: DbContext.SaveChangesAsync is called exactly
/// once per request, from within the handler, after all aggregate mutations
/// and outbox enqueues for that request are staged). Mirrors
/// IBookingDbContext / IAuthDbContext.
/// </summary>
public interface INotificationDbContext
{
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationLog> NotificationLogs { get; }
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<RecipientPreference> RecipientPreferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
