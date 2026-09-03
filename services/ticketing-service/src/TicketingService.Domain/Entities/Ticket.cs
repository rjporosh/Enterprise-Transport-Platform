using TicketingService.Domain.Common;
using TicketingService.Domain.Enums;
using TicketingService.Domain.Events;
using TicketingService.Domain.ValueObjects;

namespace TicketingService.Domain.Entities;

/// <summary>
/// Aggregate root for an issued travel ticket. Created from a
/// <c>booking.confirmed</c> integration event; owns its number, verification
/// code, journey/passenger snapshot and lifecycle. Reissue preserves the
/// ticket number and verification code (a reprint, not a new ticket).
/// </summary>
public sealed class Ticket : AggregateRoot
{
    private readonly List<TicketSeat> _seats = new();

    public string Number { get; private set; } = default!;
    public string VerificationCode { get; private set; } = default!;
    public Guid BookingId { get; private set; }
    public Guid PaymentId { get; private set; }
    public Guid TripId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid OperatorId { get; private set; }

    public string CustomerName { get; private set; } = default!;
    public string CustomerEmail { get; private set; } = default!;
    public string? CustomerPhone { get; private set; }

    public string OriginCity { get; private set; } = default!;
    public string DestinationCity { get; private set; } = default!;
    public DateTimeOffset DepartureUtc { get; private set; }
    public DateTimeOffset ArrivalUtc { get; private set; }
    public string BusPlateNumber { get; private set; } = default!;
    public string BusType { get; private set; } = default!;

    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = default!;

    public Guid TemplateId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public int PrintCount { get; private set; }

    /// <summary>The rendered PDF bytes, cached (regenerated on template/reissue change).</summary>
    public byte[]? PdfBytes { get; private set; }

    public IReadOnlyCollection<TicketSeat> Seats => _seats.AsReadOnly();

    private Ticket() { }

    public static Ticket Issue(
        Guid bookingId, Guid paymentId, Guid tripId, Guid customerId, Guid operatorId, Guid templateId,
        string customerName, string customerEmail, string? customerPhone,
        string originCity, string destinationCity, DateTimeOffset departureUtc, DateTimeOffset arrivalUtc,
        string busPlateNumber, string busType, decimal totalAmount, string currency,
        IEnumerable<(string SeatNumber, string PassengerName)> seats,
        DateTimeOffset now)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Number = TicketNumber.New(now).Value,
            VerificationCode = ValueObjects.VerificationCode.New().Value,
            BookingId = bookingId,
            PaymentId = paymentId,
            TripId = tripId,
            CustomerId = customerId,
            OperatorId = operatorId,
            TemplateId = templateId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            CustomerPhone = customerPhone,
            OriginCity = originCity,
            DestinationCity = destinationCity,
            DepartureUtc = departureUtc,
            ArrivalUtc = arrivalUtc,
            BusPlateNumber = busPlateNumber,
            BusType = busType,
            TotalAmount = totalAmount,
            Currency = currency,
            Status = TicketStatus.Issued,
            IssuedAtUtc = now
        };

        foreach (var (seatNumber, passenger) in seats)
            ticket._seats.Add(new TicketSeat(Guid.NewGuid(), ticket.Id, seatNumber, passenger));

        return ticket;
    }

    public void AttachPdf(byte[] bytes) => PdfBytes = bytes;

    public string BuildVerificationUrl(string publicBaseUrl) =>
        $"{publicBaseUrl.TrimEnd('/')}/api/v1/tickets/verify/{VerificationCode}";

    public void RaiseIssued(string pdfUrl) => Raise(new TicketIssuedDomainEvent(
        Id, Number, BookingId, TripId, CustomerId, VerificationCode,
        CustomerEmail, CustomerName, CustomerPhone, OriginCity, DestinationCity, DepartureUtc, pdfUrl));

    public void MarkUsed(DateTimeOffset now)
    {
        if (Status != TicketStatus.Issued) throw new InvalidOperationException($"Ticket {Number} is {Status}, cannot be used.");
        Status = TicketStatus.Used;
        UsedAtUtc = now;
    }

    public void Cancel(string reason, DateTimeOffset now)
    {
        if (Status is TicketStatus.Cancelled or TicketStatus.Used)
            throw new InvalidOperationException($"Ticket {Number} is {Status}, cannot be cancelled.");
        Status = TicketStatus.Cancelled;
        CancelledAtUtc = now;
        Raise(new TicketCancelledDomainEvent(Id, Number, reason));
    }

    /// <summary>Reprint — same number + code, PDF regenerated, print count bumped.</summary>
    public void Reissue()
    {
        if (Status == TicketStatus.Cancelled) throw new InvalidOperationException($"Ticket {Number} is cancelled.");
        PrintCount++;
        PdfBytes = null; // force re-render
        Raise(new TicketReissuedDomainEvent(Id, Number, CustomerId));
    }
}

public sealed class TicketSeat : Entity
{
    public Guid TicketId { get; private set; }
    public string SeatNumber { get; private set; } = default!;
    public string PassengerName { get; private set; } = default!;

    private TicketSeat() { }
    public TicketSeat(Guid id, Guid ticketId, string seatNumber, string passengerName) : base(id)
    {
        TicketId = ticketId;
        SeatNumber = seatNumber;
        PassengerName = passengerName;
    }
}
