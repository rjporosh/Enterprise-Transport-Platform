namespace AuthService.Domain.Events;

/// <summary>Lets Notification Service send a "your password changed" security alert.</summary>
public sealed record PasswordChangedDomainEvent(Guid UserId, string Email) : Common.DomainEvent;
