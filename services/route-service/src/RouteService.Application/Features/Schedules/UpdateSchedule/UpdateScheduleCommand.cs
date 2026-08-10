using MediatR;
using RouteService.Application.Common.Models;
using RouteService.Domain.Exceptions;
using RouteService.Domain.Enums;
using RouteService.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Schedules.UpdateSchedule;

public sealed record UpdateScheduleCommand(Guid ScheduleId, TimeSpan DepartureTime, TimeSpan ArrivalTime, DateTimeOffset? EffectiveTo, uint ExpectedVersion, string? UpdatedBy) : IRequest<Result<ScheduleDto>>;
