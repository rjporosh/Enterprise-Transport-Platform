using MediatR;
using NotificationService.Application.Common.Models;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Features.Notifications.SendNotification;

/// <summary>
/// Sends (or schedules, when ScheduledForUtc is in the future) a notification
/// on one channel. Content comes from EITHER an explicit Subject/Body OR a
/// TemplateKey + Variables bag to render server-side — exactly one must be
/// supplied (see SendNotificationValidator).
/// </summary>
public sealed record SendNotificationCommand(
    string Recipient,
    NotificationChannel Channel,
    string? TemplateKey,
    IReadOnlyDictionary<string, object?>? TemplateVariables,
    string? Subject,
    string? Body,
    string? DataPayload,
    string? RecipientId,
    string? SourceReference,
    string? Locale,
    NotificationPriority Priority,
    DateTimeOffset? ScheduledForUtc,
    int? MaxRetryCount,
    /// <summary>Transactional notifications (booking confirmation, OTP, payment receipt) bypass the recipient's channel opt-out. Marketing/informational sends must set this false and honor RecipientPreference.</summary>
    bool IsTransactional
) : IRequest<Result<SendNotificationResultDto>>;
