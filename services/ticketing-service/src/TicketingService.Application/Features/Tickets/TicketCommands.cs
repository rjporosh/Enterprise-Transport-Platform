using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketingService.Application.Common.Interfaces;

namespace TicketingService.Application.Features.Tickets;

// ---- Cancel -----------------------------------------------------------
public sealed record CancelTicketCommand(Guid TicketId, string Reason, Guid RequestedByCustomerId, bool IsPrivileged) : IRequest;

public sealed class CancelTicketValidator : AbstractValidator<CancelTicketCommand>
{
    public CancelTicketValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}

public sealed class CancelTicketHandler(ITicketingDbContext db, IEventPublisher events, IDateTimeProvider clock)
    : IRequestHandler<CancelTicketCommand>
{
    public async Task Handle(CancelTicketCommand request, CancellationToken cancellationToken)
    {
        var t = await db.Tickets.FirstOrDefaultAsync(x => x.Id == request.TicketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");
        if (!request.IsPrivileged && t.CustomerId != request.RequestedByCustomerId)
            throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        t.Cancel(request.Reason, clock.UtcNow);
        foreach (var e in t.DomainEvents) await events.EnqueueAsync(e, cancellationToken);
        t.ClearDomainEvents();
        await db.SaveChangesAsync(cancellationToken);
    }
}

// ---- Reissue (reprint — same number) ---------------------------------
public sealed record ReissueTicketCommand(Guid TicketId, Guid RequestedByCustomerId, bool IsPrivileged) : IRequest<Guid>;

public sealed class ReissueTicketHandler(ITicketingDbContext db, ITicketPdfRenderer pdf, IEventPublisher events, IDateTimeProvider clock)
    : IRequestHandler<ReissueTicketCommand, Guid>
{
    public async Task<Guid> Handle(ReissueTicketCommand request, CancellationToken cancellationToken)
    {
        var t = await db.Tickets.Include(x => x.Seats).FirstOrDefaultAsync(x => x.Id == request.TicketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");
        if (!request.IsPrivileged && t.CustomerId != request.RequestedByCustomerId)
            throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        t.Reissue();
        var template = await db.TicketTemplates.FirstOrDefaultAsync(x => x.Id == t.TemplateId, cancellationToken)
                       ?? Domain.Entities.TicketTemplate.Create(Guid.Empty, "Platform Default", "Enterprise Transport", true, clock.UtcNow);
        t.AttachPdf(pdf.Render(t, template, t.BuildVerificationUrl("")));

        foreach (var e in t.DomainEvents) await events.EnqueueAsync(e, cancellationToken);
        t.ClearDomainEvents();
        await db.SaveChangesAsync(cancellationToken);
        return t.Id;
    }
}
