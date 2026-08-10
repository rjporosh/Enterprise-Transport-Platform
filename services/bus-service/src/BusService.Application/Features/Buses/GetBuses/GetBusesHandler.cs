using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using BusService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Buses.GetBuses;

public sealed class GetBusesHandler : IRequestHandler<GetBusesQuery, PagedResult<BusDto>>
{
    private const int MaxPageSize = 200;

    private readonly IBusDbContext _context;

    public GetBusesHandler(IBusDbContext context) => _context = context;

    public async Task<PagedResult<BusDto>> Handle(GetBusesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Buses.AsQueryable();

        if (request.OperatorId.HasValue)
            query = query.Where(b => b.OperatorId == request.OperatorId.Value);

        if (request.DepotId.HasValue)
            query = query.Where(b => b.DepotId == request.DepotId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<BusStatus>(request.Status, ignoreCase: true, out var status))
            query = query.Where(b => b.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(b => b.PlateNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BusDto(b.Id, b.OperatorId, b.PlateNumber, b.BusType.ToString(), b.TotalSeats, b.DepotId,
                b.Status.ToString(), b.Manufacturer, b.Model, b.YearOfManufacture, b.CreatedAtUtc, b.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<BusDto>(items, page, pageSize, totalCount);
    }
}
