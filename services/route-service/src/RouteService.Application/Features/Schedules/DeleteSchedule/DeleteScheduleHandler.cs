using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Events;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Schedules.DeleteSchedule;

public sealed class DeleteScheduleHandler : IRequestHandler<DeleteScheduleCommand, Result>
{
    private readonly IRouteDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteScheduleHandler> _logger;

    public DeleteScheduleHandler(IRouteDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<DeleteScheduleHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId && !s.IsDeleted, cancellationToken);

        if (schedule is null) return Result.Failure(new Error("ScheduleNotFound", $"Schedule '{request.ScheduleId}' was not found."));

        if (schedule.Version != request.ExpectedVersion)
            return Result.Failure(new Error("ConcurrencyConflict", "The schedule has been modified by another user. Please refresh and try again."));

        schedule.SoftDelete(_clock.UtcNow);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict while deleting schedule {ScheduleId}", request.ScheduleId);
            return Result.Failure(new Error("ConcurrencyConflict", "The schedule has been modified by another user. Please refresh and try again."));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to delete schedule {ScheduleId}", request.ScheduleId);
            return Result.Failure(new Error("SaveFailed", "Failed to delete schedule due to a database error."));
        }

        await _auditLogger.LogAsync("DeleteSchedule", "Schedule", schedule.Id, _currentUser.UserId, new { schedule.RouteId }, cancellationToken);

        return Result.Success();
    }
}
