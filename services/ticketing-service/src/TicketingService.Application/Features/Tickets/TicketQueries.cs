using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketingService.Application.Common.Interfaces;
using TicketingService.Application.Common.Models;
using TicketingService.Domain.Enums;

namespace TicketingService.Application.Features.Tickets;

public sealed record TicketDto(
    Guid TicketId, string Number, string Status, Guid BookingId, Guid TripId,
    string OriginCity, string DestinationCity, DateTimeOffset DepartureUtc, DateTimeOffset ArrivalUtc,
    string BusPlateNumber, string BusType, decimal TotalAmount, string Currency,
    IReadOnlyCollection<string> Seats, IReadOnlyCollection<string> Passengers,
    string VerificationCode, DateTimeOffset IssuedAtUtc);

public sealed record TicketVerificationDto(
    string Number, string Status, string OriginCity, string DestinationCity,
    DateTimeOffset DepartureUtc, string BusPlateNumber, IReadOnlyCollection<string> Seats, bool IsValid);

// ---- Get my tickets --------------------------------------------------------
public sealed record GetMyTicketsQuery(Guid CustomerId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<TicketDto>>;

public sealed class GetMyTicketsHandler(ITicketingDbContext db) : IRequestHandler<GetMyTicketsQuery, PagedResult<TicketDto>>
{
    public async Task<PagedResult<TicketDto>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var q = db.Tickets.AsNoTracking().Where(t => t.CustomerId == request.CustomerId).OrderByDescending(t => t.IssuedAtUtc);
        var total = await q.CountAsync(cancellationToken);
        var rows = await q.Skip((page - 1) * size).Take(size).Include(t => t.Seats).ToListAsync(cancellationToken);

        return new PagedResult<TicketDto>(rows.Select(Map).ToList(), total, page, size);
    }

    internal static TicketDto Map(Domain.Entities.Ticket t) => new(
        t.Id, t.Number, t.Status.ToString(), t.BookingId, t.TripId,
        t.OriginCity, t.DestinationCity, t.DepartureUtc, t.ArrivalUtc,
        t.BusPlateNumber, t.BusType, t.TotalAmount, t.Currency,
        t.Seats.Select(s => s.SeatNumber).ToList(), t.Seats.Select(s => s.PassengerName).ToList(),
        t.VerificationCode, t.IssuedAtUtc);
}

// ---- Get one -------------------------------------------------------------
public sealed record GetTicketByIdQuery(Guid TicketId, Guid RequestedByCustomerId, bool IsPrivileged) : IRequest<TicketDto?>;

public sealed class GetTicketByIdHandler(ITicketingDbContext db) : IRequestHandler<GetTicketByIdQuery, TicketDto?>
{
    public async Task<TicketDto?> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var t = await db.Tickets.AsNoTracking().Include(x => x.Seats).FirstOrDefaultAsync(x => x.Id == request.TicketId, cancellationToken);
        if (t is null || (!request.IsPrivileged && t.CustomerId != request.RequestedByCustomerId)) return null;
        return GetMyTicketsHandler.Map(t);
    }
}

// ---- Get PDF ------------------------------------------------------------
public sealed record GetTicketPdfQuery(Guid TicketId, Guid RequestedByCustomerId, bool IsPrivileged) : IRequest<byte[]?>;

public sealed class GetTicketPdfHandler(ITicketingDbContext db, ITicketPdfRenderer pdf) : IRequestHandler<GetTicketPdfQuery, byte[]?>
{
    public async Task<byte[]?> Handle(GetTicketPdfQuery request, CancellationToken cancellationToken)
    {
        var t = await db.Tickets.Include(x => x.Seats).FirstOrDefaultAsync(x => x.Id == request.TicketId, cancellationToken);
        if (t is null || (!request.IsPrivileged && t.CustomerId != request.RequestedByCustomerId)) return null;
        if (t.PdfBytes is { Length: > 0 }) return t.PdfBytes;

        var template = await db.TicketTemplates.FirstOrDefaultAsync(x => x.Id == t.TemplateId, cancellationToken)
                       ?? Domain.Entities.TicketTemplate.Create(Guid.Empty, "Platform Default", "Enterprise Transport", true, t.IssuedAtUtc);
        var bytes = pdf.Render(t, template, t.BuildVerificationUrl(""));
        t.AttachPdf(bytes);
        await db.SaveChangesAsync(cancellationToken);
        return bytes;
    }
}

// ---- Verify (public) ---------------------------------------------------
public sealed record VerifyTicketQuery(string Code) : IRequest<TicketVerificationDto?>;

public sealed class VerifyTicketHandler(ITicketingDbContext db) : IRequestHandler<VerifyTicketQuery, TicketVerificationDto?>
{
    public async Task<TicketVerificationDto?> Handle(VerifyTicketQuery request, CancellationToken cancellationToken)
    {
        var t = await db.Tickets.AsNoTracking().Include(x => x.Seats)
            .FirstOrDefaultAsync(x => x.VerificationCode == request.Code, cancellationToken);
        if (t is null) return null;

        return new TicketVerificationDto(
            t.Number, t.Status.ToString(), t.OriginCity, t.DestinationCity, t.DepartureUtc,
            t.BusPlateNumber, t.Seats.Select(s => s.SeatNumber).ToList(),
            IsValid: t.Status == TicketStatus.Issued && t.DepartureUtc > DateTimeOffset.UtcNow.AddHours(-12));
    }
}
