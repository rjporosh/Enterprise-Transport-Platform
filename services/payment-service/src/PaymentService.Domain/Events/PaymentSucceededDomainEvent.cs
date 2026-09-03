namespace PaymentService.Domain.Events;

using PaymentService.Domain.Common;

/// <summary>
/// Published on <c>payment.events</c> under
/// <c>Platform.Contracts.EventTypes.PaymentSucceeded</c> (<c>payment.succeeded</c>).
/// <see cref="OrderReference"/> is the booking id — booking-service's
/// <c>PaymentEventConsumer</c> keys off it to confirm the booking.
/// </summary>
public sealed record PaymentSucceededDomainEvent(
    Guid PaymentId,
    Guid TenantId,
    Guid CustomerId,
    string OrderReference,
    string ProviderReference,
    string? ProviderTransactionId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
