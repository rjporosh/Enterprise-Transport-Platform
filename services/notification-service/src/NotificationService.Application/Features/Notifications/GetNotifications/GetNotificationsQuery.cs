using MediatR;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Notifications.GetNotificationById;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Features.Notifications.GetNotifications;

/// <summary>Paged, filtered, searchable notification listing — backs the admin console's notification-history screen and support/troubleshooting lookups. Search matches Recipient, Subject, and SourceReference (case-insensitive substring).</summary>
public sealed record GetNotificationsQuery(
    int Page,
    int PageSize,
    NotificationChannel? Channel,
    NotificationStatus? Status,
    string? Recipient,
    string? SourceReference,
    string? Search,
    DateTimeOffset? CreatedFromUtc,
    DateTimeOffset? CreatedToUtc) : IRequest<Result<PagedResult<NotificationDto>>>;
