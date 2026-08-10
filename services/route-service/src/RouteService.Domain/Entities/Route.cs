using RouteService.Domain.Common;
using RouteService.Domain.Interfaces;
using RouteService.Domain.Enums;
using RouteService.Domain.Events;
using RouteService.Domain.Exceptions;

namespace RouteService.Domain.Entities;

public sealed class Route : AggregateRoot, IAuditable
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Guid OriginStopId { get; private set; }
    public Guid DestinationStopId { get; private set; }
    public TransportMode TransportMode { get; private set; }
    public double DistanceKm { get; private set; }
    public TimeSpan EstimatedDuration { get; private set; }
    public RouteStatus Status { get; private set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    private readonly List<RouteStop> _stops = new();
    public IReadOnlyCollection<RouteStop> Stops => _stops.AsReadOnly();

    private Route() { }

    private Route(Guid id, string code, string name, Guid originStopId, Guid destinationStopId, TransportMode transportMode, double distanceKm, TimeSpan estimatedDuration, DateTimeOffset now)
        : base(id)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        OriginStopId = originStopId;
        DestinationStopId = destinationStopId;
        TransportMode = transportMode;
        DistanceKm = distanceKm;
        EstimatedDuration = estimatedDuration;
        Status = RouteStatus.Draft;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        IsDeleted = false;
        Version = 1;
    }

    public static Route Create(Guid id, string code, string name, Guid originStopId, Guid destinationStopId, TransportMode transportMode, double distanceKm, TimeSpan estimatedDuration, DateTimeOffset now)
    {
        if (originStopId == destinationStopId)
            throw new InvalidRouteException("Origin and destination stops cannot be the same.");

        var route = new Route(id, code, name, originStopId, destinationStopId, transportMode, distanceKm, estimatedDuration, now);
        route.Raise(new RouteCreatedDomainEvent(route.Id, route.Code, route.Name, route.OriginStopId, route.DestinationStopId, route.TransportMode));
        return route;
    }

    public void UpdateDetails(string name, TransportMode transportMode, double distanceKm, TimeSpan estimatedDuration, string? createdBy, DateTimeOffset now)
    {
        if (Status == RouteStatus.Deprecated)
            throw new InvalidRouteException("Cannot update a deprecated route.");

        Name = name.Trim();
        TransportMode = transportMode;
        DistanceKm = distanceKm;
        EstimatedDuration = estimatedDuration;
        UpdatedAtUtc = now;
        UpdatedBy = createdBy;
        Version++;

        Raise(new RouteUpdatedDomainEvent(Id, Code, Name, TransportMode));
    }

    public void ChangeStatus(RouteStatus newStatus, DateTimeOffset now)
    {
        var isValid = (Status, newStatus) switch
        {
            (RouteStatus.Draft, RouteStatus.Active) => true,
            (RouteStatus.Draft, RouteStatus.Deprecated) => true,
            (RouteStatus.Active, RouteStatus.Suspended) => true,
            (RouteStatus.Active, RouteStatus.Deprecated) => true,
            (RouteStatus.Suspended, RouteStatus.Active) => true,
            (RouteStatus.Suspended, RouteStatus.Deprecated) => true,
            _ => false
        };

        if (!isValid)
            throw new InvalidRouteException($"Invalid status transition from {Status} to {newStatus}.");

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAtUtc = now;
        Version++;

        Raise(new RouteStatusChangedDomainEvent(Id, Code, oldStatus, newStatus));
    }

    public void AddStop(RouteStop routeStop)
    {
        if (_stops.Any(s => s.StopId == routeStop.StopId))
            throw new InvalidRouteException("Stop already added to this route.");

        _stops.Add(routeStop);
    }

    public void RemoveStop(Guid stopId)
    {
        var stop = _stops.FirstOrDefault(s => s.StopId == stopId);
        if (stop is null) return;

        _stops.Remove(stop);
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = now;
        UpdatedAtUtc = now;
        Status = RouteStatus.Deprecated;
    }

    public void Restore(DateTimeOffset now)
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAtUtc = null;
        UpdatedAtUtc = now;
        Status = RouteStatus.Draft;
    }
}
