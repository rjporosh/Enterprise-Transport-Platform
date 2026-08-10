namespace PaymentService.Infrastructure.Caching;

public class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "payment-service";
    public int DefaultTtlSeconds { get; set; } = 300;
}
