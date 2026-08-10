namespace PaymentService.Domain.Events;

using PaymentService.Domain.Common;

public sealed record PaymentFailedDomainEvent(
    Guid PaymentId,
    Guid TenantId,
    string Reason,
    string? ProviderErrorCode,
    DateTimeOffset OccurredOnUtc) : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
