using MediatR;
using RouteService.Application.Common.Models;

namespace RouteService.Application.Features.Schedules.ActivateSchedule;

public sealed record ActivateScheduleCommand(Guid ScheduleId) : IRequest<Result>;
