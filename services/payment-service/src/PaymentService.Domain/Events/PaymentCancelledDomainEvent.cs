namespace PaymentService.Domain.Events;

using PaymentService.Domain.Common;

public sealed record PaymentCancelledDomainEvent(
    Guid PaymentId,
    Guid TenantId,
    string? Reason,
    DateTimeOffset OccurredOnUtc) : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
