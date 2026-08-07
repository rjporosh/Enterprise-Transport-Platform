using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Infrastructure.Persistence;
using Quartz;

namespace NotificationService.Infrastructure.Scheduling.Jobs;

/// <summary>
/// A notification is marked Sending immediately before the channel provider
/// call and only leaves that status on success/failure of that same call
/// (see Notification.MarkSending/MarkSent/MarkFailed). If the process
/// crashes or is killed between those two points, the row is stuck in
/// Sending forever with no job watching it — this job is that watcher: any
/// notification still Sending after StuckThreshold is assumed lost and is
/// force-failed (via the normal MarkFailed retry/dead-letter path) so it
/// re-enters the dispatch queue instead of silently vanishing.
/// </summary>
[DisallowConcurrentExecution]
public sealed class StuckNotificationRecoveryJob : IJob
{
    private static readonly TimeSpan StuckThreshold = TimeSpan.FromMinutes(10);
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StuckNotificationRecoveryJob> _logger;

    public StuckNotificationRecoveryJob(IServiceScopeFactory scopeFactory, ILogger<StuckNotificationRecoveryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var nowUtc = dateTimeProvider.UtcNow;
        var cutoff = nowUtc - StuckThreshold;

        var stuck = await dbContext.Notifications
            .Where(n => n.Status == Domain.Enums.NotificationStatus.Sending && n.UpdatedAtUtc != null && n.UpdatedAtUtc <= cutoff)
            .Take(BatchSize)
            .ToListAsync(context.CancellationToken);

        if (stuck.Count == 0) return;

        _logger.LogWarning(
            "StuckNotificationRecoveryJob found {Count} notification(s) stuck in Sending for over {Minutes} minutes " +
            "(likely a prior process crash mid-send); re-queuing them via the normal retry path.",
            stuck.Count, StuckThreshold.TotalMinutes);

        foreach (var notification in stuck)
        {
            notification.MarkFailed("Recovered from a stuck Sending state (process likely crashed mid-send).", nowUtc);

            foreach (var domainEvent in notification.DomainEvents)
                await eventPublisher.EnqueueAsync(domainEvent, context.CancellationToken);
            notification.ClearDomainEvents();
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
