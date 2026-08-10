namespace AuthService.Domain.Events;

public sealed record OtpFailedDomainEvent(Guid UserId) : Common.DomainEvent;
