namespace AuthService.Domain.Events;

public sealed record PasswordResetCompletedDomainEvent(Guid UserId, string Email) : Common.DomainEvent;
