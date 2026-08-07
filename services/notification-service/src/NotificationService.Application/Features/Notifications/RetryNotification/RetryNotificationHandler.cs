using MediatR;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Application.Features.Notifications.RetryNotification;

public sealed class RetryNotificationHandler : IRequestHandler<RetryNotificationCommand, Result>
{
    private readonly INotificationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RetryNotificationHandler(INotificationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RetryNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _dbContext.Notifications.FindAsync(new object?[] { request.NotificationId }, cancellationToken);
        if (notification is null || notification.IsDeleted)
            return Result.Failure(Error.NotFound($"Notification '{request.NotificationId}' was not found."));

        try
        {
            notification.ResetForManualRetry(request.AdditionalAttempts, _dateTimeProvider.UtcNow);
        }
        catch (InvalidNotificationStateException ex)
        {
            return Result.Failure(Error.InvalidState(ex.Message));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
