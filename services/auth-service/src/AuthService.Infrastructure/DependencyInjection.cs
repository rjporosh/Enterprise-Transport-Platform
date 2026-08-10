using AuthService.Application.Common.Interfaces;
using AuthService.Application.Common.Services;
using AuthService.Infrastructure.Caching;
using AuthService.Infrastructure.Common;
using AuthService.Infrastructure.Jobs;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.Observability;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Persistence.Outbox;
using AuthService.Infrastructure.Security;
using AuthService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
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

        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<ISmsSender, SmsSender>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IPasswordHistoryValidator, PasswordHistoryValidator>();
        services.AddScoped<ISecurityAnswerValidator, SecurityAnswerValidator>();

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
        services.AddSingleton<IAuthMetrics, AuthMetrics>();

        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            var jobKey = new JobKey(nameof(OtpCleanupJob));
            q.AddJob<OtpCleanupJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity($"{nameof(OtpCleanupJob)}-trigger")
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(3, 0)));
        });
        services.AddQuartzHostedService();

        return services;
    }

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
