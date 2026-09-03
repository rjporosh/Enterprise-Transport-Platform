using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketingService.Application.Common.Interfaces;
using TicketingService.Domain.Entities;

namespace TicketingService.Application.Features.Tickets;

public sealed record PassengerSeat(string SeatNumber, string PassengerName);

/// <summary>
/// Issues a ticket for a confirmed booking (invoked by the
/// <c>booking.confirmed</c> consumer). Idempotent on <see cref="BookingId"/> —
/// a redelivered event returns the existing ticket.
/// </summary>
public sealed record IssueTicketCommand(
    Guid BookingId,
    Guid PaymentId,
    Guid TripId,
    Guid CustomerId,
    Guid OperatorId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string OriginCity,
    string DestinationCity,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    string BusPlateNumber,
    string BusType,
    decimal TotalAmount,
    string Currency,
    IReadOnlyCollection<PassengerSeat> Seats) : IRequest<Guid>;

public sealed class IssueTicketHandler : IRequestHandler<IssueTicketCommand, Guid>
{
    private readonly ITicketingDbContext _db;
    private readonly ITicketPdfRenderer _pdf;
    private readonly IEventPublisher _events;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<IssueTicketHandler> _logger;
    private readonly string _publicBaseUrl;

    public IssueTicketHandler(
        ITicketingDbContext db,
        ITicketPdfRenderer pdf,
        IEventPublisher events,
        IDateTimeProvider clock,
        ILogger<IssueTicketHandler> logger,
        Microsoft.Extensions.Options.IOptions<TicketingSettings> settings)
    {
        _db = db;
        _pdf = pdf;
        _events = events;
        _clock = clock;
        _logger = logger;
        _publicBaseUrl = settings.Value.PublicBaseUrl;
    }

    public async Task<Guid> Handle(IssueTicketCommand request, CancellationToken cancellationToken)
    {
        var existing = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.BookingId == request.BookingId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Ticket already issued for booking {BookingId} ({Number}); skipping.", request.BookingId, existing.Number);
            return existing.Id;
        }

        var template = await ResolveTemplateAsync(request.OperatorId, cancellationToken);

        var ticket = Ticket.Issue(
            request.BookingId, request.PaymentId, request.TripId, request.CustomerId, request.OperatorId, template.Id,
            request.CustomerName, request.CustomerEmail, request.CustomerPhone,
            request.OriginCity, request.DestinationCity, request.DepartureUtc, request.ArrivalUtc,
            request.BusPlateNumber, request.BusType, request.TotalAmount, request.Currency,
            request.Seats.Select(s => (s.SeatNumber, s.PassengerName)),
            _clock.UtcNow);

        var verifyUrl = ticket.BuildVerificationUrl(_publicBaseUrl);
        ticket.AttachPdf(_pdf.Render(ticket, template, verifyUrl));
        ticket.RaiseIssued(verifyUrl.Replace("/verify/" + ticket.VerificationCode, $"/{ticket.Id}/pdf"));

        _db.Tickets.Add(ticket);
        foreach (var e in ticket.DomainEvents)
            await _events.EnqueueAsync(e, cancellationToken);
        ticket.ClearDomainEvents();
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Issued ticket {Number} for booking {BookingId}.", ticket.Number, request.BookingId);
        return ticket.Id;
    }

    private async Task<TicketTemplate> ResolveTemplateAsync(Guid operatorId, CancellationToken ct)
    {
        var operatorTemplate = await _db.TicketTemplates
            .Where(t => t.OperatorId == operatorId && t.IsActive)
            .OrderByDescending(t => t.IsDefault)
            .FirstOrDefaultAsync(ct);
        if (operatorTemplate is not null) return operatorTemplate;

        var platformDefault = await _db.TicketTemplates
            .FirstOrDefaultAsync(t => t.OperatorId == Guid.Empty && t.IsDefault, ct);
        if (platformDefault is not null) return platformDefault;

        // Nothing seeded — create the platform default on first use so a ticket is never blocked.
        var created = TicketTemplate.Create(Guid.Empty, "Platform Default", "Enterprise Transport", isDefault: true, _clock.UtcNow);
        _db.TicketTemplates.Add(created);
        return created;
    }
}
