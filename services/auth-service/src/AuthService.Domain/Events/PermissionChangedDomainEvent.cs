namespace AuthService.Domain.Events;

public sealed record PermissionChangedDomainEvent(string PermissionName) : Common.DomainEvent;
