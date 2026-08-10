using MediatR;
using RouteService.Application.Common.Models;
using RouteService.Domain.Enums;

namespace RouteService.Application.Features.Schedules.CreateSchedule;

public sealed record CreateScheduleCommand(Guid RouteId, TimeSpan DepartureTime, TimeSpan ArrivalTime, DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo, string? CreatedBy) : IRequest<Result<ScheduleDto>>;
