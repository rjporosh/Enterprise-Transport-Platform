using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;

namespace NotificationService.Application.Features.Notifications.GetNotificationById;

public sealed class GetNotificationByIdHandler : IRequestHandler<GetNotificationByIdQuery, Result<NotificationDto>>
{
    private readonly INotificationDbContext _dbContext;

    public GetNotificationByIdHandler(INotificationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<NotificationDto>> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        var notification = await _dbContext.Notifications
            .AsNoTracking()
            .Include(n => n.Logs)
            .Where(n => !n.IsDeleted)
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId, cancellationToken);

        if (notification is null)
            return Result<NotificationDto>.Failure(Error.NotFound($"Notification '{request.NotificationId}' was not found."));

        return Result<NotificationDto>.Success(new NotificationDto(
            notification.Id, notification.Recipient, notification.Channel, notification.Status, notification.Priority,
            notification.Subject, notification.Body, notification.SourceReference, notification.Locale,
            notification.ScheduledForUtc, notification.SentAtUtc, notification.DeliveredAtUtc,
            notification.RetryCount, notification.MaxRetryCount, notification.NextRetryAtUtc, notification.LastError,
            notification.CreatedAtUtc, notification.UpdatedAtUtc,
            notification.Logs
                .OrderBy(l => l.AttemptNumber)
                .Select(l => new NotificationLogDto(l.AttemptNumber, l.WasSuccessful, l.ProviderMessageId, l.Error, l.AttemptedAtUtc))
                .ToList()));
    }
}
