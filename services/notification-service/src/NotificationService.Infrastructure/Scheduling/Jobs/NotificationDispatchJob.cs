using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Persistence;
using Quartz;

namespace NotificationService.Infrastructure.Scheduling.Jobs;

/// <summary>
/// The single dispatch point for every outbound send — immediate,
/// scheduled-for-later, and automatic-retry notifications all become
/// eligible here and go through the exact same code path (see
/// SendNotificationHandler's remarks on why the API layer never calls a
/// channel sender directly). Runs on a short fixed interval (see
/// NotificationSchedulingExtensions) so "immediate" sends still feel close
/// to real-time without needing a separate hot path.
///
/// Picks up rows where:
///   Status = Pending, OR
///   Status = Scheduled AND ScheduledForUtc &lt;= now, OR
///   Status = Retrying AND NextRetryAtUtc &lt;= now
/// ordered by Priority (Critical first) then CreatedAtUtc (fairness within
/// a priority band), batched to avoid one run monopolizing the DB.
/// </summary>
[DisallowConcurrentExecution]
public sealed class NotificationDispatchJob : IJob
{
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDispatchJob> _logger;

    public NotificationDispatchJob(IServiceScopeFactory scopeFactory, ILogger<NotificationDispatchJob> logger)
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
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var smsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
        var pushSender = scope.ServiceProvider.GetRequiredService<IPushSender>();

        var nowUtc = dateTimeProvider.UtcNow;

        var due = await dbContext.Notifications
            .Where(n =>
                n.Status == NotificationStatus.Pending ||
                (n.Status == NotificationStatus.Scheduled && n.ScheduledForUtc <= nowUtc) ||
                (n.Status == NotificationStatus.Retrying && n.NextRetryAtUtc <= nowUtc))
            .OrderByDescending(n => n.Priority)
            .ThenBy(n => n.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(context.CancellationToken);

        if (due.Count == 0) return;

        _logger.LogInformation("NotificationDispatchJob picked up {Count} notification(s) to send.", due.Count);

        foreach (var notification in due)
        {
            // Claim-then-send: persist Sending status BEFORE the channel call so
            // that a process crash between the send and the result save leaves the
            // row in Sending (not Pending). StuckNotificationRecoveryJob resets
            // Sending rows after its stuck threshold — preventing a duplicate send
            // on the next dispatch cycle. Without this first save, a crash after
            // SMTP/SMS delivery but before SaveChanges would re-dispatch and
            // re-deliver the same notification.
            notification.MarkSending(dateTimeProvider.UtcNow);
            await dbContext.SaveChangesAsync(context.CancellationToken);

            await DispatchOneAsync(notification, dateTimeProvider, emailSender, smsSender, pushSender, context.CancellationToken);

            foreach (var domainEvent in notification.DomainEvents)
                await eventPublisher.EnqueueAsync(domainEvent, context.CancellationToken);
            notification.ClearDomainEvents();

            // Persist Sent/Failed status + outbox events atomically per notification.
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }

    private async Task DispatchOneAsync(
        Notification notification,
        IDateTimeProvider dateTimeProvider,
        IEmailSender emailSender,
        ISmsSender smsSender,
        IPushSender pushSender,
        CancellationToken cancellationToken)
    {
        // MarkSending is called by the caller loop before the first SaveChanges
        // (claim step) so we arrive here already in Sending status.
        var result = notification.Channel switch
        {
            NotificationChannel.Email => await emailSender.SendAsync(
                new EmailMessage(notification.Recipient, notification.Subject ?? string.Empty, notification.Body), cancellationToken),
            NotificationChannel.Sms => await smsSender.SendAsync(
                new SmsMessage(notification.Recipient, notification.Body), cancellationToken),
            NotificationChannel.Push => await pushSender.SendAsync(
                new PushMessage(notification.Recipient, notification.Subject ?? string.Empty, notification.Body, ParseDataPayload(notification.DataPayload)), cancellationToken),
            _ => new ChannelSendResult(false, null, $"Unsupported channel '{notification.Channel}'.")
        };

        var nowUtc = dateTimeProvider.UtcNow;
        if (result.IsSuccess)
            notification.MarkSent(nowUtc, result.ProviderMessageId);
        else
            notification.MarkFailed(result.Error ?? "Unknown channel send failure.", nowUtc);
    }

    private static IReadOnlyDictionary<string, string>? ParseDataPayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
