namespace AuthService.Domain.Events;

public sealed record SecurityQuestionsConfiguredDomainEvent(Guid UserId) : Common.DomainEvent;
