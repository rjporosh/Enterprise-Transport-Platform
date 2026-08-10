namespace AuthService.Domain.Entities;

public sealed class Claim : Common.Entity
{
    public string Type { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public Guid? UserId { get; private set; }
    public Guid? RoleId { get; private set; }
    public Guid? PolicyId { get; private set; }

    private Claim() { }

    public Claim(Guid id, string type, string value, Guid? userId = null, Guid? roleId = null, Guid? policyId = null) : base(id)
    {
        Type = type;
        Value = value;
        UserId = userId;
        RoleId = roleId;
        PolicyId = policyId;
    }
}
