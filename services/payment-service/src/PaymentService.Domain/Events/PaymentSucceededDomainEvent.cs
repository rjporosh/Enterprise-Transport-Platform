namespace PaymentService.Domain.Events;

using PaymentService.Domain.Common;
using PaymentService.Domain.Enums;

public sealed record PaymentSucceededDomainEvent(
    Guid PaymentId,
    Guid TenantId,
    string ProviderReference,
    string? ProviderTransactionId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
