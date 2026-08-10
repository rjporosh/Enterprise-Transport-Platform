using NotificationService.Domain.Enums;

namespace NotificationService.Application.Features.Notifications.GetNotificationById;

public sealed record NotificationLogDto(
    int AttemptNumber, bool WasSuccessful, string? ProviderMessageId, string? Error, DateTimeOffset AttemptedAtUtc);

public sealed record NotificationDto(
    Guid Id,
    string Recipient,
    NotificationChannel Channel,
    NotificationStatus Status,
    NotificationPriority Priority,
    string? Subject,
    string Body,
    string? SourceReference,
    string? Locale,
    DateTimeOffset? ScheduledForUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    int RetryCount,
    int MaxRetryCount,
    DateTimeOffset? NextRetryAtUtc,
    string? LastError,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyCollection<NotificationLogDto> Logs);
