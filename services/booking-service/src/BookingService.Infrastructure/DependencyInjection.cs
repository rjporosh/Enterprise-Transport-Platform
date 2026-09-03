using BookingService.Application.Common.Interfaces;
using BookingService.Infrastructure.Caching;
using BookingService.Infrastructure.Common;
using BookingService.Infrastructure.Messaging;
using BookingService.Infrastructure.Messaging.Consumers;
using BookingService.Infrastructure.Observability;
using BookingService.Infrastructure.Observability.FileLogging;
using BookingService.Infrastructure.Persistence;
using BookingService.Infrastructure.Persistence.Outbox;
using BookingService.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using StackExchange.Redis;

namespace BookingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // File-based diagnostic logging (query logs). Registered before the
        // DbContext so the interceptor is resolvable when AddDbContext runs.
        services.Configure<FileLoggingOptions>(configuration.GetSection(FileLoggingOptions.SectionName));
        services.AddSingleton<QueryLogSink>();
        services.AddSingleton<IQueryLogSink>(sp => sp.GetRequiredService<QueryLogSink>());
        services.AddHostedService<QueryLogWriterBackgroundService>();

        AddDatabase(services, configuration);

        services.AddScoped<IBookingDbContext>(sp => sp.GetRequiredService<BookingDbContext>());
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxProcessor>();

        // Payment-driven booking confirmation: on payment.succeeded the
        // booking is confirmed + seats booked + booking.confirmed published;
        // on payment.failed the seat hold is released. Dedups via the inbox
        // table. (The Bus/Route read-model replicas are kept current by the
        // admin CreateTrip upsert rather than a separate consumer — see
        // CreateTripHandler.)
        services.AddHostedService<PaymentEventConsumer>();

        // Redis cache-aside for SearchTrips. Single shared multiplexer;
        // AbortOnConnectFail=false keeps a Redis outage from taking the API
        // down (RedisCacheService fails open).
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
            var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configOptions.AbortOnConnectFail = false;
            configOptions.ConnectTimeout = 3000;
            return ConnectionMultiplexer.Connect(configOptions);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IBookingMetrics, BookingMetrics>();

        AddScheduling(services);

        return services;
    }

    /// <summary>
    /// <c>Database:Provider</c> in configuration selects the EF Core provider
    /// at startup — Postgres (default/primary) | SqlServer | MySql | Sqlite.
    /// Switching is configuration-only; migrations remain provider-specific
    /// (regenerate for a non-Postgres target). Oracle and MongoDB are
    /// documented as not wired — MongoDB is not applicable to the relational
    /// Trip/Booking aggregates. See
    /// <c>docs/programmers-guide/database-provider-factory.md</c>.
    /// </summary>
    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var provider = (configuration["Database:Provider"] ?? "Postgres").Trim().ToLowerInvariant();
        var connectionString = configuration.GetConnectionString("BookingDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookingDb is not configured.");

        services.AddDbContext<BookingDbContext>((sp, options) =>
        {
            switch (provider)
            {
                case "postgres":
                case "postgresql":
                case "npgsql":
                    options.UseNpgsql(connectionString, npgsql => npgsql
                        .MigrationsAssembly("BookingService.Infrastructure")
                        .MigrationsHistoryTable("__ef_migrations_history", "booking"));
                    break;

                case "sqlserver":
                case "mssql":
                    options.UseSqlServer(connectionString, sql => sql
                        .MigrationsAssembly("BookingService.Infrastructure")
                        .MigrationsHistoryTable("__ef_migrations_history", "booking"));
                    break;

                case "mysql":
                    options.UseMySql(connectionString,
                        ServerVersion.Create(new Version(8, 0, 0), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql),
                        mysql => mysql.MigrationsAssembly("BookingService.Infrastructure"));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported Database:Provider '{provider}'. Supported: Postgres (default), SqlServer, MySql. " +
                        "Sqlite/Oracle/MongoDB: see docs/programmers-guide/database-provider-factory.md.");
            }

            var fileLogging = sp.GetRequiredService<IOptions<FileLoggingOptions>>().Value;
            if (fileLogging.EnableQueryLogging)
            {
                options.AddInterceptors(new QueryLoggingInterceptor(
                    sp.GetRequiredService<IQueryLogSink>(),
                    provider,
                    DescribeServer(provider)));
            }
        });
    }

    private static string DescribeServer(string provider) => provider switch
    {
        "postgres" or "postgresql" or "npgsql" => "PostgreSQL",
        "sqlserver" or "mssql" => "SQL Server",
        "mysql" => "MySQL",
        _ => provider
    };

    private static void AddScheduling(IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey(nameof(ExpiredHoldSweepJob));
            q.AddJob<ExpiredHoldSweepJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(t => t
                .ForJob(jobKey)
                .WithIdentity($"{nameof(ExpiredHoldSweepJob)}-trigger")
                .WithSimpleSchedule(s => s.WithIntervalInSeconds(60).RepeatForever()));
        });
        services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);
    }
}
