namespace PaymentService.Domain.Events;

using PaymentService.Domain.Common;

public sealed record PaymentRefundedDomainEvent(
    Guid PaymentId,
    Guid RefundId,
    Guid TenantId,
    decimal RefundAmount,
    string Currency,
    string? ProviderRefundReference,
    DateTimeOffset OccurredOnUtc) : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
