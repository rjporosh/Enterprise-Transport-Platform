using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Infrastructure.Channels.Email;
using NotificationService.Infrastructure.Channels.Push;
using NotificationService.Infrastructure.Channels.Sms;
using NotificationService.Infrastructure.Common;
using NotificationService.Infrastructure.Localization;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Persistence.Outbox;
using NotificationService.Infrastructure.Retry;
using NotificationService.Infrastructure.Scheduling;
using NotificationService.Infrastructure.Templating;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);

        services.AddScoped<INotificationDbContext>(sp => sp.GetRequiredService<NotificationDbContext>());
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ITemplateRenderer, ScribanTemplateRenderer>();
        services.AddSingleton<ILocalizationService, ResourceLocalizationService>();

        // --- Messaging: outbound outbox relay + inbound upstream-event consumer ---
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<NotificationEventConsumer>();

        services.Configure<UserDirectoryOptions>(configuration.GetSection(UserDirectoryOptions.SectionName));
        services.AddHttpClient<IUserDirectoryClient, HttpUserDirectoryClient>();

        // --- Retry ---
        services.Configure<RetryOptions>(configuration.GetSection(RetryOptions.SectionName));
        services.AddSingleton<ChannelRetryPolicyFactory>();

        // --- Channels ---
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
        services.AddHttpClient<TwilioSmsSender>();
        services.AddHttpClient<GenericHttpSmsSender>();
        services.AddSingleton<ISmsSender>(sp => AddSmsSenderFactory(sp, configuration));

        services.Configure<PushOptions>(configuration.GetSection(PushOptions.SectionName));
        services.AddHttpClient<IPushSender, FcmPushSender>();

        // --- Scheduling ---
        services.AddNotificationScheduling();

        return services;
    }

    /// <summary>
    /// "Sms:Provider" in appsettings picks the ISmsSender implementation at
    /// startup — Twilio | GenericHttp — with zero code changes elsewhere,
    /// same "switch by config" convention as the database provider below.
    /// </summary>
    private static ISmsSender AddSmsSenderFactory(IServiceProvider sp, IConfiguration configuration)
    {
        var provider = (configuration[$"{SmsOptions.SectionName}:Provider"] ?? "GenericHttp").Trim().ToLowerInvariant();
        return provider switch
        {
            "twilio" => sp.GetRequiredService<TwilioSmsSender>(),
            "generichttp" => sp.GetRequiredService<GenericHttpSmsSender>(),
            _ => throw new InvalidOperationException(
                $"Unsupported Sms:Provider '{provider}'. Supported: Twilio, GenericHttp.")
        };
    }

    /// <summary>
    /// "Database:Provider" in appsettings picks the EF Core provider at
    /// startup — Postgres | SqlServer | MySql — identical convention and
    /// caveats (migrations are provider-specific; switching means
    /// regenerating them, not just flipping this setting in prod) to
    /// AuthService.Infrastructure.DependencyInjection.AddDatabase. Oracle and
    /// MongoDB are deliberately not wired — see
    /// NotificationService.Infrastructure.csproj comment and this
    /// delivery's Known Limitations.
    /// </summary>
    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Postgres";
        var connectionString = configuration.GetConnectionString("NotificationDb")
            ?? throw new InvalidOperationException("ConnectionStrings:NotificationDb is not configured.");

        services.AddDbContext<NotificationDbContext>(options =>
        {
            switch (provider.Trim().ToLowerInvariant())
            {
                case "postgres":
                case "postgresql":
                case "npgsql":
                    options.UseNpgsql(connectionString, npgsql =>
                        npgsql.MigrationsAssembly("NotificationService.Infrastructure")
                              .MigrationsHistoryTable("__ef_migrations_history", "notification"));
                    break;

                case "sqlserver":
                case "mssql":
                    options.UseSqlServer(connectionString, sql =>
                        sql.MigrationsAssembly("NotificationService.Infrastructure")
                           .MigrationsHistoryTable("__ef_migrations_history", "notification"));
                    break;

                case "mysql":
                    options.UseMySql(connectionString,
                        ServerVersion.Create(new Version(8, 0, 0), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql),
                        mysql => mysql.MigrationsAssembly("NotificationService.Infrastructure"));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported Database:Provider '{provider}'. Supported: Postgres, SqlServer, MySql. " +
                        "See docs/architecture/notification-service-architecture.md, \"Database portability\" to add another.");
            }
        });
    }
}
