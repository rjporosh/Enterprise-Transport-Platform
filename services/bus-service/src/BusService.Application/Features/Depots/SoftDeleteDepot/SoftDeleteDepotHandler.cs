using BusService.Application.Common.Interfaces;
using BusService.Domain.Entities;
using BusService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Depots.SoftDeleteDepot;

public sealed class SoftDeleteDepotHandler : IRequestHandler<SoftDeleteDepotCommand>
{
    private readonly IBusDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public SoftDeleteDepotHandler(IBusDbContext context, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(SoftDeleteDepotCommand request, CancellationToken cancellationToken)
    {
        var depot = await _context.Depots.FirstOrDefaultAsync(d => d.Id == request.DepotId, cancellationToken);
        if (depot is null)
            throw new DepotNotFoundException(request.DepotId);

        var deletedBy = _currentUser.UserId?.ToString() ?? "system";
        depot.SoftDelete(deletedBy, _clock.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record SoftDeleteDepotCommand(Guid DepotId) : IRequest;
