using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using BusService.Domain.Entities;
using BusService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Buses.RestoreBus;

public sealed class RestoreBusHandler : IRequestHandler<RestoreBusCommand, BusDto>
{
    private readonly IBusDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly ICacheService _cache;

    public RestoreBusHandler(IBusDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock, ICacheService cache)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _cache = cache;
    }

    public async Task<BusDto> Handle(RestoreBusCommand request, CancellationToken cancellationToken)
    {
        var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == request.BusId, cancellationToken);
        if (bus is null)
            throw new BusNotFoundException(request.BusId);

        bus.Restore(_clock.UtcNow);

        foreach (var domainEvent in bus.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        bus.ClearDomainEvents();

        await _cache.RemoveByPrefixAsync($"bus:{bus.Id}", cancellationToken);

        return new BusDto(bus.Id, bus.OperatorId, bus.PlateNumber, bus.BusType.ToString(), bus.TotalSeats, bus.DepotId,
            bus.Status.ToString(), bus.Manufacturer, bus.Model, bus.YearOfManufacture, bus.TenantId, bus.CompanyId, bus.OrganizationId, bus.IsDeleted, bus.CreatedAtUtc, bus.UpdatedAtUtc);
    }
}

public sealed record RestoreBusCommand(Guid BusId) : IRequest<BusDto>;
