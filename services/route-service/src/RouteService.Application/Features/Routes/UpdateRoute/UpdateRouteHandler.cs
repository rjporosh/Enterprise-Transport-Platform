using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Enums;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Routes.UpdateRoute;

public sealed class UpdateRouteHandler : IRequestHandler<UpdateRouteCommand, Result<RouteDto>>
{
    private readonly IRouteDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateRouteHandler> _logger;

    public UpdateRouteHandler(IRouteDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<UpdateRouteHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<RouteDto>> Handle(UpdateRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .FirstOrDefaultAsync(r => r.Id == request.RouteId && !r.IsDeleted, cancellationToken);

        if (route is null) return Result<RouteDto>.Failure(new Error("RouteNotFound", $"Route '{request.RouteId}' was not found."));

        if (route.Version != request.ExpectedVersion)
            return Result<RouteDto>.Failure(new Error("ConcurrencyConflict", "The route has been modified by another user. Please refresh and try again."));

        if (!Enum.TryParse<TransportMode>(request.TransportMode, true, out var transportMode))
            return Result<RouteDto>.Failure(new Error("InvalidTransportMode", $"Transport mode '{request.TransportMode}' is invalid."));

        try
        {
            route.UpdateDetails(request.Name, transportMode, request.DistanceKm, request.EstimatedDuration, request.UpdatedBy ?? _currentUser.UserId, _clock.UtcNow);
        }
        catch (InvalidRouteException ex)
        {
            return Result<RouteDto>.Failure(new Error("InvalidRoute", ex.Message));
        }

        foreach (var domainEvent in route.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict while updating route {RouteId}", request.RouteId);
            return Result<RouteDto>.Failure(new Error("ConcurrencyConflict", "The route has been modified by another user. Please refresh and try again."));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update route {RouteId}", request.RouteId);
            return Result<RouteDto>.Failure(new Error("SaveFailed", "Failed to update route due to a database error."));
        }

        route.ClearDomainEvents();

        await _auditLogger.LogAsync("UpdateRoute", "Route", route.Id, _currentUser.UserId, new { route.Code, route.Name, route.TransportMode }, cancellationToken);

        var dto = new RouteDto(route.Id, route.Code, route.Name, route.OriginStopId, route.DestinationStopId, route.TransportMode.ToString(), route.DistanceKm, route.EstimatedDuration, route.Status.ToString(), route.Version, route.CreatedBy, route.UpdatedBy, route.CreatedAtUtc, route.UpdatedAtUtc);
        return Result<RouteDto>.Success(dto);
    }
}
