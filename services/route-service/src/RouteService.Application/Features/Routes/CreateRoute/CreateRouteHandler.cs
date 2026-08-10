using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Enums;
using RouteService.Domain.Events;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Routes.CreateRoute;

public sealed class CreateRouteHandler : IRequestHandler<CreateRouteCommand, Result<RouteDto>>
{
    private readonly IRouteDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateRouteHandler> _logger;

    public CreateRouteHandler(IRouteDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<CreateRouteHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<RouteDto>> Handle(CreateRouteCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _context.Routes.AnyAsync(r => r.Code == normalizedCode && !r.IsDeleted, cancellationToken);
        if (codeExists) return Result<RouteDto>.Failure(new Error("DuplicateRouteCode", $"Route with code '{normalizedCode}' already exists."));

        var originExists = await _context.Stops.AnyAsync(s => s.Id == request.OriginStopId && !s.IsDeleted, cancellationToken);
        if (!originExists) return Result<RouteDto>.Failure(new Error("StopNotFound", $"Origin stop '{request.OriginStopId}' was not found."));

        var destinationExists = await _context.Stops.AnyAsync(s => s.Id == request.DestinationStopId && !s.IsDeleted, cancellationToken);
        if (!destinationExists) return Result<RouteDto>.Failure(new Error("StopNotFound", $"Destination stop '{request.DestinationStopId}' was not found."));

        if (!Enum.TryParse<TransportMode>(request.TransportMode, true, out var transportMode))
            return Result<RouteDto>.Failure(new Error("InvalidTransportMode", $"Transport mode '{request.TransportMode}' is invalid."));

        var now = _clock.UtcNow;
        var route = Route.Create(Guid.NewGuid(), normalizedCode, request.Name, request.OriginStopId, request.DestinationStopId, transportMode, request.DistanceKm, request.EstimatedDuration, now);

        _context.Routes.Add(route);

        foreach (var domainEvent in route.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save new route {Code}", normalizedCode);
            return Result<RouteDto>.Failure(new Error("SaveFailed", "Failed to create route due to a database error."));
        }

        route.ClearDomainEvents();

        await _auditLogger.LogAsync("CreateRoute", "Route", route.Id, _currentUser.UserId, new { route.Code, route.Name }, cancellationToken);

        var dto = new RouteDto(route.Id, route.Code, route.Name, route.OriginStopId, route.DestinationStopId, route.TransportMode.ToString(), route.DistanceKm, route.EstimatedDuration, route.Status.ToString(), route.Version, route.CreatedBy, route.UpdatedBy, route.CreatedAtUtc, route.UpdatedAtUtc);
        return Result<RouteDto>.Success(dto);
    }
}
