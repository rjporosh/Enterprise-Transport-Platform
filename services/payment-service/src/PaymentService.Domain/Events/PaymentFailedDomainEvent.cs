namespace PaymentService.Domain.Events;

using PaymentService.Domain.Common;

/// <summary>
/// Published on <c>payment.events</c> under
/// <c>Platform.Contracts.EventTypes.PaymentFailed</c> (<c>payment.failed</c>).
/// <see cref="OrderReference"/> is the booking id — booking-service releases
/// the seat hold when it sees this.
/// </summary>
public sealed record PaymentFailedDomainEvent(
    Guid PaymentId,
    Guid TenantId,
    Guid CustomerId,
    string OrderReference,
    string Reason,
    string? ProviderErrorCode,
    DateTimeOffset OccurredOnUtc) : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
