namespace AuthService.Domain.Entities;

public sealed class Permission : Common.Entity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Module { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private Permission() { }

    public Permission(Guid id, string name, string description, string module) : base(id)
    {
        Name = name;
        Description = description;
        Module = module;
        IsActive = true;
    }

    public void Update(string description, string module)
    {
        Description = description;
        Module = module;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
