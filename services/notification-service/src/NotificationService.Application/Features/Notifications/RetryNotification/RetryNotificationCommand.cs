using MediatR;
using NotificationService.Application.Common.Models;

namespace NotificationService.Application.Features.Notifications.RetryNotification;

/// <summary>Manual operator action to give a DeadLettered notification a fresh retry budget (e.g. after fixing an SMTP credential issue). Distinct from the automatic Failed-&gt;Retrying loop the dispatch job already performs on its own.</summary>
public sealed record RetryNotificationCommand(Guid NotificationId, int AdditionalAttempts) : IRequest<Result>;
