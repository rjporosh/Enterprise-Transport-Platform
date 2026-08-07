using NotificationService.Domain.Enums;

namespace NotificationService.Application.Features.Notifications.SendNotification;

public sealed record SendNotificationResultDto(
    Guid NotificationId,
    NotificationChannel Channel,
    NotificationStatus Status,
    DateTimeOffset? ScheduledForUtc);
