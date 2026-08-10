using RouteService.Application.Common.Interfaces;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Stops.UpdateStop;

public sealed class UpdateStopHandler : IRequestHandler<UpdateStopCommand, Result<StopDto>>
{
    private readonly IRouteDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateStopHandler> _logger;

    public UpdateStopHandler(IRouteDbContext context, IDateTimeProvider clock, IAuditLogger auditLogger, ICurrentUser currentUser, ILogger<UpdateStopHandler> logger)
    {
        _context = context;
        _clock = clock;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<StopDto>> Handle(UpdateStopCommand request, CancellationToken cancellationToken)
    {
        var stop = await _context.Stops
            .FirstOrDefaultAsync(s => s.Id == request.StopId && !s.IsDeleted, cancellationToken);

        if (stop is null) return Result<StopDto>.Failure(new Error("StopNotFound", $"Stop '{request.StopId}' was not found."));

        stop.Update(request.Name, request.City, request.Address, request.Latitude, request.Longitude, _clock.UtcNow);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update stop {StopId}", request.StopId);
            return Result<StopDto>.Failure(new Error("SaveFailed", "Failed to update stop due to a database error."));
        }

        await _auditLogger.LogAsync("UpdateStop", "Stop", stop.Id, _currentUser.UserId, new { stop.Code, stop.Name, stop.City }, cancellationToken);

        var dto = new StopDto(stop.Id, stop.Code, stop.Name, stop.City, stop.Address, stop.Latitude, stop.Longitude, stop.CreatedBy, stop.UpdatedBy, stop.CreatedAtUtc, stop.UpdatedAtUtc);
        return Result<StopDto>.Success(dto);
    }
}
