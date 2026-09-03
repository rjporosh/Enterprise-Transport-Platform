namespace AuthService.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "https://identity.bus-ticketing.local";
    public string Audience { get; set; } = "bus-ticketing-api";
    public string SigningKey { get; set; } = "dev-only-signing-key-change-me-32chars-minimum";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 30;

    /// <summary>
    /// Tenant id stamped into the <c>tenant_id</c> claim of every access token.
    /// Single-tenant until M10 (SaaS foundation) makes this per-user. Keep in
    /// sync with the seed data's default tenant.
    /// </summary>
    public string DefaultTenantId { get; set; } = "00000000-0000-0000-0000-000000000001";
}
