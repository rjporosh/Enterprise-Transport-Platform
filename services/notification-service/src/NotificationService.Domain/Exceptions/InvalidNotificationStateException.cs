namespace NotificationService.Domain.Exceptions;

/// <summary>Raised when an operation is attempted against a notification whose current status makes it invalid — e.g. cancelling one that already Sent, or retrying one that is not Failed/DeadLettered.</summary>
public sealed class InvalidNotificationStateException : DomainException
{
    public InvalidNotificationStateException(string message) : base(message) { }
}
