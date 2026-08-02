namespace AuthService.Domain.Events;

public sealed record UserLockedOutDomainEvent(Guid UserId, string Email, DateTimeOffset LockedUntilUtc) : Common.DomainEvent;
