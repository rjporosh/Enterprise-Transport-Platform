using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Events;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Stops.CreateStop;

public sealed class CreateStopHandler : IRequestHandler<CreateStopCommand, Result<StopDto>>
{
    private readonly IRouteDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateStopHandler> _logger;

    public CreateStopHandler(IRouteDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<CreateStopHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<StopDto>> Handle(CreateStopCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _context.Stops.AnyAsync(s => s.Code == normalizedCode && !s.IsDeleted, cancellationToken);
        if (codeExists) return Result<StopDto>.Failure(new Error("DuplicateStopCode", $"Stop with code '{normalizedCode}' already exists."));

        var now = _clock.UtcNow;
        var stop = Stop.Create(Guid.NewGuid(), normalizedCode, request.Name, request.City, request.Address, request.Latitude, request.Longitude, now);

        _context.Stops.Add(stop);

        foreach (var domainEvent in stop.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save new stop {Code}", normalizedCode);
            return Result<StopDto>.Failure(new Error("SaveFailed", "Failed to create stop due to a database error."));
        }

        stop.ClearDomainEvents();

        await _auditLogger.LogAsync("CreateStop", "Stop", stop.Id, _currentUser.UserId, new { stop.Code, stop.Name, stop.City }, cancellationToken);

        var dto = new StopDto(stop.Id, stop.Code, stop.Name, stop.City, stop.Address, stop.Latitude, stop.Longitude, stop.CreatedBy, stop.UpdatedBy, stop.CreatedAtUtc, stop.UpdatedAtUtc);
        return Result<StopDto>.Success(dto);
    }
}
