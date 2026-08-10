using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http;
using RouteService.Application.Common.Interfaces;
using RouteService.Infrastructure.Caching;
using RouteService.Infrastructure.Common;
using RouteService.Infrastructure.Communication;
using RouteService.Infrastructure.Localization;
using RouteService.Infrastructure.Messaging;
using RouteService.Infrastructure.Observability;
using RouteService.Infrastructure.Persistence;
using RouteService.Infrastructure.Persistence.Interceptors;
using RouteService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace RouteService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IRouteDbContext>(sp => sp.GetRequiredService<RouteDbContext>());
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddSingleton<ILocalizationService, ResourceLocalizationService>();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxProcessor>();

        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
            var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configOptions.AbortOnConnectFail = false;
            configOptions.ConnectTimeout = 3000;
            return ConnectionMultiplexer.Connect(configOptions);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IRouteMetrics, RouteMetrics>();

        services.Configure<CommunicationOptions>(configuration.GetSection(CommunicationOptions.SectionName));
        services.AddHttpClient<ICommunicationService, HttpCommunicationService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<CommunicationOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
        });

        AddDatabase(services, configuration);

        return services;
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Postgres";
        var connectionString = configuration.GetConnectionString("RouteDb")
            ?? throw new InvalidOperationException("ConnectionStrings:RouteDb is not configured.");

        services.AddDbContext<RouteDbContext>((sp, options) =>
        {
            switch (provider.Trim().ToLowerInvariant())
            {
                case "postgres":
                case "postgresql":
                case "npgsql":
                    options.UseNpgsql(connectionString, npgsql =>
                        npgsql.MigrationsAssembly("RouteService.Infrastructure")
                              .MigrationsHistoryTable("__ef_migrations_history", "route"));
                    break;

                case "sqlserver":
                case "mssql":
                    options.UseSqlServer(connectionString, sql =>
                        sql.MigrationsAssembly("RouteService.Infrastructure")
                           .MigrationsHistoryTable("__ef_migrations_history", "route"));
                    break;

                case "mysql":
                    options.UseMySql(connectionString, ServerVersion.Create(new Version(8, 0, 0), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql), mysql =>
                        mysql.MigrationsAssembly("RouteService.Infrastructure"));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported Database:Provider '{provider}'. Supported: Postgres, SqlServer, MySql.");
            }

            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });
    }
}
