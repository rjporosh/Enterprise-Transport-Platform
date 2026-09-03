using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Features.Notifications.SendNotification;

/// <summary>
/// Creates a Notification row (Pending or Scheduled) and enqueues its
/// NotificationCreatedDomainEvent to the outbox. Deliberately does NOT call
/// an IEmailSender/ISmsSender/IPushSender directly — the actual channel send
/// happens later, off the request thread, in NotificationDispatchJob
/// (Infrastructure/Scheduling/Jobs). This keeps the API responsive
/// regardless of provider latency, gives every send the same retry/backoff
/// path (immediate and scheduled sends flow through one dispatch code path
/// — no duplicated send logic here vs. in the retry job), and means an SMTP
/// or SMS-gateway outage never turns into a slow or failing API call.
/// </summary>
public sealed class SendNotificationHandler : IRequestHandler<SendNotificationCommand, Result<SendNotificationResultDto>>
{
    private readonly INotificationDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITemplateRenderer _templateRenderer;

    public SendNotificationHandler(
        INotificationDbContext dbContext,
        IEventPublisher eventPublisher,
        IDateTimeProvider dateTimeProvider,
        ITemplateRenderer templateRenderer)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _dateTimeProvider = dateTimeProvider;
        _templateRenderer = templateRenderer;
    }

    public async Task<Result<SendNotificationResultDto>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        // Inbox dedup: if SourceReference is set and a notification already exists
        // for this exact event id, return a lightweight success so the consumer can
        // ack the message without creating a duplicate row. Covers RabbitMQ
        // at-least-once redelivery and idempotent REST callers.
        if (!string.IsNullOrWhiteSpace(request.SourceReference))
        {
            var alreadyExists = await _dbContext.Notifications
                .AsNoTracking()
                .AnyAsync(n => n.SourceReference == request.SourceReference, cancellationToken);

            if (alreadyExists)
                return Result<SendNotificationResultDto>.Success(
                    new SendNotificationResultDto(Guid.Empty, request.Channel, NotificationStatus.Pending, null));
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var errors = new List<Error>();

        string? subject = request.Subject;
        string body = request.Body ?? string.Empty;
        string? dataPayload = request.DataPayload;
        Guid? templateId = null;
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "en" : request.Locale;

        if (!string.IsNullOrWhiteSpace(request.RecipientId))
        {
            var preference = await _dbContext.RecipientPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.RecipientId == request.RecipientId, cancellationToken);

            if (preference is not null)
            {
                locale = string.IsNullOrWhiteSpace(request.Locale) ? preference.Locale : locale;

                if (!request.IsTransactional && IsOptedOut(preference, request.Channel))
                {
                    errors.Add(Error.Conflict(
                        $"Recipient has opted out of {request.Channel} notifications."));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.TemplateKey))
        {
            var template = await ResolveTemplateAsync(request.TemplateKey!, request.Channel, locale, cancellationToken);
            if (template is null)
            {
                errors.Add(Error.NotFound(
                    $"No active template '{request.TemplateKey}' found for channel '{request.Channel}' (locale '{locale}' or fallback 'en')."));
            }
            else
            {
                var variables = request.TemplateVariables ?? new Dictionary<string, object?>();
                subject = template.Subject is null ? null : _templateRenderer.Render(template.Subject, variables);
                body = _templateRenderer.Render(template.Body, variables);
                dataPayload = template.DataPayloadTemplate is null
                    ? null
                    : _templateRenderer.Render(template.DataPayloadTemplate, variables);
                templateId = template.Id;
            }
        }

        if (errors.Count > 0)
            return Result<SendNotificationResultDto>.Failure(errors);

        var notification = Notification.Create(
            recipient: request.Recipient,
            channel: request.Channel,
            body: body,
            subject: subject,
            dataPayload: dataPayload,
            templateId: templateId,
            sourceReference: request.SourceReference,
            locale: locale,
            priority: request.Priority,
            scheduledForUtc: request.ScheduledForUtc,
            maxRetryCount: request.MaxRetryCount ?? Notification.DefaultMaxRetryCount,
            nowUtc: nowUtc);

        _dbContext.Notifications.Add(notification);

        foreach (var domainEvent in notification.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
        notification.ClearDomainEvents();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SendNotificationResultDto>.Success(new SendNotificationResultDto(
            notification.Id, notification.Channel, notification.Status, notification.ScheduledForUtc));
    }

    private static bool IsOptedOut(RecipientPreference preference, NotificationChannel channel) => channel switch
    {
        NotificationChannel.Email => preference.EmailOptOut,
        NotificationChannel.Sms => preference.SmsOptOut,
        NotificationChannel.Push => preference.PushOptOut,
        _ => false
    };

    /// <summary>Exact (key, channel, locale) match first; falls back to (key, channel, "en") so a partially-translated template set still works.</summary>
    private async Task<NotificationTemplate?> ResolveTemplateAsync(
        string templateKey, NotificationChannel channel, string locale, CancellationToken cancellationToken)
    {
        var templateChannel = (TemplateChannel)(int)channel;
        var key = templateKey.Trim().ToLowerInvariant();

        var exact = await _dbContext.NotificationTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == key && t.Channel == templateChannel && t.Locale == locale && t.IsActive && !t.IsDeleted, cancellationToken);
        if (exact is not null) return exact;

        if (locale == "en") return null;

        return await _dbContext.NotificationTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == key && t.Channel == templateChannel && t.Locale == "en" && t.IsActive && !t.IsDeleted, cancellationToken);
    }
}
