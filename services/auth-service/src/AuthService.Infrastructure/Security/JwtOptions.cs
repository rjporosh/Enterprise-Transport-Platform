namespace AuthService.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "https://identity.bus-ticketing.local";
    public string Audience { get; set; } = "bus-ticketing-api";
    public string SigningKey { get; set; } = "dev-only-signing-key-change-me-32chars-minimum";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
