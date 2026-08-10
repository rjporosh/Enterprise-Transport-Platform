namespace NotificationService.Domain.Exceptions;

public sealed class NotificationNotFoundException : DomainException
{
    public NotificationNotFoundException(Guid notificationId)
        : base($"Notification {notificationId} was not found.") { }
}
