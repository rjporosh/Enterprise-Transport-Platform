namespace AuthService.Domain.Events;

/// <summary>
/// Published to RabbitMQ (routing key "auth.user.registered") for other
/// services to react to — e.g. Notification Service sends a welcome email,
/// Booking Service can lazily create a customer profile row.
/// </summary>
public sealed record UserRegisteredDomainEvent(Guid UserId, string Email, string FirstName, string LastName) : Common.DomainEvent;
