using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Schedules.UpdateSchedule;

public sealed class UpdateScheduleHandler : IRequestHandler<UpdateScheduleCommand, Result<ScheduleDto>>
{
    private readonly IRouteDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateScheduleHandler> _logger;

    public UpdateScheduleHandler(IRouteDbContext context, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<UpdateScheduleHandler> logger)
    {
        _context = context;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ScheduleDto>> Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId && !s.IsDeleted, cancellationToken);

        if (schedule is null) return Result<ScheduleDto>.Failure(new Error("ScheduleNotFound", $"Schedule '{request.ScheduleId}' was not found."));

        if (schedule.Version != request.ExpectedVersion)
            return Result<ScheduleDto>.Failure(new Error("ConcurrencyConflict", "The schedule has been modified by another user. Please refresh and try again."));

        try
        {
            schedule.Update(request.DepartureTime, request.ArrivalTime, request.EffectiveTo, _clock.UtcNow);
        }
        catch (InvalidScheduleException ex)
        {
            return Result<ScheduleDto>.Failure(new Error("InvalidSchedule", ex.Message));
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict while updating schedule {ScheduleId}", request.ScheduleId);
            return Result<ScheduleDto>.Failure(new Error("ConcurrencyConflict", "The schedule has been modified by another user. Please refresh and try again."));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update schedule {ScheduleId}", request.ScheduleId);
            return Result<ScheduleDto>.Failure(new Error("SaveFailed", "Failed to update schedule due to a database error."));
        }

        await _auditLogger.LogAsync("UpdateSchedule", "Schedule", schedule.Id, _currentUser.UserId, new { schedule.DepartureTime, schedule.ArrivalTime }, cancellationToken);

        var dto = new ScheduleDto(schedule.Id, schedule.RouteId, schedule.DepartureTime, schedule.ArrivalTime, schedule.Status.ToString(), schedule.EffectiveFrom, schedule.EffectiveTo, schedule.Version, schedule.CreatedBy, schedule.UpdatedBy, schedule.CreatedAtUtc, schedule.UpdatedAtUtc);
        return Result<ScheduleDto>.Success(dto);
    }
}
