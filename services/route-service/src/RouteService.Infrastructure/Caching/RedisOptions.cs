namespace RouteService.Infrastructure.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = "localhost:6379";
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(30);
}
