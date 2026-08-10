using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using BusService.Domain.Enums;
using BusService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Buses.UpdateBusDetails;

public sealed class UpdateBusDetailsHandler : IRequestHandler<UpdateBusDetailsCommand, BusDto>
{
    private readonly IBusDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly ICacheService _cache;

    public UpdateBusDetailsHandler(IBusDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock, ICacheService cache)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _cache = cache;
    }

    public async Task<BusDto> Handle(UpdateBusDetailsCommand request, CancellationToken cancellationToken)
    {
        var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == request.BusId, cancellationToken);
        if (bus is null)
            throw new BusNotFoundException(request.BusId);

        var depotExists = await _context.Depots.AnyAsync(d => d.Id == request.DepotId, cancellationToken);
        if (!depotExists)
            throw new DepotNotFoundException(request.DepotId);

        var busType = Enum.Parse<BusType>(request.BusType, ignoreCase: true);
        bus.UpdateDetails(busType, request.TotalSeats, request.DepotId, request.Manufacturer, request.Model, request.YearOfManufacture, _clock.UtcNow);

        foreach (var domainEvent in bus.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        bus.ClearDomainEvents();

        // Invalidate the cache-aside read for this bus rather than update it
        // in place — simpler and correct, at the cost of one extra DB read
        // on the next GetBus call, which is an acceptable trade for a
        // low-frequency admin action like this.
        await _cache.RemoveByPrefixAsync($"bus:{bus.Id}", cancellationToken);

        return new BusDto(bus.Id, bus.OperatorId, bus.PlateNumber, bus.BusType.ToString(), bus.TotalSeats, bus.DepotId,
            bus.Status.ToString(), bus.Manufacturer, bus.Model, bus.YearOfManufacture, bus.CreatedAtUtc, bus.UpdatedAtUtc);
    }
}
