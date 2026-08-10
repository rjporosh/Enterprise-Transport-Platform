using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Events;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Schedules.CreateSchedule;

public sealed class CreateScheduleHandler : IRequestHandler<CreateScheduleCommand, Result<ScheduleDto>>
{
    private readonly IRouteDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateScheduleHandler> _logger;

    public CreateScheduleHandler(IRouteDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<CreateScheduleHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ScheduleDto>> Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        var routeExists = await _context.Routes.AnyAsync(r => r.Id == request.RouteId && !r.IsDeleted, cancellationToken);
        if (!routeExists) return Result<ScheduleDto>.Failure(new Error("RouteNotFound", $"Route '{request.RouteId}' was not found."));

        var now = _clock.UtcNow;
        var schedule = Schedule.Create(Guid.NewGuid(), request.RouteId, request.DepartureTime, request.ArrivalTime, request.EffectiveFrom, request.EffectiveTo, now);

        _context.Schedules.Add(schedule);

        foreach (var domainEvent in schedule.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save new schedule for route {RouteId}", request.RouteId);
            return Result<ScheduleDto>.Failure(new Error("SaveFailed", "Failed to create schedule due to a database error."));
        }

        schedule.ClearDomainEvents();

        await _auditLogger.LogAsync("CreateSchedule", "Schedule", schedule.Id, _currentUser.UserId, new { schedule.RouteId, schedule.DepartureTime, schedule.ArrivalTime }, cancellationToken);

        var dto = new ScheduleDto(schedule.Id, schedule.RouteId, schedule.DepartureTime, schedule.ArrivalTime, schedule.Status.ToString(), schedule.EffectiveFrom, schedule.EffectiveTo, schedule.Version, schedule.CreatedBy, schedule.UpdatedBy, schedule.CreatedAtUtc, schedule.UpdatedAtUtc);
        return Result<ScheduleDto>.Success(dto);
    }
}
