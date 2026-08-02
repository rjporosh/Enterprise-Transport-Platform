namespace AuthService.Domain.Entities;

/// <summary>
/// A simple lookup entity, not an aggregate root of its own — roles are
/// created/managed by a future Admin feature; for now they are seeded
/// (Customer, Operator, Admin) via an EF Core HasData seed, see
/// RoleConfiguration.
/// </summary>
public sealed class Role : Common.Entity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    private Role() { }

    public Role(Guid id, string name, string description) : base(id)
    {
        Name = name;
        Description = description;
    }

    public static class WellKnown
    {
        public const string Customer = "Customer";
        public const string Operator = "Operator";
        public const string Admin = "Admin";
    }
}
