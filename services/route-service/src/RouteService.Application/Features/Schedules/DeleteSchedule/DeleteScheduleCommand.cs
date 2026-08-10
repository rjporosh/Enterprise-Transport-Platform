using MediatR;
using RouteService.Application.Common.Models;

namespace RouteService.Application.Features.Schedules.DeleteSchedule;

public sealed record DeleteScheduleCommand(Guid ScheduleId, uint ExpectedVersion) : IRequest<Result>;
