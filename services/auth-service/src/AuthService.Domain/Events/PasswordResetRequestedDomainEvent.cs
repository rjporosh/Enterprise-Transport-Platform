namespace AuthService.Domain.Events;

public sealed record PasswordResetRequestedDomainEvent(Guid UserId, string Email) : Common.DomainEvent;
