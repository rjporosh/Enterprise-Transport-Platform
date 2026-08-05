using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using BusService.Domain.Enums;
using BusService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Buses.ChangeBusStatus;

public sealed class ChangeBusStatusHandler : IRequestHandler<ChangeBusStatusCommand, BusDto>
{
    private readonly IBusDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly IBusMetrics _metrics;
    private readonly ICacheService _cache;

    public ChangeBusStatusHandler(IBusDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock, IBusMetrics metrics, ICacheService cache)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _metrics = metrics;
        _cache = cache;
    }

    public async Task<BusDto> Handle(ChangeBusStatusCommand request, CancellationToken cancellationToken)
    {
        var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == request.BusId, cancellationToken);
        if (bus is null)
            throw new BusNotFoundException(request.BusId);

        var newStatus = Enum.Parse<BusStatus>(request.NewStatus, ignoreCase: true);
        bus.ChangeStatus(newStatus, _clock.UtcNow);

        foreach (var domainEvent in bus.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        bus.ClearDomainEvents();

        _metrics.RecordStatusChange(bus.Status.ToString());
        await _cache.RemoveByPrefixAsync($"bus:{bus.Id}", cancellationToken);

        return new BusDto(bus.Id, bus.OperatorId, bus.PlateNumber, bus.BusType.ToString(), bus.TotalSeats, bus.DepotId,
            bus.Status.ToString(), bus.Manufacturer, bus.Model, bus.YearOfManufacture, bus.CreatedAtUtc, bus.UpdatedAtUtc);
    }
}
