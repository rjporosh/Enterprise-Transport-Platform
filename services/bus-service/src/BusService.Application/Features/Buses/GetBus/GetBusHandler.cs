using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using BusService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Buses.GetBus;

public sealed class GetBusHandler : IRequestHandler<GetBusQuery, BusDto>
{
    private readonly IBusDbContext _context;
    private readonly ICacheService _cache;

    private static string CacheKey(Guid id) => $"bus:{id}";

    public GetBusHandler(IBusDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<BusDto> Handle(GetBusQuery request, CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync<BusDto>(CacheKey(request.BusId), cancellationToken);
        if (cached is not null)
            return cached;

        var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == request.BusId, cancellationToken);
        if (bus is null)
            throw new BusNotFoundException(request.BusId);

        var dto = new BusDto(bus.Id, bus.OperatorId, bus.PlateNumber, bus.BusType.ToString(), bus.TotalSeats, bus.DepotId,
            bus.Status.ToString(), bus.Manufacturer, bus.Model, bus.YearOfManufacture, bus.CreatedAtUtc, bus.UpdatedAtUtc);

        await _cache.SetAsync(CacheKey(request.BusId), dto, TimeSpan.FromMinutes(5), cancellationToken);
        return dto;
    }
}
