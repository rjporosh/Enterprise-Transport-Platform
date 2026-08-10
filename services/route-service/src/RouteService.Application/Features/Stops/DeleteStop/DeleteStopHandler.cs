using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Stops.DeleteStop;

public sealed class DeleteStopHandler : IRequestHandler<DeleteStopCommand, Result>
{
    private readonly IRouteDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteStopHandler> _logger;

    public DeleteStopHandler(IRouteDbContext context, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<DeleteStopHandler> logger)
    {
        _context = context;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteStopCommand request, CancellationToken cancellationToken)
    {
        var stop = await _context.Stops
            .FirstOrDefaultAsync(s => s.Id == request.StopId && !s.IsDeleted, cancellationToken);

        if (stop is null) return Result.Failure(new Error("StopNotFound", $"Stop '{request.StopId}' was not found."));

        var isUsed = await _context.RouteStops.AnyAsync(rs => rs.StopId == request.StopId && !rs.Route.IsDeleted, cancellationToken);
        if (isUsed) return Result.Failure(new Error("StopInUse", "Cannot delete a stop that is currently used by one or more routes."));

        stop.SoftDelete(_clock.UtcNow);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to delete stop {StopId}", request.StopId);
            return Result.Failure(new Error("SaveFailed", "Failed to delete stop due to a database error."));
        }

        await _auditLogger.LogAsync("DeleteStop", "Stop", stop.Id, _currentUser.UserId, new { stop.Code }, cancellationToken);

        return Result.Success();
    }
}
