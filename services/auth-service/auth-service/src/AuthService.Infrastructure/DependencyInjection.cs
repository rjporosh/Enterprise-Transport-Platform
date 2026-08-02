using AuthService.Application.Common.Interfaces;
using AuthService.Infrastructure.Caching;
using AuthService.Infrastructure.Common;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.Observability;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Persistence.Outbox;
using AuthService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AuthService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);

        services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AuthDbContext>());
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxProcessor>();

        // Single shared multiplexer for the app's lifetime; AbortOnConnectFail=false
        // is what makes RedisCacheService's "fail open" behavior true end-to-end.
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
        services.AddSingleton<IAuthMetrics, AuthMetrics>();

        return services;
    }

    /// <summary>
    /// "Database:Provider" in appsettings picks the EF Core provider at
    /// startup — Postgres | SqlServer | MySql — with zero code changes
    /// elsewhere in the app, satisfying the "switch DB easily" requirement.
    /// The connection string always comes from ConnectionStrings:AuthDb; its
    /// format just needs to match whichever provider is selected.
    /// See docs/architecture/auth-service-architecture.md, "Database
    /// portability" for the migration-generation implication (EF Core
    /// migrations are provider-specific — switching providers means
    /// regenerating migrations, not just flipping this setting in prod).
    /// </summary>
    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Postgres";
        var connectionString = configuration.GetConnectionString("AuthDb")
            ?? throw new InvalidOperationException("ConnectionStrings:AuthDb is not configured.");

        services.AddDbContext<AuthDbContext>(options =>
        {
            switch (provider.Trim().ToLowerInvariant())
            {
                case "postgres":
                case "postgresql":
                case "npgsql":
                    options.UseNpgsql(connectionString, npgsql =>
                        npgsql.MigrationsAssembly("AuthService.Infrastructure")
                              .MigrationsHistoryTable("__ef_migrations_history", "auth"));
                    break;

                case "sqlserver":
                case "mssql":
                    options.UseSqlServer(connectionString, sql =>
                        sql.MigrationsAssembly("AuthService.Infrastructure")
                           .MigrationsHistoryTable("__ef_migrations_history", "auth"));
                    break;

                case "mysql":
                    // Pomelo needs an explicit server version; AutoDetect requires
                    // an open connection at startup, which we deliberately avoid
                    // (fail fast on a real query, not on DI container build).
                    options.UseMySql(connectionString, ServerVersion.Create(new Version(8, 0, 0), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql), mysql =>
                        mysql.MigrationsAssembly("AuthService.Infrastructure"));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported Database:Provider '{provider}'. Supported: Postgres, SqlServer, MySql. " +
                        "See docs/architecture/auth-service-architecture.md, \"Database portability\" to add another.");
            }
        });
    }
}
