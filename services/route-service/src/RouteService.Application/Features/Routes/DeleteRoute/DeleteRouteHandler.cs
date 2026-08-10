using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Events;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Routes.DeleteRoute;

public sealed class DeleteRouteHandler : IRequestHandler<DeleteRouteCommand, Result>
{
    private readonly IRouteDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteRouteHandler> _logger;

    public DeleteRouteHandler(IRouteDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<DeleteRouteHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .FirstOrDefaultAsync(r => r.Id == request.RouteId && !r.IsDeleted, cancellationToken);

        if (route is null) return Result.Failure(new Error("RouteNotFound", $"Route '{request.RouteId}' was not found."));

        if (route.Version != request.ExpectedVersion)
            return Result.Failure(new Error("ConcurrencyConflict", "The route has been modified by another user. Please refresh and try again."));

        route.SoftDelete(_clock.UtcNow);

        foreach (var domainEvent in route.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict while deleting route {RouteId}", request.RouteId);
            return Result.Failure(new Error("ConcurrencyConflict", "The route has been modified by another user. Please refresh and try again."));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to delete route {RouteId}", request.RouteId);
            return Result.Failure(new Error("SaveFailed", "Failed to delete route due to a database error."));
        }

        route.ClearDomainEvents();

        await _auditLogger.LogAsync("DeleteRoute", "Route", route.Id, _currentUser.UserId, new { route.Code }, cancellationToken);

        return Result.Success();
    }
}
