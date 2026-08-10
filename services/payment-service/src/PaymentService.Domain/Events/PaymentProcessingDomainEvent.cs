namespace PaymentService.Domain.Events;

using PaymentService.Domain.Common;
using PaymentService.Domain.Enums;

public sealed record PaymentProcessingDomainEvent(
    Guid PaymentId,
    Guid TenantId,
    string? ProviderReference,
    DateTimeOffset OccurredOnUtc) : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
