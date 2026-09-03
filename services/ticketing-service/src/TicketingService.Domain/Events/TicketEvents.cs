using TicketingService.Domain.Common;

namespace TicketingService.Domain.Events;

/// <summary>
/// Published on <c>ticket.events</c> under
/// <c>Platform.Contracts.EventTypes.TicketIssued</c>. Carries the customer
/// contact + a signed PDF URL so notification-service can deliver the ticket
/// without calling back.
/// </summary>
public sealed record TicketIssuedDomainEvent(
    Guid TicketId,
    string TicketNumber,
    Guid BookingId,
    Guid TripId,
    Guid CustomerId,
    string VerificationCode,
    string CustomerEmail,
    string CustomerName,
    string? CustomerPhone,
    string OriginCity,
    string DestinationCity,
    DateTimeOffset DepartureUtc,
    string PdfUrl) : DomainEvent;

public sealed record TicketCancelledDomainEvent(Guid TicketId, string TicketNumber, string Reason) : DomainEvent;

public sealed record TicketReissuedDomainEvent(Guid TicketId, string TicketNumber, Guid CustomerId) : DomainEvent;
