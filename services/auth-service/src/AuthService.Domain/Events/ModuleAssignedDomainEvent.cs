namespace AuthService.Domain.Events;

public sealed record ModuleAssignedDomainEvent(Guid UserId, Guid ModuleId) : Common.DomainEvent;
