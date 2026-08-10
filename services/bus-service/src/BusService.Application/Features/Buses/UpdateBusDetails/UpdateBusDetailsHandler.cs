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

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("The bus was modified by another user. Please refresh and try again.");
        }

        bus.ClearDomainEvents();

        await _cache.RemoveByPrefixAsync($"bus:{bus.Id}", cancellationToken);

        return new BusDto(bus.Id, bus.OperatorId, bus.PlateNumber, bus.BusType.ToString(), bus.TotalSeats, bus.DepotId,
            bus.Status.ToString(), bus.Manufacturer, bus.Model, bus.YearOfManufacture, bus.TenantId, bus.CompanyId, bus.OrganizationId, bus.IsDeleted, bus.CreatedAtUtc, bus.UpdatedAtUtc);
    }
}
