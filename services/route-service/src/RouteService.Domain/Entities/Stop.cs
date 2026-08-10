using RouteService.Domain.Common;
using RouteService.Domain.Interfaces;
using RouteService.Domain.Events;
using RouteService.Domain.Exceptions;

namespace RouteService.Domain.Entities;

public sealed class Stop : AggregateRoot, IAuditable
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string? Address { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    private Stop() { }

    private Stop(Guid id, string code, string name, string city, string? address, double latitude, double longitude, DateTimeOffset now)
        : base(id)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        City = city.Trim();
        Address = address?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        IsDeleted = false;
    }

    public static Stop Create(Guid id, string code, string name, string city, string? address, double latitude, double longitude, DateTimeOffset now)
    {
        var stop = new Stop(id, code, name, city, address, latitude, longitude, now);
        stop.Raise(new StopCreatedDomainEvent(stop.Id, stop.Code, stop.Name, stop.City));
        return stop;
    }

    public void Update(string name, string city, string? address, double latitude, double longitude, DateTimeOffset now)
    {
        Name = name.Trim();
        City = city.Trim();
        Address = address?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAtUtc = now;

        Raise(new StopUpdatedDomainEvent(Id, Code, Name));
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Restore(DateTimeOffset now)
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAtUtc = null;
        UpdatedAtUtc = now;
    }
}
