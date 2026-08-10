using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Routes.RestoreRoute;

public sealed class RestoreRouteHandler : IRequestHandler<RestoreRouteCommand, Result>
{
    private readonly IRouteDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RestoreRouteHandler> _logger;

    public RestoreRouteHandler(IRouteDbContext context, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<RestoreRouteHandler> logger)
    {
        _context = context;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(RestoreRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .FirstOrDefaultAsync(r => r.Id == request.RouteId && r.IsDeleted, cancellationToken);

        if (route is null) return Result.Failure(new Error("RouteNotFound", $"Route '{request.RouteId}' was not found or is not deleted."));

        route.Restore(_clock.UtcNow);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to restore route {RouteId}", request.RouteId);
            return Result.Failure(new Error("SaveFailed", "Failed to restore route due to a database error."));
        }

        await _auditLogger.LogAsync("RestoreRoute", "Route", route.Id, _currentUser.UserId, new { route.Code }, cancellationToken);

        return Result.Success();
    }
}
