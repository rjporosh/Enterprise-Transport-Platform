using BusService.Application.Common.Interfaces;
using BusService.Infrastructure.Caching;
using BusService.Infrastructure.Common;
using BusService.Infrastructure.Messaging;
using BusService.Infrastructure.Observability;
using BusService.Infrastructure.Observability.FileLogging;
using BusService.Infrastructure.Persistence;
using BusService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BusService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Query logging interceptor must be registered before AddDatabase()
        // resolves it via the (IServiceProvider, options) AddDbContext overload.
        services.Configure<FileLoggingOptions>(configuration.GetSection(FileLoggingOptions.SectionName));
        services.AddSingleton<QueryLogSink>();
        services.AddSingleton<IQueryLogSink>(sp => sp.GetRequiredService<QueryLogSink>());
        services.AddSingleton<QueryLoggingInterceptor>();
        services.AddHostedService<QueryLogWriterBackgroundService>();

        AddDatabase(services, configuration);

        services.AddScoped<IBusDbContext>(sp => sp.GetRequiredService<BusDbContext>());
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

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
        services.AddSingleton<IBusMetrics, BusMetrics>();

        return services;
    }

    /// <summary>
    /// "Database:Provider" in appsettings picks the EF Core provider at
    /// startup — Postgres | SqlServer | MySql — matching the exact same
    /// switch Auth Service uses (see that service's
    /// docs/architecture/auth-service-architecture.md §8 for the full
    /// "Database portability" rationale, which applies here unchanged:
    /// migrations are still provider-specific, Oracle is still
    /// documented-but-not-wired for the same licensing/verification
    /// reasons, and Bus.Version is Ignore'd rather than mapped to a
    /// provider-specific concurrency column for the same reason
    /// User.Version is).
    /// </summary>
    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Postgres";
        var connectionString = configuration.GetConnectionString("BusDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BusDb is not configured.");

        services.AddDbContext<BusDbContext>((sp, options) =>
        {
            switch (provider.Trim().ToLowerInvariant())
            {
                case "postgres":
                case "postgresql":
                case "npgsql":
                    options.UseNpgsql(connectionString, npgsql =>
                        npgsql.MigrationsAssembly("BusService.Infrastructure")
                              .MigrationsHistoryTable("__ef_migrations_history", "bus"));
                    break;

                case "sqlserver":
                case "mssql":
                    options.UseSqlServer(connectionString, sql =>
                        sql.MigrationsAssembly("BusService.Infrastructure")
                           .MigrationsHistoryTable("__ef_migrations_history", "bus"));
                    break;

                case "mysql":
                    options.UseMySql(connectionString, ServerVersion.Create(new Version(8, 0, 0), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql), mysql =>
                        mysql.MigrationsAssembly("BusService.Infrastructure"));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported Database:Provider '{provider}'. Supported: Postgres, SqlServer, MySql. " +
                        "See docs/architecture/bus-service-architecture.md, \"Database portability\" to add another.");
            }

            var fileLoggingOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileLoggingOptions>>().Value;
            if (fileLoggingOptions.EnableQueryLogging)
                options.AddInterceptors(sp.GetRequiredService<QueryLoggingInterceptor>());
        });
    }
}
