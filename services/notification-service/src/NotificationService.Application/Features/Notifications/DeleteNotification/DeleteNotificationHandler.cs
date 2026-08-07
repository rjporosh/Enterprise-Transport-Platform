using MediatR;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;

namespace NotificationService.Application.Features.Notifications.DeleteNotification;

public sealed class DeleteNotificationHandler : IRequestHandler<DeleteNotificationCommand, Result>
{
    private readonly INotificationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteNotificationHandler(INotificationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _dbContext.Notifications.FindAsync(new object?[] { request.NotificationId }, cancellationToken);
        if (notification is null)
            return Result.Failure(Error.NotFound($"Notification '{request.NotificationId}' was not found."));

        notification.SoftDelete(_dateTimeProvider.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
