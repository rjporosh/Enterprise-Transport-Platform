namespace AuthService.Domain.Entities;

public sealed class Role : Common.Entity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private Role() { }

    public Role(Guid id, string name, string description) : base(id)
    {
        Name = name;
        Description = description;
        IsActive = true;
    }

    public void Update(string description)
    {
        Description = description;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public static class WellKnown
    {
        public const string Customer = "Customer";
        public const string Operator = "Operator";
        public const string Admin = "Admin";
    }
}
