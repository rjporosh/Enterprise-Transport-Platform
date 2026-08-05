namespace BusService.Domain.Entities;

/// <summary>
/// Where a bus is based when not on a trip. A simple lookup entity, not an
/// aggregate root of its own — depots are managed by fleet admins directly,
/// with no invariants beyond "name and city are required."
/// </summary>
public sealed class Depot : Common.Entity
{
    public string Name { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string? Address { get; private set; }

    private Depot() { } // EF Core

    private Depot(Guid id, string name, string city, string? address) : base(id)
    {
        Name = name;
        City = city;
        Address = address;
    }

    public static Depot Create(Guid id, string name, string city, string? address) =>
        new(id, name, city, address);
}
