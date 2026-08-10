namespace AuthService.Domain.Events;

public sealed record OtpVerifiedDomainEvent(Guid UserId) : Common.DomainEvent;
