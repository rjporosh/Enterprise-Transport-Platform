using MediatR;
using RouteService.Application.Common.Models;

namespace RouteService.Application.Features.Schedules.SuspendSchedule;

public sealed record SuspendScheduleCommand(Guid ScheduleId) : IRequest<Result>;
