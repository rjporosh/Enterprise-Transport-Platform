using BusService.Application.Common.Interfaces;
using BusService.Domain.Entities;
using BusService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Buses.SoftDeleteBus;

public sealed class SoftDeleteBusHandler : IRequestHandler<SoftDeleteBusCommand>
{
    private readonly IBusDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;

    public SoftDeleteBusHandler(IBusDbContext context, ICurrentUser currentUser, IEventPublisher eventPublisher, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _eventPublisher = eventPublisher;
        _clock = clock;
    }

    public async Task Handle(SoftDeleteBusCommand request, CancellationToken cancellationToken)
    {
        var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == request.BusId, cancellationToken);
        if (bus is null)
            throw new BusNotFoundException(request.BusId);

        var deletedBy = _currentUser.UserId?.ToString() ?? "system";
        bus.SoftDelete(deletedBy, _clock.UtcNow);

        foreach (var domainEvent in bus.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        bus.ClearDomainEvents();
    }
}

public sealed record SoftDeleteBusCommand(Guid BusId) : IRequest;
