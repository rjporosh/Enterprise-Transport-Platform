using RouteService.Application.Common.Models;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Schedules.GetSchedule;

public sealed class GetScheduleHandler : IRequestHandler<GetScheduleQuery, ScheduleDto>
{
    private readonly IRouteDbContext _context;

    public GetScheduleHandler(IRouteDbContext context) => _context = context;

    public async Task<ScheduleDto> Handle(GetScheduleQuery request, CancellationToken cancellationToken)
    {
        var schedule = await _context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId && !s.IsDeleted, cancellationToken);

        if (schedule is null) throw new ScheduleNotFoundException(request.ScheduleId);

        return new ScheduleDto(schedule.Id, schedule.RouteId, schedule.DepartureTime, schedule.ArrivalTime, schedule.Status.ToString(), schedule.EffectiveFrom, schedule.EffectiveTo, schedule.Version, schedule.CreatedBy, schedule.UpdatedBy, schedule.CreatedAtUtc, schedule.UpdatedAtUtc);
    }
}
