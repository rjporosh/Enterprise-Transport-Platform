using MediatR;
using RouteService.Application.Common.Models;
using RouteService.Domain.Exceptions;

namespace RouteService.Application.Features.Schedules.GetSchedule;

public sealed record GetScheduleQuery(Guid ScheduleId) : IRequest<ScheduleDto>;
