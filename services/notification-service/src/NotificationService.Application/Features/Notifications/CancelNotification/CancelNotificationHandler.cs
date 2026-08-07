using MediatR;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Application.Features.Notifications.CancelNotification;

public sealed class CancelNotificationHandler : IRequestHandler<CancelNotificationCommand, Result>
{
    private readonly INotificationDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelNotificationHandler(INotificationDbContext dbContext, IEventPublisher eventPublisher, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CancelNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _dbContext.Notifications.FindAsync(new object?[] { request.NotificationId }, cancellationToken);
        if (notification is null || notification.IsDeleted)
            return Result.Failure(Error.NotFound($"Notification '{request.NotificationId}' was not found."));

        try
        {
            notification.Cancel(request.Reason, _dateTimeProvider.UtcNow);
        }
        catch (InvalidNotificationStateException ex)
        {
            return Result.Failure(Error.InvalidState(ex.Message));
        }

        foreach (var domainEvent in notification.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
        notification.ClearDomainEvents();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
