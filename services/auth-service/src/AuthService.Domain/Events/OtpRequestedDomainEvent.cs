namespace AuthService.Domain.Events;

public sealed record OtpRequestedDomainEvent(Guid UserId, string Channel, string Destination) : Common.DomainEvent;
