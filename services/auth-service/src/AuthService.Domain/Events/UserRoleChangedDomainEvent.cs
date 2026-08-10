namespace AuthService.Domain.Events;

public sealed record UserRoleChangedDomainEvent(Guid UserId, string Email) : Common.DomainEvent;
