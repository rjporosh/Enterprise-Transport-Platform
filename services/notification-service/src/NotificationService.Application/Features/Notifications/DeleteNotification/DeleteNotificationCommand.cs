using MediatR;
using NotificationService.Application.Common.Models;

namespace NotificationService.Application.Features.Notifications.DeleteNotification;

/// <summary>Soft delete only (CLAUDE.md, "Soft Delete") -- hides the notification from listings/lookups via NotificationDbContext's global query filter while retaining the row (and its NotificationLog audit trail) for compliance/support. No status-transition restriction, unlike Cancel: an operator can remove any notification (even a terminal Sent/Failed one) from active views.</summary>
public sealed record DeleteNotificationCommand(Guid NotificationId) : IRequest<Result>;
