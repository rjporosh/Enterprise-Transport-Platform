using MediatR;
using RouteService.Application.Common.Models;

namespace RouteService.Application.Features.Schedules.GetSchedules;

public sealed record GetSchedulesQuery(Guid? RouteId, string? Status, int Page = 1, int PageSize = 50) : IRequest<PagedResult<ScheduleDto>>;
