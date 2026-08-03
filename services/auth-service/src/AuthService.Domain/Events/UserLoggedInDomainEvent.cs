namespace AuthService.Domain.Events;

public sealed record UserLoggedInDomainEvent(Guid UserId, string Email, string? Ip) : Common.DomainEvent;
