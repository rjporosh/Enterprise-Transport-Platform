using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using BusService.Domain.Entities;
using BusService.Domain.Enums;
using BusService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Buses.RegisterBus;

public sealed class RegisterBusHandler : IRequestHandler<RegisterBusCommand, BusDto>
{
    private readonly IBusDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly IBusMetrics _metrics;
    private readonly ICurrentUser _currentUser;

    public RegisterBusHandler(IBusDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock, IBusMetrics metrics, ICurrentUser currentUser)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _metrics = metrics;
        _currentUser = currentUser;
    }

    public async Task<BusDto> Handle(RegisterBusCommand request, CancellationToken cancellationToken)
    {
        var normalizedPlate = request.PlateNumber.Trim().ToUpperInvariant();

        var depotExists = await _context.Depots.AnyAsync(d => d.Id == request.DepotId, cancellationToken);
        if (!depotExists)
            throw new DepotNotFoundException(request.DepotId);

        var plateTaken = await _context.Buses.AnyAsync(b => b.PlateNumber == normalizedPlate, cancellationToken);
        if (plateTaken)
            throw new DuplicatePlateNumberException(normalizedPlate);

        var busType = Enum.Parse<BusType>(request.BusType, ignoreCase: true);
        var now = _clock.UtcNow;
        var tenantId = _currentUser.TenantId ?? request.TenantId;
        var companyId = _currentUser.CompanyId ?? request.CompanyId;
        var organizationId = _currentUser.OrganizationId ?? request.OrganizationId;

        var bus = Bus.Register(
            Guid.NewGuid(), request.OperatorId, normalizedPlate, busType, request.TotalSeats, request.DepotId,
            request.Manufacturer, request.Model, request.YearOfManufacture, tenantId, companyId, organizationId, now);

        _context.Buses.Add(bus);

        foreach (var domainEvent in bus.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var wonByAnotherRequest = await _context.Buses.AnyAsync(b => b.Id != bus.Id && b.PlateNumber == normalizedPlate, cancellationToken);
            if (!wonByAnotherRequest)
                throw;

            throw new DuplicatePlateNumberException(normalizedPlate);
        }

        bus.ClearDomainEvents();
        _metrics.RecordBusRegistered();

        return new BusDto(bus.Id, bus.OperatorId, bus.PlateNumber, bus.BusType.ToString(), bus.TotalSeats, bus.DepotId,
            bus.Status.ToString(), bus.Manufacturer, bus.Model, bus.YearOfManufacture, bus.TenantId, bus.CompanyId, bus.OrganizationId, bus.IsDeleted, bus.CreatedAtUtc, bus.UpdatedAtUtc);
    }
}
