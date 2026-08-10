using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Infrastructure.Caching;
using PaymentService.Infrastructure.Common;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Observability;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Persistence.Outbox;
using PaymentService.Infrastructure.Providers;
using Pomelo.EntityFrameworkCore.MySql;
using System.Net.Http.Headers;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options =>
        {
            var provider = configuration["Database:Provider"] ?? "PostgreSQL";
            ApplyDatabaseProvider(options, provider, configuration.GetConnectionString("DefaultConnection") ?? string.Empty);
        });

        services.AddScoped<IPaymentDbContext>(sp => sp.GetRequiredService<PaymentDbContext>());

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddSingleton<IPaymentMetrics, PaymentMetrics>();

        services.Configure<RedisOptions>(configuration.GetSection("Redis"));
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMQ"));
        services.Configure<BkashOptions>(configuration.GetSection("Bkash"));

        services.AddHttpClient("Bkash", (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BkashOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddSingleton<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddSingleton<DefaultPaymentProvider>();
        services.AddSingleton<BkashPaymentProvider>();

        services.AddHostedService<OutboxProcessor>();

        return services;
    }

    private static void ApplyDatabaseProvider(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder options, string provider, string connectionString)
    {
        options = provider.ToLowerInvariant() switch
        {
            "postgresql" or "postgres" => options.UseNpgsql(connectionString),
            "sqlserver" or "mssql" => options.UseSqlServer(connectionString),
            "mysql" => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
            _ => throw new NotSupportedException($"Database provider '{provider}' is not supported.")
        };
    }
}
