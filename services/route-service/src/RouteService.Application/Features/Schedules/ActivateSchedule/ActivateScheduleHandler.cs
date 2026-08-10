using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Enums;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Schedules.ActivateSchedule;

public sealed class ActivateScheduleHandler : IRequestHandler<ActivateScheduleCommand, Result>
{
    private readonly IRouteDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActivateScheduleHandler> _logger;

    public ActivateScheduleHandler(IRouteDbContext context, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<ActivateScheduleHandler> logger)
    {
        _context = context;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(ActivateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId && !s.IsDeleted, cancellationToken);

        if (schedule is null) return Result.Failure(new Error("ScheduleNotFound", $"Schedule '{request.ScheduleId}' was not found."));

        try
        {
            schedule.Activate(_clock.UtcNow);
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
            _logger.LogError(ex, "Failed to activate schedule {ScheduleId}", request.ScheduleId);
            return Result.Failure(new Error("SaveFailed", "Failed to activate schedule due to a database error."));
        }

        await _auditLogger.LogAsync("ActivateSchedule", "Schedule", schedule.Id, _currentUser.UserId, new { schedule.Status }, cancellationToken);

        return Result.Success();
    }
}
