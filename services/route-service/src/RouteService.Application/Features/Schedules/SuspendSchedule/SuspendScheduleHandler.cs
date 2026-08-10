using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Schedules.SuspendSchedule;

public sealed class SuspendScheduleHandler : IRequestHandler<SuspendScheduleCommand, Result>
{
    private readonly IRouteDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SuspendScheduleHandler> _logger;

    public SuspendScheduleHandler(IRouteDbContext context, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<SuspendScheduleHandler> logger)
    {
        _context = context;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(SuspendScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId && !s.IsDeleted, cancellationToken);

        if (schedule is null) return Result.Failure(new Error("ScheduleNotFound", $"Schedule '{request.ScheduleId}' was not found."));

        try
        {
            schedule.Suspend(_clock.UtcNow);
        }
        catch (InvalidScheduleException ex)
        {
            return Result.Failure(new Error("InvalidSchedule", ex.Message));
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to suspend schedule {ScheduleId}", request.ScheduleId);
            return Result.Failure(new Error("SaveFailed", "Failed to suspend schedule due to a database error."));
        }

        await _auditLogger.LogAsync("SuspendSchedule", "Schedule", schedule.Id, _currentUser.UserId, new { schedule.Status }, cancellationToken);

        return Result.Success();
    }
}
